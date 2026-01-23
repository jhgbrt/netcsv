using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
namespace Net.Code.Csv;

public static class Extensions
{
    private readonly record struct PropertyMapping(
        PropertyInfo Property,
        string ColumnName,
        int SchemaOrdinal,
        string Format,
        bool AllowNull);

    static Func<IDataRecord, T> GetActivator<T>(CsvSchema schema)
    {
        var type = typeof(T);
        var activator = GetActivator(type, schema);
        return record => (T)activator(record);
    }

    private static Func<IDataRecord, object> GetActivator(Type type, CsvSchema schema)
    {
        schema ??= Schema.From(type);
        var mappings = GetPropertyMappings(type, schema);
        var properties = mappings.Select(m => m.Property).ToArray();

        var slowActivator = BuildSlowActivator(type, mappings, properties);
        var fastActivator = TryBuildFastActivator(type, mappings, properties);

        if (fastActivator is null)
        {
            return slowActivator;
        }

        return record => record is CsvDataReader reader ? fastActivator(reader) : slowActivator(record);
    }

    private static Func<IDataRecord, object> BuildSlowActivator(Type type, PropertyMapping[] mappings, PropertyInfo[] properties)
    {
        int[] ordinals = null;
        int[] EnsureOrdinals(IDataRecord record)
        {
            if (ordinals != null)
            {
                return ordinals;
            }

            ordinals = new int[mappings.Length];
            if (record is CsvDataReader)
            {
                for (var i = 0; i < mappings.Length; i++)
                {
                    ordinals[i] = mappings[i].SchemaOrdinal;
                }
                return ordinals;
            }

            for (var i = 0; i < mappings.Length; i++)
            {
                ordinals[i] = record.GetOrdinal(mappings[i].ColumnName);
            }

            return ordinals;
        }

        var constructor = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType))));

        if (constructor is null)
        {
            constructor = type.GetConstructor([]);
            return record =>
            {
                var item = constructor.Invoke([]);
                var resolvedOrdinals = EnsureOrdinals(record);
                for (var i = 0; i < mappings.Length; i++)
                {
                    mappings[i].Property.SetValue(item, record.GetValue(resolvedOrdinals[i]));
                }
                return item;
            };
        }

        var values = new object[mappings.Length];
        return record =>
        {
            var resolvedOrdinals = EnsureOrdinals(record);
            for (var i = 0; i < mappings.Length; i++)
            {
                values[i] = record.GetValue(resolvedOrdinals[i]);
            }
            return constructor.Invoke(values);
        };
    }

    private static Func<CsvDataReader, object> TryBuildFastActivator(Type type, PropertyMapping[] mappings, PropertyInfo[] properties)
    {
        if (mappings.Length == 0)
        {
            return null;
        }

        var constructor = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType))));

        if (constructor is null && properties.Any(p => p.SetMethod is null))
        {
            return null;
        }

        var readerParameter = Expression.Parameter(typeof(CsvDataReader), "reader");

        if (constructor is not null)
        {
            var arguments = new Expression[mappings.Length];
            for (var i = 0; i < mappings.Length; i++)
            {
                arguments[i] = BuildValueExpression(readerParameter, mappings[i]);
            }

            var newExpression = Expression.New(constructor, arguments);
            var body = Expression.Convert(newExpression, typeof(object));
            return Expression.Lambda<Func<CsvDataReader, object>>(body, readerParameter).Compile();
        }

        var itemVariable = Expression.Variable(type, "item");
        var expressions = new List<Expression>
        {
            Expression.Assign(itemVariable, Expression.New(type))
        };

        foreach (var mapping in mappings)
        {
            var valueExpression = BuildValueExpression(readerParameter, mapping);
            var assignExpression = Expression.Assign(Expression.Property(itemVariable, mapping.Property), valueExpression);
            expressions.Add(assignExpression);
        }

        expressions.Add(Expression.Convert(itemVariable, typeof(object)));
        var block = Expression.Block(new[] { itemVariable }, expressions);
        return Expression.Lambda<Func<CsvDataReader, object>>(block, readerParameter).Compile();
    }

    private static Expression BuildValueExpression(ParameterExpression reader, PropertyMapping mapping)
    {
        var propertyType = mapping.Property.PropertyType;
        var underlyingType = propertyType.GetUnderlyingType();
        var isNullable = propertyType.IsNullableType();
        var ordinalExpression = Expression.Constant(mapping.SchemaOrdinal);
        var formatExpression = Expression.Constant(mapping.Format, typeof(string));

        if (underlyingType == typeof(string))
        {
            return Expression.Call(reader, Methods.GetStringRaw, ordinalExpression);
        }

        if (underlyingType == typeof(bool))
        {
            var method = isNullable ? Methods.GetBooleanNullableRaw : Methods.GetBooleanRaw;
            return Expression.Call(reader, method, ordinalExpression, formatExpression);
        }

        if (underlyingType == typeof(DateTime))
        {
            var method = isNullable ? Methods.GetDateTimeNullableRaw : Methods.GetDateTimeRaw;
            return Expression.Call(reader, method, ordinalExpression, formatExpression);
        }

        if (underlyingType == typeof(DateTimeOffset))
        {
            var method = isNullable ? Methods.GetDateTimeOffsetNullableRaw : Methods.GetDateTimeOffsetRaw;
            return Expression.Call(reader, method, ordinalExpression, formatExpression);
        }

        if (underlyingType == typeof(Guid))
        {
            var method = isNullable ? Methods.GetGuidNullableRaw : Methods.GetGuidRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(char))
        {
            var method = isNullable ? Methods.GetCharNullableRaw : Methods.GetCharRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(byte))
        {
            var method = isNullable ? Methods.GetByteNullableRaw : Methods.GetByteRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(sbyte))
        {
            var method = isNullable ? Methods.GetSByteNullableRaw : Methods.GetSByteRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(short))
        {
            var method = isNullable ? Methods.GetInt16NullableRaw : Methods.GetInt16Raw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(int))
        {
            var method = isNullable ? Methods.GetInt32NullableRaw : Methods.GetInt32Raw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(long))
        {
            var method = isNullable ? Methods.GetInt64NullableRaw : Methods.GetInt64Raw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(ushort))
        {
            var method = isNullable ? Methods.GetUInt16NullableRaw : Methods.GetUInt16Raw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(uint))
        {
            var method = isNullable ? Methods.GetUInt32NullableRaw : Methods.GetUInt32Raw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(ulong))
        {
            var method = isNullable ? Methods.GetUInt64NullableRaw : Methods.GetUInt64Raw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(float))
        {
            var method = isNullable ? Methods.GetSingleNullableRaw : Methods.GetSingleRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(double))
        {
            var method = isNullable ? Methods.GetDoubleNullableRaw : Methods.GetDoubleRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (underlyingType == typeof(decimal))
        {
            var method = isNullable ? Methods.GetDecimalNullableRaw : Methods.GetDecimalRaw;
            return Expression.Call(reader, method, ordinalExpression);
        }

        if (isNullable)
        {
            var method = Methods.GetCustomNullableRaw.MakeGenericMethod(underlyingType);
            return Expression.Call(reader, method, ordinalExpression);
        }

        var customMethod = Methods.GetCustomRaw.MakeGenericMethod(underlyingType);
        return Expression.Call(reader, customMethod, ordinalExpression);
    }

    private static PropertyMapping[] GetPropertyMappings(Type type, CsvSchema schema)
    {
        var columns = schema.Columns;
        var byPropertyName = new Dictionary<string, (string columnName, int ordinal, string format, bool allowNull)>(columns.Count, StringComparer.Ordinal);
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            byPropertyName[column.PropertyName] = (column.Name, i, column.Format, column.AllowNull);
        }

        var formatByPropertyName = type.GetPropertiesWithCsvFormat().ToDictionary(x => x.property.Name, x => x.format, StringComparer.Ordinal);
        var properties = type.GetProperties();
        var mappings = new List<PropertyMapping>(properties.Length);
        foreach (var property in properties)
        {
            if (byPropertyName.TryGetValue(property.Name, out var match))
            {
                formatByPropertyName.TryGetValue(property.Name, out var format);
                mappings.Add(new PropertyMapping(property, match.columnName, match.ordinal, match.format ?? format, match.allowNull));
            }
        }

        return [.. mappings];
    }

    private static class Methods
    {
        public static readonly MethodInfo GetStringRaw = Get(nameof(CsvDataReader.GetStringRaw), 1);
        public static readonly MethodInfo GetBooleanRaw = Get(nameof(CsvDataReader.GetBooleanRaw), 2);
        public static readonly MethodInfo GetBooleanNullableRaw = Get(nameof(CsvDataReader.GetBooleanNullableRaw), 2);
        public static readonly MethodInfo GetDateTimeRaw = Get(nameof(CsvDataReader.GetDateTimeRaw), 2);
        public static readonly MethodInfo GetDateTimeNullableRaw = Get(nameof(CsvDataReader.GetDateTimeNullableRaw), 2);
        public static readonly MethodInfo GetDateTimeOffsetRaw = Get(nameof(CsvDataReader.GetDateTimeOffsetRaw), 2);
        public static readonly MethodInfo GetDateTimeOffsetNullableRaw = Get(nameof(CsvDataReader.GetDateTimeOffsetNullableRaw), 2);
        public static readonly MethodInfo GetGuidRaw = Get(nameof(CsvDataReader.GetGuidRaw), 1);
        public static readonly MethodInfo GetGuidNullableRaw = Get(nameof(CsvDataReader.GetGuidNullableRaw), 1);
        public static readonly MethodInfo GetCharRaw = Get(nameof(CsvDataReader.GetCharRaw), 1);
        public static readonly MethodInfo GetCharNullableRaw = Get(nameof(CsvDataReader.GetCharNullableRaw), 1);
        public static readonly MethodInfo GetByteRaw = Get(nameof(CsvDataReader.GetByteRaw), 1);
        public static readonly MethodInfo GetByteNullableRaw = Get(nameof(CsvDataReader.GetByteNullableRaw), 1);
        public static readonly MethodInfo GetSByteRaw = Get(nameof(CsvDataReader.GetSByteRaw), 1);
        public static readonly MethodInfo GetSByteNullableRaw = Get(nameof(CsvDataReader.GetSByteNullableRaw), 1);
        public static readonly MethodInfo GetInt16Raw = Get(nameof(CsvDataReader.GetInt16Raw), 1);
        public static readonly MethodInfo GetInt16NullableRaw = Get(nameof(CsvDataReader.GetInt16NullableRaw), 1);
        public static readonly MethodInfo GetInt32Raw = Get(nameof(CsvDataReader.GetInt32Raw), 1);
        public static readonly MethodInfo GetInt32NullableRaw = Get(nameof(CsvDataReader.GetInt32NullableRaw), 1);
        public static readonly MethodInfo GetInt64Raw = Get(nameof(CsvDataReader.GetInt64Raw), 1);
        public static readonly MethodInfo GetInt64NullableRaw = Get(nameof(CsvDataReader.GetInt64NullableRaw), 1);
        public static readonly MethodInfo GetUInt16Raw = Get(nameof(CsvDataReader.GetUInt16Raw), 1);
        public static readonly MethodInfo GetUInt16NullableRaw = Get(nameof(CsvDataReader.GetUInt16NullableRaw), 1);
        public static readonly MethodInfo GetUInt32Raw = Get(nameof(CsvDataReader.GetUInt32Raw), 1);
        public static readonly MethodInfo GetUInt32NullableRaw = Get(nameof(CsvDataReader.GetUInt32NullableRaw), 1);
        public static readonly MethodInfo GetUInt64Raw = Get(nameof(CsvDataReader.GetUInt64Raw), 1);
        public static readonly MethodInfo GetUInt64NullableRaw = Get(nameof(CsvDataReader.GetUInt64NullableRaw), 1);
        public static readonly MethodInfo GetSingleRaw = Get(nameof(CsvDataReader.GetSingleRaw), 1);
        public static readonly MethodInfo GetSingleNullableRaw = Get(nameof(CsvDataReader.GetSingleNullableRaw), 1);
        public static readonly MethodInfo GetDoubleRaw = Get(nameof(CsvDataReader.GetDoubleRaw), 1);
        public static readonly MethodInfo GetDoubleNullableRaw = Get(nameof(CsvDataReader.GetDoubleNullableRaw), 1);
        public static readonly MethodInfo GetDecimalRaw = Get(nameof(CsvDataReader.GetDecimalRaw), 1);
        public static readonly MethodInfo GetDecimalNullableRaw = Get(nameof(CsvDataReader.GetDecimalNullableRaw), 1);
        public static readonly MethodInfo GetCustomRaw = GetGeneric(nameof(CsvDataReader.GetCustomRaw), 1);
        public static readonly MethodInfo GetCustomNullableRaw = GetGeneric(nameof(CsvDataReader.GetCustomNullableRaw), 1);

        private static MethodInfo Get(string name, int parameterCount)
        {
            return typeof(CsvDataReader).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(m => m.Name == name && m.GetParameters().Length == parameterCount);
        }

        private static MethodInfo GetGeneric(string name, int parameterCount)
        {
            return typeof(CsvDataReader).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(m => m.Name == name && m.IsGenericMethodDefinition && m.GetParameters().Length == parameterCount);
        }
    }

    public static IEnumerable<T> AsEnumerable<T>(this IDataReader reader)
    {
        CsvSchema schema = reader is CsvDataReader r ? r.Schema : Schema.From<T>();
        var activator = GetActivator<T>(schema);
        while (reader.Read())
        {
            T item = activator(reader);
            yield return item;
        }
    }

    public static IEnumerable<dynamic> AsEnumerable(this IDataReader reader)
    {
        while (reader.Read())
        {
            yield return reader.ToExpando();
        }
    }
    public static IEnumerable<dynamic> AsEnumerable(this IDataReader reader, Type type)
    {
        CsvSchema schema = reader is CsvDataReader r ? r.Schema : Schema.From(type);
        var activator = GetActivator(type, schema);
        while (reader.Read())
        {
            var item = activator(reader);
            yield return item;
        }
    }

    internal static dynamic ToExpando(this IDataRecord rdr) => Dynamic.From(rdr.NameValues().ToDictionary(p => p.name, p => p.value));
    internal static IEnumerable<(string name, object value)> NameValues(this IDataRecord record)
    {
        for (var i = 0; i < record.FieldCount; i++) yield return (record.GetName(i), record[i]);
    }
    public static string[] GetFieldHeaders(this IDataReader reader)
        => reader.GetSchemaTable().Rows.OfType<DataRow>().Select(r => r["ColumnName"]).OfType<string>().ToArray();
}
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
namespace Net.Code.Csv;

public static class Extensions
{
    private static readonly Dictionary<ActivatorCacheKey, Func<CsvDataReader, object>> FastActivatorCache = new();

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
        var fastActivator = GetOrBuildFastActivator(type, schema, mappings, properties);

        if (fastActivator is null)
        {
            return slowActivator;
        }

        return record => record is CsvDataReader reader ? fastActivator(reader) : slowActivator(record);
    }

    private static Func<IDataRecord, object> BuildSlowActivator(Type type, ActivatorPropertyMapping[] mappings, PropertyInfo[] properties)
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

    private static Func<CsvDataReader, object> GetOrBuildFastActivator(Type type, CsvSchema schema, ActivatorPropertyMapping[] mappings, PropertyInfo[] properties)
    {
        if (mappings.Length == 0)
        {
            return null;
        }

        var key = ActivatorCacheKey.Create(type, schema, mappings);
        lock (FastActivatorCache)
        {
            if (FastActivatorCache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var built = TryBuildFastActivator(type, mappings, properties);
        if (built is null)
        {
            return null;
        }

        lock (FastActivatorCache)
        {
            FastActivatorCache[key] = built;
        }

        return built;
    }

    private static Func<CsvDataReader, object> TryBuildFastActivator(Type type, ActivatorPropertyMapping[] mappings, PropertyInfo[] properties)
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

    private static Expression BuildValueExpression(ParameterExpression reader, ActivatorPropertyMapping mapping)
    {
        var propertyType = mapping.Property.PropertyType;
        var underlyingType = propertyType.GetUnderlyingType();
        var isNullable = propertyType.IsNullableType();
        var ordinalExpression = Expression.Constant(mapping.SchemaOrdinal);

        if (underlyingType == typeof(string))
        {
            return Expression.Call(reader, Methods.GetStringRaw, ordinalExpression);
        }

        if (isNullable)
        {
            var method = Methods.GetSchemaNullableRaw.MakeGenericMethod(underlyingType);
            return Expression.Call(reader, method, ordinalExpression);
        }

        var schemaMethod = Methods.GetSchemaRaw.MakeGenericMethod(underlyingType);
        return Expression.Call(reader, schemaMethod, ordinalExpression);
    }

    private static ActivatorPropertyMapping[] GetPropertyMappings(Type type, CsvSchema schema)
        => ActivatorMapping.GetMappings(type, schema);

    private readonly record struct ActivatorCacheKey(Type Type, string Signature)
    {
        public static ActivatorCacheKey Create(Type type, CsvSchema schema, ActivatorPropertyMapping[] mappings)
        {
            // Signature captures schema + property/format mapping, so the cached activator is only reused when safe.
            var signature = ActivatorMapping.BuildSignature(schema, mappings);
            return new ActivatorCacheKey(type, signature);
        }
    }

    private static class Methods
    {
        public static readonly MethodInfo GetStringRaw = Get(nameof(CsvDataReader.GetStringRaw), 1);
        public static readonly MethodInfo GetSchemaRaw = GetGeneric(nameof(CsvDataReader.GetSchemaRaw), 1);
        public static readonly MethodInfo GetSchemaNullableRaw = GetGeneric(nameof(CsvDataReader.GetSchemaNullableRaw), 1);

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
        if (reader is CsvDataReader csvReader)
        {
            foreach (var item in TypedRowEnumerable.FromDataReader<T>(csvReader))
            {
                yield return item;
            }
            yield break;
        }

        CsvSchema schema = Schema.From<T>();
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

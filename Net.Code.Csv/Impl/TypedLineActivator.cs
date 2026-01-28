using System.Linq.Expressions;
using System.Reflection;

namespace Net.Code.Csv.Impl;

internal static class TypedLineActivator<T>
{
    private static readonly Dictionary<ActivatorCacheKey, Func<CsvLineSlice, T>> Cache = new();

    internal static Func<CsvLineSlice, T> Get(CsvSchema schema, CsvHeader header, Converter converter, bool useSchemaConverters)
    {
        schema ??= Schema.From<T>();
        var baseMappings = ActivatorMapping.GetMappings(typeof(T), schema);
        var mappings = GetPropertyMappings(baseMappings, header);
        var signature = ActivatorMapping.BuildSignature(schema, baseMappings);
        var key = ActivatorCacheKey.Create(signature, header, useSchemaConverters);

        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var properties = mappings.Select(m => m.Property).ToArray();
        var fast = TryBuildFastActivator(typeof(T), mappings, properties, converter, useSchemaConverters);
        var slow = fast is null ? BuildSlowActivator(typeof(T), mappings, properties, converter, useSchemaConverters) : null;
        var activator = fast ?? slow;

        lock (Cache)
        {
            Cache[key] = activator;
        }

        return activator;
    }

    private static PropertyMapping[] GetPropertyMappings(ActivatorPropertyMapping[] baseMappings, CsvHeader header)
    {
        var mappings = new PropertyMapping[baseMappings.Length];
        for (var i = 0; i < baseMappings.Length; i++)
        {
            var mapping = baseMappings[i];
            var sourceOrdinal = ResolveOrdinal(header, mapping.ColumnName, mapping.SchemaOrdinal);
            mappings[i] = new PropertyMapping(mapping.Property, sourceOrdinal, mapping.Format, mapping.Column);
        }
        return mappings;
    }

    private static int ResolveOrdinal(CsvHeader header, string columnName, int fallbackOrdinal)
        => header.TryGetIndex(columnName, out var index) ? index : fallbackOrdinal;

    private static Func<CsvLineSlice, T> BuildSlowActivator(
        Type type,
        PropertyMapping[] mappings,
        PropertyInfo[] properties,
        Converter converter,
        bool useSchemaConverters)
    {
        var constructor = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType))));

        if (constructor is null)
        {
            constructor = type.GetConstructor([]);
            return line =>
            {
                var item = constructor.Invoke([]);
                for (var i = 0; i < mappings.Length; i++)
                {
                    var value = GetValueObject(line, mappings[i], converter, useSchemaConverters);
                    mappings[i].Property.SetValue(item, value);
                }
                return (T)item;
            };
        }

        var values = new object[mappings.Length];
        return line =>
        {
            for (var i = 0; i < mappings.Length; i++)
            {
                values[i] = GetValueObject(line, mappings[i], converter, useSchemaConverters);
            }
            return (T)constructor.Invoke(values);
        };
    }

    private static object GetValueObject(CsvLineSlice line, PropertyMapping mapping, Converter converter, bool useSchemaConverters)
    {
        var propertyType = mapping.Property.PropertyType;
        var underlyingType = propertyType.GetUnderlyingType();
        var isNullable = propertyType.IsNullableType();

        if (underlyingType == typeof(string))
        {
            return TypedLineValueAccessor.GetString(line, mapping.SourceOrdinal);
        }

        if (useSchemaConverters)
        {
            return isNullable
                ? TypedLineSchemaAccessor.GetSchemaNullableValue(line, mapping.SourceOrdinal, mapping.Column, underlyingType)
                : TypedLineSchemaAccessor.GetSchemaValue(line, mapping.SourceOrdinal, mapping.Column, underlyingType);
        }

        return isNullable
            ? TypedLineValueAccessor.GetNullableObject(line, mapping.SourceOrdinal, converter, underlyingType, mapping.Format)
            : TypedLineValueAccessor.GetObject(line, mapping.SourceOrdinal, converter, underlyingType, mapping.Format);
    }

    private static Func<CsvLineSlice, T> TryBuildFastActivator(
        Type type,
        PropertyMapping[] mappings,
        PropertyInfo[] properties,
        Converter converter,
        bool useSchemaConverters)
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

        var lineParameter = Expression.Parameter(typeof(CsvLineSlice), "line");

        if (constructor is not null)
        {
            var arguments = new Expression[mappings.Length];
            for (var i = 0; i < mappings.Length; i++)
            {
                arguments[i] = BuildValueExpression(lineParameter, mappings[i], converter, useSchemaConverters);
            }

            var newExpression = Expression.New(constructor, arguments);
            return Expression.Lambda<Func<CsvLineSlice, T>>(newExpression, lineParameter).Compile();
        }

        var itemVariable = Expression.Variable(type, "item");
        var expressions = new List<Expression>
        {
            Expression.Assign(itemVariable, Expression.New(type))
        };

        foreach (var mapping in mappings)
        {
            var valueExpression = BuildValueExpression(lineParameter, mapping, converter, useSchemaConverters);
            var assignExpression = Expression.Assign(Expression.Property(itemVariable, mapping.Property), valueExpression);
            expressions.Add(assignExpression);
        }

        expressions.Add(itemVariable);
        var block = Expression.Block([itemVariable], expressions);
        return Expression.Lambda<Func<CsvLineSlice, T>>(block, lineParameter).Compile();
    }

    private static Expression BuildValueExpression(
        ParameterExpression line,
        PropertyMapping mapping,
        Converter converter,
        bool useSchemaConverters)
    {
        var propertyType = mapping.Property.PropertyType;
        var underlyingType = propertyType.GetUnderlyingType();
        var isNullable = propertyType.IsNullableType();
        var ordinalExpression = Expression.Constant(mapping.SourceOrdinal);

        if (underlyingType == typeof(string))
        {
            return Expression.Call(Methods.GetString, line, ordinalExpression);
        }

        if (useSchemaConverters)
        {
            var columnExpression = Expression.Constant(mapping.Column);
            if (isNullable)
            {
                var method = Methods.GetSchemaNullable.MakeGenericMethod(underlyingType);
                return Expression.Call(method, line, ordinalExpression, columnExpression);
            }

            var schemaMethod = Methods.GetSchemaValue.MakeGenericMethod(underlyingType);
            return Expression.Call(schemaMethod, line, ordinalExpression, columnExpression);
        }

        if (converter is null)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        var converterExpression = Expression.Constant(converter);
        var formatExpression = Expression.Constant(mapping.Format, typeof(string));

        if (underlyingType == typeof(bool))
        {
            var method = isNullable ? Methods.GetNullableBoolean : Methods.GetBoolean;
            return Expression.Call(method, line, ordinalExpression, converterExpression, formatExpression);
        }

        if (underlyingType == typeof(DateTime))
        {
            var method = isNullable ? Methods.GetNullableDateTime : Methods.GetDateTime;
            return Expression.Call(method, line, ordinalExpression, converterExpression, formatExpression);
        }

        if (underlyingType == typeof(DateTimeOffset))
        {
            var method = isNullable ? Methods.GetNullableDateTimeOffset : Methods.GetDateTimeOffset;
            return Expression.Call(method, line, ordinalExpression, converterExpression, formatExpression);
        }

        if (underlyingType == typeof(byte))
        {
            var method = isNullable ? Methods.GetNullableByte : Methods.GetByte;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(char))
        {
            var method = isNullable ? Methods.GetNullableChar : Methods.GetChar;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(Guid))
        {
            var method = isNullable ? Methods.GetNullableGuid : Methods.GetGuid;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(short))
        {
            var method = isNullable ? Methods.GetNullableInt16 : Methods.GetInt16;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(int))
        {
            var method = isNullable ? Methods.GetNullableInt32 : Methods.GetInt32;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(long))
        {
            var method = isNullable ? Methods.GetNullableInt64 : Methods.GetInt64;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(float))
        {
            var method = isNullable ? Methods.GetNullableSingle : Methods.GetSingle;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(double))
        {
            var method = isNullable ? Methods.GetNullableDouble : Methods.GetDouble;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(decimal))
        {
            var method = isNullable ? Methods.GetNullableDecimal : Methods.GetDecimal;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(ushort))
        {
            var method = isNullable ? Methods.GetNullableUInt16 : Methods.GetUInt16;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(uint))
        {
            var method = isNullable ? Methods.GetNullableUInt32 : Methods.GetUInt32;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(ulong))
        {
            var method = isNullable ? Methods.GetNullableUInt64 : Methods.GetUInt64;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        if (underlyingType == typeof(sbyte))
        {
            var method = isNullable ? Methods.GetNullableSByte : Methods.GetSByte;
            return Expression.Call(method, line, ordinalExpression, converterExpression);
        }

        var objectMethod = isNullable ? Methods.GetNullableObject.MakeGenericMethod(underlyingType) : Methods.GetObject.MakeGenericMethod(underlyingType);
        return Expression.Call(objectMethod, line, ordinalExpression, converterExpression);
    }

    private readonly record struct PropertyMapping(
        PropertyInfo Property,
        int SourceOrdinal,
        string Format,
        CsvColumn Column);

    private readonly record struct ActivatorCacheKey(string Signature, bool UseSchemaConverters)
    {
        public static ActivatorCacheKey Create(
            string signature,
            CsvHeader header,
            bool useSchemaConverters)
        {
            // Signature + header fields identify a stable mapping; avoids rebuilding activators across readers.
            var builder = new StringBuilder(signature.Length + (header.Fields.Length * 8) + 2);
            builder.Append(signature);
            builder.Append('#');
            for (var i = 0; i < header.Fields.Length; i++)
            {
                builder.Append(header.Fields[i])
                    .Append('|');
            }

            return new ActivatorCacheKey(builder.ToString(), useSchemaConverters);
        }
    }

    private static class Methods
    {
        public static readonly MethodInfo GetString = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetString), 2);

        public static readonly MethodInfo GetBoolean = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetBoolean), 4);
        public static readonly MethodInfo GetNullableBoolean = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableBoolean), 4);

        public static readonly MethodInfo GetDateTime = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetDateTime), 4);
        public static readonly MethodInfo GetNullableDateTime = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableDateTime), 4);

        public static readonly MethodInfo GetDateTimeOffset = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetDateTimeOffset), 4);
        public static readonly MethodInfo GetNullableDateTimeOffset = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableDateTimeOffset), 4);

        public static readonly MethodInfo GetByte = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetByte), 3);
        public static readonly MethodInfo GetNullableByte = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableByte), 3);
        public static readonly MethodInfo GetChar = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetChar), 3);
        public static readonly MethodInfo GetNullableChar = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableChar), 3);
        public static readonly MethodInfo GetGuid = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetGuid), 3);
        public static readonly MethodInfo GetNullableGuid = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableGuid), 3);
        public static readonly MethodInfo GetInt16 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetInt16), 3);
        public static readonly MethodInfo GetNullableInt16 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableInt16), 3);
        public static readonly MethodInfo GetInt32 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetInt32), 3);
        public static readonly MethodInfo GetNullableInt32 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableInt32), 3);
        public static readonly MethodInfo GetInt64 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetInt64), 3);
        public static readonly MethodInfo GetNullableInt64 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableInt64), 3);
        public static readonly MethodInfo GetSingle = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetSingle), 3);
        public static readonly MethodInfo GetNullableSingle = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableSingle), 3);
        public static readonly MethodInfo GetDouble = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetDouble), 3);
        public static readonly MethodInfo GetNullableDouble = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableDouble), 3);
        public static readonly MethodInfo GetDecimal = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetDecimal), 3);
        public static readonly MethodInfo GetNullableDecimal = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableDecimal), 3);
        public static readonly MethodInfo GetUInt16 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetUInt16), 3);
        public static readonly MethodInfo GetNullableUInt16 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableUInt16), 3);
        public static readonly MethodInfo GetUInt32 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetUInt32), 3);
        public static readonly MethodInfo GetNullableUInt32 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableUInt32), 3);
        public static readonly MethodInfo GetUInt64 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetUInt64), 3);
        public static readonly MethodInfo GetNullableUInt64 = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableUInt64), 3);
        public static readonly MethodInfo GetSByte = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetSByte), 3);
        public static readonly MethodInfo GetNullableSByte = Get(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableSByte), 3);

        public static readonly MethodInfo GetObject = GetGeneric(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetObject), 3);
        public static readonly MethodInfo GetNullableObject = GetGeneric(typeof(TypedLineValueAccessor), nameof(TypedLineValueAccessor.GetNullableObject), 3);

        public static readonly MethodInfo GetSchemaValue = GetGeneric(typeof(TypedLineSchemaAccessor), nameof(TypedLineSchemaAccessor.GetSchemaValue), 3);
        public static readonly MethodInfo GetSchemaNullable = GetGeneric(typeof(TypedLineSchemaAccessor), nameof(TypedLineSchemaAccessor.GetSchemaNullableValue), 3);

        private static MethodInfo Get(Type type, string name, int parameterCount)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .First(m => m.Name == name && m.GetParameters().Length == parameterCount);
        }

        private static MethodInfo GetGeneric(Type type, string name, int parameterCount)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .First(m => m.Name == name && m.IsGenericMethodDefinition && m.GetParameters().Length == parameterCount);
        }
    }
}

internal static class TypedLineValueAccessor
{
    internal static string GetString(CsvLineSlice line, int ordinal)
    {
        var field = line.GetField(ordinal);
        if (field.IsNull)
        {
            return null;
        }

        var span = field.Span;
        return span.Length == 0 ? null : span.ToString();
    }

    internal static bool GetBoolean(CsvLineSlice line, int ordinal, Converter converter, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return default;
        }

        return format is null ? converter.ToBoolean(span) : converter.ToBoolean(span, format);
    }

    internal static bool? GetNullableBoolean(CsvLineSlice line, int ordinal, Converter converter, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return null;
        }

        return format is null ? converter.ToBoolean(span) : converter.ToBoolean(span, format);
    }

    internal static byte GetByte(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToByte(span) : default;

    internal static byte? GetNullableByte(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToByte(span) : null;

    internal static char GetChar(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToChar(span) : default;

    internal static char? GetNullableChar(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToChar(span) : null;

    internal static Guid GetGuid(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToGuid(span) : default;

    internal static Guid? GetNullableGuid(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToGuid(span) : null;

    internal static short GetInt16(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToInt16(span) : default;

    internal static short? GetNullableInt16(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToInt16(span) : null;

    internal static int GetInt32(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToInt32(span) : default;

    internal static int? GetNullableInt32(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToInt32(span) : null;

    internal static long GetInt64(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToInt64(span) : default;

    internal static long? GetNullableInt64(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToInt64(span) : null;

    internal static float GetSingle(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToSingle(span) : default;

    internal static float? GetNullableSingle(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToSingle(span) : null;

    internal static double GetDouble(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToDouble(span) : default;

    internal static double? GetNullableDouble(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToDouble(span) : null;

    internal static decimal GetDecimal(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToDecimal(span) : default;

    internal static decimal? GetNullableDecimal(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToDecimal(span) : null;

    internal static ushort GetUInt16(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToUInt16(span) : default;

    internal static ushort? GetNullableUInt16(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToUInt16(span) : null;

    internal static uint GetUInt32(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToUInt32(span) : default;

    internal static uint? GetNullableUInt32(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToUInt32(span) : null;

    internal static ulong GetUInt64(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToUInt64(span) : default;

    internal static ulong? GetNullableUInt64(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToUInt64(span) : null;

    internal static sbyte GetSByte(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToSByte(span) : default;

    internal static sbyte? GetNullableSByte(CsvLineSlice line, int ordinal, Converter converter)
        => TryGetSpan(line, ordinal, out var span) ? converter.ToSByte(span) : null;

    internal static DateTime GetDateTime(CsvLineSlice line, int ordinal, Converter converter, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return default;
        }

        return format is null ? converter.ToDateTime(span) : converter.ToDateTime(span, format);
    }

    internal static DateTime? GetNullableDateTime(CsvLineSlice line, int ordinal, Converter converter, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return null;
        }

        return format is null ? converter.ToDateTime(span) : converter.ToDateTime(span, format);
    }

    internal static DateTimeOffset GetDateTimeOffset(CsvLineSlice line, int ordinal, Converter converter, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return default;
        }

        return converter.ToDateTimeOffset(span, format);
    }

    internal static DateTimeOffset? GetNullableDateTimeOffset(CsvLineSlice line, int ordinal, Converter converter, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return null;
        }

        return converter.ToDateTimeOffset(span, format);
    }

    internal static T GetObject<T>(CsvLineSlice line, int ordinal, Converter converter)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return default;
        }

        return (T)converter.FromSpan(typeof(T), span);
    }

    internal static T? GetNullableObject<T>(CsvLineSlice line, int ordinal, Converter converter) where T : struct
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return null;
        }

        return (T)converter.FromSpan(typeof(T), span);
    }

    internal static object GetObject(CsvLineSlice line, int ordinal, Converter converter, Type targetType, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        return ConvertUsingConverter(converter, targetType, span, format);
    }

    internal static object GetNullableObject(CsvLineSlice line, int ordinal, Converter converter, Type targetType, string format)
    {
        if (!TryGetSpan(line, ordinal, out var span))
        {
            return null;
        }

        return ConvertUsingConverter(converter, targetType, span, format);
    }

    private static object ConvertUsingConverter(Converter converter, Type targetType, ReadOnlySpan<char> span, string format)
    {
        if (targetType == typeof(bool))
        {
            return format is null ? converter.ToBoolean(span) : converter.ToBoolean(span, format);
        }
        if (targetType == typeof(DateTime))
        {
            return format is null ? converter.ToDateTime(span) : converter.ToDateTime(span, format);
        }
        if (targetType == typeof(DateTimeOffset))
        {
            return converter.ToDateTimeOffset(span, format);
        }
        if (targetType == typeof(byte))
        {
            return converter.ToByte(span);
        }
        if (targetType == typeof(char))
        {
            return converter.ToChar(span);
        }
        if (targetType == typeof(Guid))
        {
            return converter.ToGuid(span);
        }
        if (targetType == typeof(short))
        {
            return converter.ToInt16(span);
        }
        if (targetType == typeof(int))
        {
            return converter.ToInt32(span);
        }
        if (targetType == typeof(long))
        {
            return converter.ToInt64(span);
        }
        if (targetType == typeof(float))
        {
            return converter.ToSingle(span);
        }
        if (targetType == typeof(double))
        {
            return converter.ToDouble(span);
        }
        if (targetType == typeof(decimal))
        {
            return converter.ToDecimal(span);
        }
        if (targetType == typeof(ushort))
        {
            return converter.ToUInt16(span);
        }
        if (targetType == typeof(uint))
        {
            return converter.ToUInt32(span);
        }
        if (targetType == typeof(ulong))
        {
            return converter.ToUInt64(span);
        }
        if (targetType == typeof(sbyte))
        {
            return converter.ToSByte(span);
        }

        return converter.FromSpan(targetType, span);
    }

    private static bool TryGetSpan(CsvLineSlice line, int ordinal, out ReadOnlySpan<char> span)
    {
        var field = line.GetField(ordinal);
        if (field.IsNull)
        {
            span = default;
            return false;
        }

        span = field.Span;
        return span.Length > 0;
    }
}

internal static class TypedLineSchemaAccessor
{
    internal static T GetSchemaValue<T>(CsvLineSlice line, int ordinal, CsvColumn column)
    {
        var field = line.GetField(ordinal);
        var span = field.Span;
        if (field.IsNull || span.Length == 0)
        {
            return default;
        }

        return (T)column.FromSpan(span);
    }

    internal static T? GetSchemaNullableValue<T>(CsvLineSlice line, int ordinal, CsvColumn column) where T : struct
    {
        var field = line.GetField(ordinal);
        var span = field.Span;
        if (field.IsNull || span.Length == 0)
        {
            return null;
        }

        return (T)column.FromSpan(span);
    }

    internal static object GetSchemaValue(CsvLineSlice line, int ordinal, CsvColumn column, Type targetType)
    {
        var field = line.GetField(ordinal);
        var span = field.Span;
        if (field.IsNull || span.Length == 0)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        return column.FromSpan(span);
    }

    internal static object GetSchemaNullableValue(CsvLineSlice line, int ordinal, CsvColumn column, Type targetType)
    {
        var field = line.GetField(ordinal);
        var span = field.Span;
        if (field.IsNull || span.Length == 0)
        {
            return null;
        }

        return column.FromSpan(span);
    }
}

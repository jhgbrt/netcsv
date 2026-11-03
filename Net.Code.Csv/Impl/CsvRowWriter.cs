using System.Linq.Expressions;
using System.Reflection;

namespace Net.Code.Csv.Impl;

/// <summary>
/// A compiled row writer for <typeparamref name="T"/>: header names, the ordered properties (with their
/// [CsvFormat]) and a delegate that formats all property values of one item into a <see cref="CsvRowBuffer"/>.
/// </summary>
internal sealed class CsvRowWriter<T>
{
    private static readonly object Lock = new();
    private static CsvRowWriter<T> _forSchema;
    private static CsvRowWriter<T> _forProperties;

    private CsvRowWriter(string[] headers, (PropertyInfo property, string format)[] properties)
    {
        Headers = headers;
        Properties = properties;
        Write = Build(properties);
    }

    public string[] Headers { get; }
    public (PropertyInfo property, string format)[] Properties { get; }
    public Action<T, CsvRowBuffer> Write { get; }

    /// <summary>
    /// Columns as defined by <see cref="Schema.From{T}"/> (used by the IEnumerable based writer).
    /// </summary>
    public static CsvRowWriter<T> ForSchema()
    {
        lock (Lock)
        {
            if (_forSchema is null)
            {
                var schema = Schema.From<T>();
                var headers = schema.Columns.Select(c => c.Name).ToArray();
                _forSchema = new CsvRowWriter<T>(headers, GetPropertiesForSchema(schema));
            }
            return _forSchema;
        }
    }

    /// <summary>
    /// All public properties of <typeparamref name="T"/> (used by the IAsyncEnumerable based writer).
    /// </summary>
    public static CsvRowWriter<T> ForProperties()
    {
        lock (Lock)
        {
            if (_forProperties is null)
            {
                var properties = typeof(T).GetPropertiesWithCsvFormat().ToArray();
                var headers = properties.Select(p => p.property.Name).ToArray();
                _forProperties = new CsvRowWriter<T>(headers, properties);
            }
            return _forProperties;
        }
    }

    private static (PropertyInfo property, string format)[] GetPropertiesForSchema(CsvSchema schema)
    {
        var properties = typeof(T).GetPropertiesWithCsvFormat().ToArray();
        var lookup = new Dictionary<string, (PropertyInfo property, string format)>(properties.Length, StringComparer.Ordinal);
        foreach (var property in properties)
        {
            lookup[property.property.Name] = property;
        }

        var result = new (PropertyInfo property, string format)[schema.Columns.Count];
        var count = 0;
        foreach (var column in schema.Columns)
        {
            if (lookup.TryGetValue(column.PropertyName, out var property))
            {
                result[count++] = property;
            }
        }

        if (count != result.Length)
        {
            Array.Resize(ref result, count);
        }
        return result;
    }

    private static Action<T, CsvRowBuffer> Build((PropertyInfo property, string format)[] properties)
    {
        var item = Expression.Parameter(typeof(T), "item");
        var buffer = Expression.Parameter(typeof(CsvRowBuffer), "buffer");
        var variables = new List<ParameterExpression>();
        var body = new List<Expression>();

        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0)
            {
                body.Add(Expression.Call(buffer, Methods.Delimiter));
            }
            var (property, format) = properties[i];
            body.Add(BuildCell(item, buffer, property, format, variables));
        }

        if (body.Count == 0)
        {
            body.Add(Expression.Empty());
        }

        var block = Expression.Block(variables, body);
        return Expression.Lambda<Action<T, CsvRowBuffer>>(block, item, buffer).Compile();
    }

    private static Expression BuildCell(ParameterExpression item, ParameterExpression buffer, PropertyInfo property, string format, List<ParameterExpression> variables)
    {
        var getter = property.GetGetMethod(true);
        if (getter is null || property.GetIndexParameters().Length > 0)
        {
            // Not readable through a plain property access: keep the reflection behavior (including its exceptions).
            var value = Expression.Call(Expression.Constant(property), Methods.PropertyGetValue, Expression.Convert(item, typeof(object)));
            return Expression.Call(buffer, Methods.WriteObject, value, Expression.Constant(format, typeof(string)));
        }

        var propertyType = property.PropertyType;
        var access = WrapInvocationExceptions(Expression.Property(getter.IsStatic ? null : item, property));

        var underlyingType = Nullable.GetUnderlyingType(propertyType);
        if (underlyingType is not null)
        {
            var writeValue = TryGetNativeWriter(underlyingType, format);
            if (writeValue is null)
            {
                return WriteObject(buffer, access, format);
            }

            var variable = Expression.Variable(propertyType);
            variables.Add(variable);
            return Expression.Block(
                Expression.Assign(variable, access),
                Expression.IfThen(
                    Expression.Property(variable, "HasValue"),
                    writeValue(buffer, Expression.Call(variable, propertyType.GetMethod("GetValueOrDefault", Type.EmptyTypes)))));
        }

        var write = TryGetNativeWriter(propertyType, format);
        return write is null ? WriteObject(buffer, access, format) : write(buffer, access);
    }

    private static Expression WriteObject(ParameterExpression buffer, Expression value, string format)
        => Expression.Call(buffer, Methods.WriteObject, Expression.Convert(value, typeof(object)), Expression.Constant(format, typeof(string)));

    /// <summary>
    /// PropertyInfo.GetValue wraps exceptions thrown by the getter in a TargetInvocationException; keep doing so.
    /// </summary>
    private static Expression WrapInvocationExceptions(Expression access)
    {
        var ex = Expression.Variable(typeof(Exception), "ex");
        return Expression.TryCatch(
            access,
            Expression.Catch(ex, Expression.Throw(Expression.New(Methods.TargetInvocationExceptionCtor, ex), access.Type)));
    }

    /// <summary>
    /// Returns a factory for a direct (non-boxing) write of a value of the given type, or null if the
    /// type must go through <see cref="Converter.ToString(object, string)"/>.
    /// </summary>
    private static Func<Expression, Expression, Expression> TryGetNativeWriter(Type type, string format)
    {
        if (type == typeof(string)) return Call(nameof(CsvRowBuffer.WriteString));
        if (type == typeof(int)) return Call(nameof(CsvRowBuffer.WriteInt32));
        if (type == typeof(long)) return Call(nameof(CsvRowBuffer.WriteInt64));
        if (type == typeof(short)) return Call(nameof(CsvRowBuffer.WriteInt16));
        if (type == typeof(byte)) return Call(nameof(CsvRowBuffer.WriteByte));
        if (type == typeof(sbyte)) return Call(nameof(CsvRowBuffer.WriteSByte));
        if (type == typeof(ushort)) return Call(nameof(CsvRowBuffer.WriteUInt16));
        if (type == typeof(uint)) return Call(nameof(CsvRowBuffer.WriteUInt32));
        if (type == typeof(ulong)) return Call(nameof(CsvRowBuffer.WriteUInt64));
        if (type == typeof(float)) return Call(nameof(CsvRowBuffer.WriteSingle));
        if (type == typeof(double)) return Call(nameof(CsvRowBuffer.WriteDouble));
        if (type == typeof(decimal)) return Call(nameof(CsvRowBuffer.WriteDecimal));
        if (type == typeof(char)) return Call(nameof(CsvRowBuffer.WriteChar));
        if (type == typeof(Guid)) return Call(nameof(CsvRowBuffer.WriteGuid));
        if (type == typeof(DateTime)) return Call(nameof(CsvRowBuffer.WriteDateTime), format ?? "O");
        if (type == typeof(DateTimeOffset)) return Call(nameof(CsvRowBuffer.WriteDateTimeOffset), format ?? "O");
        if (type == typeof(TimeSpan)) return Call(nameof(CsvRowBuffer.WriteTimeSpan), format ?? "c");
        if (type == typeof(bool))
        {
            if (string.IsNullOrEmpty(format))
            {
                return Call(nameof(CsvRowBuffer.WriteBoolean));
            }
            var separator = format.IndexOf('|');
            if (separator <= 0 || separator == format.Length - 1)
            {
                // Invalid format: Converter throws a FormatException, but only when a (non-null) value is written.
                return null;
            }
            var @true = Expression.Constant(format.Substring(0, separator));
            var @false = Expression.Constant(format.Substring(separator + 1));
            return (buffer, value) => Expression.Call(buffer, Methods.WriteString, Expression.Condition(value, @true, @false));
        }
        return null;
    }

    private static Func<Expression, Expression, Expression> Call(string method)
    {
        var methodInfo = typeof(CsvRowBuffer).GetMethod(method);
        return (buffer, value) => Expression.Call(buffer, methodInfo, value);
    }

    private static Func<Expression, Expression, Expression> Call(string method, string format)
    {
        var methodInfo = typeof(CsvRowBuffer).GetMethod(method);
        var formatExpression = Expression.Constant(format, typeof(string));
        return (buffer, value) => Expression.Call(buffer, methodInfo, value, formatExpression);
    }

    private static class Methods
    {
        public static readonly MethodInfo Delimiter = typeof(CsvRowBuffer).GetMethod(nameof(CsvRowBuffer.Delimiter));
        public static readonly MethodInfo WriteString = typeof(CsvRowBuffer).GetMethod(nameof(CsvRowBuffer.WriteString));
        public static readonly MethodInfo WriteObject = typeof(CsvRowBuffer).GetMethod(nameof(CsvRowBuffer.WriteObject));
        public static readonly MethodInfo PropertyGetValue = typeof(PropertyInfo).GetMethod(nameof(PropertyInfo.GetValue), [typeof(object)]);
        public static readonly ConstructorInfo TargetInvocationExceptionCtor = typeof(TargetInvocationException).GetConstructor([typeof(Exception)]);
    }
}

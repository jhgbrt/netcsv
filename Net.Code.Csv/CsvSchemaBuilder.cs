using System.Reflection;

namespace Net.Code.Csv;

public record CsvSchema(IList<CsvColumn> Columns)
{
    public CsvColumn this[int i] => Columns[i];
    public string GetName(int i) => Columns[i].Name;
    public int GetOrdinal(string name)
    {
        for (var i = 0; i < Columns.Count; i++)
        {
            if (Columns[i].Name == name) return i;
        }
        return -1;
    }
}

public record struct CsvColumn(
    string Name,
    Type Type,
    Func<string, object> FromString,
    bool AllowNull);

public static class Schema
{
    public static CsvSchema From<T>() => new CsvSchemaBuilder().From<T>().Schema;
    public static CsvSchema[] From<T1, T2>() =>
    [
        new CsvSchemaBuilder().From<T1>().Schema,
        new CsvSchemaBuilder().From<T2>().Schema,
    ];
    public static CsvSchema[] From<T1, T2, T3>() =>
    [
        new CsvSchemaBuilder().From<T1>().Schema,
        new CsvSchemaBuilder().From<T2>().Schema,
        new CsvSchemaBuilder().From<T3>().Schema,
    ];
}

public class CsvSchemaBuilder(CultureInfo cultureInfo)
{
    private readonly List<CsvColumn> _columns = [];
    private readonly Converter _converter = new(cultureInfo ?? CultureInfo.InvariantCulture);

    public CsvSchemaBuilder() : this(CultureInfo.InvariantCulture)
    {
    }

    public CsvSchemaBuilder Add<T>(string name, Func<string, T> convert, bool allowNull)
    {
        _columns.Add(new (name, typeof(T), s => convert(s), allowNull));
        return this;
    }

    private CsvSchemaBuilder Add<T>(string name, string format, Func<string, string, T> convert, bool allowNull)
    {
        _columns.Add(new (name, typeof(T), s => convert(s, format), allowNull));
        return this;
    }


    private CsvSchemaBuilder Add(string name, Type type, bool allowNull)
    {
        _columns.Add(new (name, type, s => _converter.FromString(type, s), allowNull));
        return this;
    }

    public CsvSchemaBuilder AddString(string name, bool allowNull = false) => Add(name, s => s, allowNull);
    public CsvSchemaBuilder AddBoolean(string name, bool allowNull = false) => Add(name, _converter.ToBoolean, allowNull);
    public CsvSchemaBuilder AddBoolean(string name, string @true, string @false, bool allowNull = false) => Add(name, $"{@true}|{@false}", _converter.ToBoolean, allowNull);
    public CsvSchemaBuilder AddBoolean(string name, string format, bool allowNull = false) => Add(name, format, _converter.ToBoolean, allowNull);
    public CsvSchemaBuilder AddInt16(string name, bool allowNull = false) => Add(name, _converter.ToInt16, allowNull);
    public CsvSchemaBuilder AddInt32(string name, bool allowNull = false) => Add(name, _converter.ToInt32, allowNull);
    public CsvSchemaBuilder AddInt64(string name, bool allowNull = false) => Add(name, _converter.ToInt64, allowNull);
    public CsvSchemaBuilder AddUInt16(string name, bool allowNull = false) => Add(name, _converter.ToUInt16, allowNull);
    public CsvSchemaBuilder AddUInt32(string name, bool allowNull = false) => Add(name, _converter.ToUInt32, allowNull);
    public CsvSchemaBuilder AddUInt64(string name, bool allowNull = false) => Add(name, _converter.ToUInt64, allowNull);
    public CsvSchemaBuilder AddSingle(string name, bool allowNull = false) => Add(name, _converter.ToSingle, allowNull);
    public CsvSchemaBuilder AddDouble(string name, bool allowNull = false) => Add(name, _converter.ToDouble, allowNull);
    public CsvSchemaBuilder AddDecimal(string name, bool allowNull = false) => Add(name, _converter.ToDecimal, allowNull);
    public CsvSchemaBuilder AddChar(string name, bool allowNull = false) => Add(name, _converter.ToChar, allowNull);
    public CsvSchemaBuilder AddByte(string name, bool allowNull = false) => Add(name, _converter.ToByte, allowNull);
    public CsvSchemaBuilder AddSByte(string name, bool allowNull = false) => Add(name, _converter.ToSByte, allowNull);
    public CsvSchemaBuilder AddGuid(string name, bool allowNull = false) => Add(name, _converter.ToGuid, allowNull);
    public CsvSchemaBuilder AddDateTime(string name, string format = null, bool allowNull = false) => Add(name, format, _converter.ToDateTime, allowNull);
    public CsvSchemaBuilder AddDateTimeOffset(string name, string format = null, bool allowNull = false) => Add(name, format, _converter.ToDateTimeOffset, allowNull);

    public CsvSchemaBuilder From<T>()
    {
        var properties = typeof(T).GetPropertiesWithCsvFormat();
        foreach (var x in properties)
        {
            var (p, format) = x;
            bool allowNull = p.PropertyType.IsNullableType() || !p.PropertyType.IsValueType;
            var underlyingType = p.PropertyType.GetUnderlyingType();

            var columnName = p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>(false)?.Name ?? p.Name;
            var propertyName = p.Name;

            if (underlyingType == typeof(DateTime))
            {
                AddDateTime(columnName, format, allowNull);
            }
            else if (underlyingType == typeof(DateTimeOffset))
            {
                AddDateTimeOffset(columnName, format, allowNull);
            }
            else if (underlyingType == typeof(decimal))
            {
                AddDecimal(columnName, allowNull);
            }
            else if (underlyingType == typeof(double))
            {
                AddDouble(columnName, allowNull);
            }
            else if (underlyingType == typeof(float))
            {
                AddSingle(columnName, allowNull);
            }
            else if (underlyingType == typeof(long))
            {
                AddInt64(columnName, allowNull);
            }
            else if (underlyingType == typeof(int))
            {
                AddInt32(columnName, allowNull);
            }
            else if (underlyingType == typeof(short))
            {
                AddInt16(columnName, allowNull);
            }
            else if (underlyingType == typeof(byte))
            {
                AddByte(columnName, allowNull);
            }
            else if (underlyingType == typeof(sbyte))
            {
                AddSByte(columnName, allowNull);
            }
            else if (underlyingType == typeof(char))
            {
                AddChar(columnName, allowNull);
            }
            else if (underlyingType == typeof(ushort))
            {
                AddUInt16(columnName, allowNull);
            }
            else if (underlyingType == typeof(uint))
            {
                AddUInt32(columnName, allowNull);
            }
            else if (underlyingType == typeof(ulong))
            {
                AddUInt64(columnName, allowNull);
            }
            else if (underlyingType == typeof(ushort))
            {
                AddUInt16(columnName, allowNull);
            }
            else if (underlyingType == typeof(Guid))
            {
                AddGuid(columnName, allowNull);
            }
            else if (underlyingType == typeof(bool))
            {
                AddBoolean(columnName, format, allowNull);
            }
            else
            {
                Add(columnName, underlyingType, allowNull);
            }
        }
        return this;
    }

    public CsvSchema Schema => new([.. _columns]);
}


/// <summary>
/// Use this attribute to specify the format for boolean values (form: "True|False", e.g. "yes|no")
/// or DateTime or DateTimeOffset values (form: .Net format string)
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public class CsvFormatAttribute(string format) : Attribute
{
    public string Format => format;
}

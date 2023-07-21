using System.ComponentModel;

namespace Net.Code.Csv.Impl;

/// <summary>
/// String to primitive conversion class. By default, uses the Convert.ToXXX methods or,
/// if not available, the [Primitive].Parse method.
/// </summary>
class Converter(CultureInfo cultureInfo)
{
    private readonly CultureInfo _cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;

    public bool ToBoolean(string value) => Convert.ToBoolean(value, _cultureInfo);
    public bool ToBoolean(string value, string format)
    {
        var (@true, @false) = GetBooleanFormats(format);
        return value switch
        {
            string v when v == @true => true,
            string v when v == @false => false,
            _ => throw new FormatException($"Unrecognized value '{value}' for true/false. Expected {@true} or {@false}.")
        };
    }

    (string @true, string @false) GetBooleanFormats(string format)
    {
        var i = format.IndexOf('|');
        if (i <= 0 || i == format.Length - 1)
            throw new FormatException($"Invalid format string '{format}' for Boolean. Should be of the form \"[true]|[false]\", for example \"yes|no\"");
        var @true = format.Substring(0, format.IndexOf('|'));
        var @false = format.Substring(format.IndexOf('|') + 1);
        return (@true, @false);
    }
    public byte ToByte(string value) => Convert.ToByte(value, _cultureInfo);
    public char ToChar(string value) => Convert.ToChar(value, _cultureInfo);
    public DateTime ToDateTime(string value) => ToDateTime(value, null);
    public DateTime ToDateTime(string value, string format) => format switch
    {
        not null => DateTime.ParseExact(value, format, _cultureInfo),
        _ => DateTime.Parse(value, _cultureInfo)
    };
    public DateTimeOffset ToDateTimeOffset(string value, string format = null) => format switch
    {
        not null => DateTimeOffset.ParseExact(value, format, _cultureInfo),
        _ => DateTimeOffset.Parse(value, _cultureInfo)
    };
    public decimal ToDecimal(string value) => Convert.ToDecimal(value, _cultureInfo);
    public Guid ToGuid(string value) => Guid.Parse(value);
    public short ToInt16(string value) => Convert.ToInt16(value, _cultureInfo);
    public int ToInt32(string value) => Convert.ToInt32(value, _cultureInfo);
    public long ToInt64(string value) => Convert.ToInt64(value, _cultureInfo);
    public sbyte ToSByte(string value) => Convert.ToSByte(value, _cultureInfo);
    public float ToSingle(string value) => Convert.ToSingle(value, _cultureInfo);
    public double ToDouble(string value) => Convert.ToDouble(value, _cultureInfo);
    public ushort ToUInt16(string value) => Convert.ToUInt16(value, _cultureInfo);
    public uint ToUInt32(string value) => Convert.ToUInt32(value, _cultureInfo);
    public ulong ToUInt64(string value) => Convert.ToUInt64(value, _cultureInfo);
    public object FromString(Type destinationType, string value)
    {
        var converter = TypeDescriptor.GetConverter(destinationType);
        return converter.ConvertFromString(null, _cultureInfo, value);
    }
    public string ToString(object value, string format) => value switch
    {
        DateTime d => d.ToString(format ?? "O", _cultureInfo),
        DateTimeOffset d => d.ToString(format ?? "O", _cultureInfo),
        bool b => format switch
        {
            null or "" => b.ToString(),
            _ => b ? GetBooleanFormats(format).@true : GetBooleanFormats(format).@false
        },
        object o => TryToConvertToString(value) ?? Convert.ToString(o, _cultureInfo),
        null => string.Empty
    };

    internal string TryToConvertToString(object o)
    {
        var converter = TypeDescriptor.GetConverter(o);
        if (converter is not null && converter.CanConvertTo(typeof(string))) return converter.ConvertToString(null, _cultureInfo, o);
        return null;
    }
}

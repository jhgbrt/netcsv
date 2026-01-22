using System.ComponentModel;
using System.Globalization;

namespace Net.Code.Csv.Impl;

/// <summary>
/// String to primitive conversion class. By default, uses the Convert.ToXXX methods or,
/// if not available, the [Primitive].Parse method.
/// </summary>
class Converter(CultureInfo cultureInfo)
{
    private readonly CultureInfo _cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;

    public bool ToBoolean(ReadOnlySpan<char> value)
        => bool.Parse(value);
    public bool ToBoolean(ReadOnlySpan<char> value, string format)
    {
        var formatSpan = format.AsSpan();
        var (@true, @false) = GetBooleanFormats(formatSpan);
        if (value.SequenceEqual(formatSpan[@true]))
        {
            return true;
        }
        if (value.SequenceEqual(formatSpan[@false]))
        {
            return false;
        }

        throw new FormatException($"Unrecognized value '{value.ToString()}' for true/false. Expected {@true} or {@false}.");
    }

    (Range @true, Range @false) GetBooleanFormats(ReadOnlySpan<char> format)
    {
        var i = format.IndexOf('|');
        if (i <= 0 || i == format.Length - 1)
            throw new FormatException($"Invalid format string '{format.ToString()}' for Boolean. Should be of the form \"[true]|[false]\", for example \"yes|no\"");
        Range @true = 0..i;
        Range @false = (i + 1)..format.Length;
        return (@true, @false);
    }
    public byte ToByte(ReadOnlySpan<char> value) => byte.Parse(value, NumberStyles.Integer, _cultureInfo);
    public char ToChar(ReadOnlySpan<char> value)
    {
        if (value.Length == 1)
        {
            return value[0];
        }
        throw new FormatException("String must be exactly one character long.");
    }
    public DateTime ToDateTime(ReadOnlySpan<char> value) => ToDateTime(value, null);
    public DateTime ToDateTime(ReadOnlySpan<char> value, string format) => format switch
    {
        not null => DateTime.ParseExact(value, format, _cultureInfo),
        _ => DateTime.Parse(value, _cultureInfo)
    };
    public DateTimeOffset ToDateTimeOffset(ReadOnlySpan<char> value, string format = null) => format switch
    {
        not null => DateTimeOffset.ParseExact(value, format, _cultureInfo),
        _ => DateTimeOffset.Parse(value, _cultureInfo)
    };
    public decimal ToDecimal(ReadOnlySpan<char> value) => decimal.Parse(value, NumberStyles.Number, _cultureInfo);
    public Guid ToGuid(ReadOnlySpan<char> value) => Guid.Parse(value);
    public short ToInt16(ReadOnlySpan<char> value) => short.Parse(value, NumberStyles.Integer, _cultureInfo);
    public int ToInt32(ReadOnlySpan<char> value) => int.Parse(value, NumberStyles.Integer, _cultureInfo);
    public long ToInt64(ReadOnlySpan<char> value) => long.Parse(value, NumberStyles.Integer, _cultureInfo);
    public sbyte ToSByte(ReadOnlySpan<char> value) => sbyte.Parse(value, NumberStyles.Integer, _cultureInfo);
    public float ToSingle(ReadOnlySpan<char> value) => float.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, _cultureInfo);
    public double ToDouble(ReadOnlySpan<char> value) => double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, _cultureInfo);
    public ushort ToUInt16(ReadOnlySpan<char> value) => ushort.Parse(value, NumberStyles.Integer, _cultureInfo);
    public uint ToUInt32(ReadOnlySpan<char> value) => uint.Parse(value, NumberStyles.Integer, _cultureInfo);
    public ulong ToUInt64(ReadOnlySpan<char> value) => ulong.Parse(value, NumberStyles.Integer, _cultureInfo);
    public object FromSpan(Type destinationType, ReadOnlySpan<char> value)
    {
        var converter = TypeDescriptor.GetConverter(destinationType);
        return converter.ConvertFromString(null, _cultureInfo, value.ToString());
    }
    public string ToString(object value, string format) => value switch
    {
        DateTime d => d.ToString(format ?? "O", _cultureInfo),
        DateTimeOffset d => d.ToString(format ?? "O", _cultureInfo),
        bool b => format switch
        {
            null or "" => b.ToString(),
            _ =>
            b
                ? format[GetBooleanFormats(format.AsSpan()).@true]
                : format[GetBooleanFormats(format.AsSpan()).@false]
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

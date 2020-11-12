using System;
using System.ComponentModel;
using System.Globalization;

namespace Net.Code.Csv.Impl
{
    /// <summary>
    /// String to primitive conversion class. By default, uses the Convert.ToXXX methods or,
    /// if not available, the [Primitive].Parse method.
    /// </summary>
    class Converter
    {
        CultureInfo _cultureInfo;
        public Converter(CultureInfo cultureInfo)
        {
            _cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        public bool ToBoolean(string value) => Convert.ToBoolean(value, _cultureInfo);
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
        public object ToString(object value, string format) => value switch
        {
            DateTime d => d.ToString(format ?? "O", _cultureInfo),
            DateTimeOffset d => d.ToString(format ?? "O", _cultureInfo),
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
}
using System;
using System.ComponentModel;
using System.Globalization;

namespace Net.Code.Csv
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
       
        /// <summary>
        /// The default converter (uses the Convert.To[Primitive] methods when available or the [Primitive].Parse
        /// method otherwise.
        /// </summary>
        public static Converter Default => new Converter(CultureInfo.InvariantCulture);
        public bool ToBoolean(string value) => Convert.ToBoolean(value, _cultureInfo);
        public byte ToByte(string value) => Convert.ToByte(value, _cultureInfo);
        public char ToChar(string value) => Convert.ToChar(value, _cultureInfo);
        public DateTime ToDateTime(string value) => Convert.ToDateTime(value, _cultureInfo);
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
        internal object ToString(object value, string format) => value switch
        {
            DateTime d => d.ToString(format ?? "O", _cultureInfo),
            DateTimeOffset d => d.ToString(format ?? "O", _cultureInfo),
            object o => TypeDescriptor.GetConverter(o)?.ConvertToString(null, _cultureInfo, o) ?? Convert.ToString(o, _cultureInfo),
            null => string.Empty
        };
    }
}
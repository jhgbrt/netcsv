using System;

namespace Net.Code.Csv
{
    /// <summary>
    /// String to primitive conversion class. By default, uses the Convert.ToXXX methods or,
    /// if not available, the [Primitive].Parse method.
    /// </summary>
    public class Converter : IConverter
    {
        /// <summary>
        /// Conversion function from string to bool. Default = <see cref="Convert.ToBoolean(object)"/>
        /// </summary>
        public Func<string, bool> ToBoolean { get; set; } = Convert.ToBoolean;

        /// <summary>
        /// Conversion function from string to short. Default = <see cref="Convert.ToInt16(object)"/>
        /// </summary>
        public Func<string, short> ToInt16 { get; set; } = Convert.ToInt16;

        /// <summary>
        /// Conversion function from string to int. Default = <see cref="Convert.ToInt32(object)"/>
        /// </summary>
        public Func<string, int> ToInt32 { get; set; } = Convert.ToInt32;

        /// <summary>
        /// Conversion function from string to long. Default = <see cref="Convert.ToInt64(object)"/>
        /// </summary>
        public Func<string, long> ToInt64 { get; set; } = Convert.ToInt64;

        /// <summary>
        /// Conversion function from string to unsigned short. Default = <see cref="Convert.ToUInt16(object)"/>
        /// </summary>
        public Func<string, ushort> ToUInt16 { get; set; } = Convert.ToUInt16;

        /// <summary>
        /// Conversion function from string to unsigned int. Default = <see cref="Convert.ToUInt32(object)"/>
        /// </summary>
        public Func<string, uint> ToUInt32 { get; set; } = Convert.ToUInt32;

        /// <summary>
        /// Conversion function from string to unsigned long. Default = <see cref="Convert.ToUInt64(object)"/>
        /// </summary>
        public Func<string, ulong> ToUInt64 { get; set; } = Convert.ToUInt64;

        /// <summary>
        /// Conversion function from string to decimal. Default = <see cref="Convert.ToDecimal(object)"/>
        /// </summary>
        public Func<string, decimal> ToDecimal { get; set; } = Convert.ToDecimal;

        /// <summary>
        /// Conversion function from string to float. Default = <see cref="Convert.ToSingle(object)"/>
        /// </summary>
        public Func<string, float> ToSingle { get; set; } = Convert.ToSingle;

        /// <summary>
        /// Conversion function from string to double. Default = <see cref="Convert.ToDouble(object)"/>
        /// </summary>
        public Func<string, double> ToDouble { get; set; } = Convert.ToDouble;

        /// <summary>
        /// Conversion function from string to byte. Default = <see cref="Convert.ToByte(object)"/>
        /// </summary>
        public Func<string, byte> ToByte { get; set; } = Convert.ToByte;

        /// <summary>
        /// Conversion function from string to sbyte. Default = <see cref="Convert.ToByte(object)"/>
        /// </summary>
        public Func<string, sbyte> ToSByte { get; set; } = Convert.ToSByte;

        /// <summary>
        /// Conversion function from string to char. Default = <see cref="Convert.ToChar(object)"/>
        /// </summary>
        public Func<string, char> ToChar { get; set; } = Convert.ToChar;

        /// <summary>
        /// Conversion function from string to DateTime. Default = <see cref="Convert.ToDateTime(object)"/>
        /// </summary>
        public Func<string, DateTime> ToDateTime { get; set; } = Convert.ToDateTime;

        /// <summary>
        /// Conversion function from string to Guid. Default = <see cref="Guid.Parse(string)"/>
        /// </summary>
        public Func<string, Guid> ToGuid { get; set; } = Guid.Parse;

        /// <summary>
        /// The default converter (uses the Convert.To[Primitive] methods when available or the [Primitive].Parse
        /// method otherwise.
        /// </summary>
        public static Converter Default => new Converter();


        bool IConverter.ToBoolean(string value)
        {
            return ToBoolean(value);
        }

        byte IConverter.ToByte(string value)
        {
            return ToByte(value);
        }

        char IConverter.ToChar(string value)
        {
            return ToChar(value);
        }

        DateTime IConverter.ToDateTime(string value)
        {
            return ToDateTime(value);
        }

        decimal IConverter.ToDecimal(string value)
        {
            return ToDecimal(value);
        }

        Guid IConverter.ToGuid(string value)
        {
            return ToGuid(value);
        }

        short IConverter.ToInt16(string value)
        {
            return ToInt16(value);
        }

        int IConverter.ToInt32(string value)
        {
            return ToInt32(value);
        }

        long IConverter.ToInt64(string value)
        {
            return ToInt64(value);
        }

        sbyte IConverter.ToSByte(string value)
        {
            return ToSByte(value);
        }

        float IConverter.ToSingle(string value)
        {
            return ToSingle(value);
        }
        double IConverter.ToDouble(string value)
        {
            return ToDouble(value);
        }

        ushort IConverter.ToUInt16(string value)
        {
            return ToUInt16(value);
        }

        uint IConverter.ToUInt32(string value)
        {
            return ToUInt32(value);
        }
        ulong IConverter.ToUInt64(string value)
        {
            return ToUInt64(value);
        }
    }
}
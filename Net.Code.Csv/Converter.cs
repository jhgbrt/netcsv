using System;

namespace Net.Code.Csv
{
    /// <summary>
    /// String to primitive conversion class. By default, uses the Convert.ToXXX methods or,
    /// if not available, the [Primitive].Parse method.
    /// </summary>
    public class Converter
    {
        private Func<string, bool> _toBoolean = Convert.ToBoolean;
        private Func<string, short> _toInt16 = Convert.ToInt16;
        private Func<string, int> _toInt32 = Convert.ToInt32;
        private Func<string, long> _toInt64 = Convert.ToInt64;
        private Func<string, ushort> _toUInt16 = Convert.ToUInt16;
        private Func<string, uint> _toUInt32 = Convert.ToUInt32;
        private Func<string, ulong> _toUInt64 = Convert.ToUInt64;
        private Func<string, decimal> _toDecimal = Convert.ToDecimal;
        private Func<string, float> _toSingle = Convert.ToSingle;
        private Func<string, double> _toDouble = Convert.ToDouble;
        private Func<string, byte> _toByte = Convert.ToByte;
        private Func<string, sbyte> _toSByte = Convert.ToSByte;
        private Func<string, char> _toChar = Convert.ToChar;
        private Func<string, DateTime> _toDateTime = Convert.ToDateTime;
        private Func<string, Guid> _toGuid = Guid.Parse;

        /// <summary>
        /// Conversion function from string to bool. Default = <see cref="Convert.ToBoolean(object)"/>
        /// </summary>
        public Func<string, bool> ToBoolean
        {
            get { return _toBoolean; }
            set { _toBoolean = value; }
        }

        /// <summary>
        /// Conversion function from string to short. Default = <see cref="Convert.ToInt16(object)"/>
        /// </summary>
        public Func<string, short> ToInt16
        {
            get { return _toInt16; }
            set { _toInt16 = value; }
        }

        /// <summary>
        /// Conversion function from string to int. Default = <see cref="Convert.ToInt32(object)"/>
        /// </summary>
        public Func<string, int> ToInt32
        {
            get { return _toInt32; }
            set { _toInt32 = value; }
        }

        /// <summary>
        /// Conversion function from string to long. Default = <see cref="Convert.ToInt64(object)"/>
        /// </summary>
        public Func<string, long> ToInt64
        {
            get { return _toInt64; }
            set { _toInt64 = value; }
        }

        /// <summary>
        /// Conversion function from string to unsigned short. Default = <see cref="Convert.ToUInt16(object)"/>
        /// </summary>
        public Func<string, ushort> ToUInt16
        {
            get { return _toUInt16; }
            set { _toUInt16 = value; }
        }

        /// <summary>
        /// Conversion function from string to unsigned int. Default = <see cref="Convert.ToUInt32(object)"/>
        /// </summary>
        public Func<string, uint> ToUInt32
        {
            get { return _toUInt32; }
            set { _toUInt32 = value; }
        }

        /// <summary>
        /// Conversion function from string to unsigned long. Default = <see cref="Convert.ToUInt64(object)"/>
        /// </summary>
        public Func<string, ulong> ToUInt64
        {
            get { return _toUInt64; }
            set { _toUInt64 = value; }
        }

        /// <summary>
        /// Conversion function from string to decimal. Default = <see cref="Convert.ToDecimal(object)"/>
        /// </summary>
        public Func<string, decimal> ToDecimal
        {
            get { return _toDecimal; }
            set { _toDecimal = value; }
        }

        /// <summary>
        /// Conversion function from string to float. Default = <see cref="Convert.ToSingle(object)"/>
        /// </summary>
        public Func<string, float> ToSingle
        {
            get { return _toSingle; }
            set { _toSingle = value; }
        }

        /// <summary>
        /// Conversion function from string to double. Default = <see cref="Convert.ToDouble(object)"/>
        /// </summary>
        public Func<string, double> ToDouble
        {
            get { return _toDouble; }
            set { _toDouble = value; }
        }

        /// <summary>
        /// Conversion function from string to byte. Default = <see cref="Convert.ToByte(object)"/>
        /// </summary>
        public Func<string, byte> ToByte
        {
            get { return _toByte; }
            set { _toByte = value; }
        }

        /// <summary>
        /// Conversion function from string to sbyte. Default = <see cref="Convert.ToByte(object)"/>
        /// </summary>
        public Func<string, sbyte> ToSByte
        {
            get { return _toSByte; }
            set { _toSByte = value; }
        }

        /// <summary>
        /// Conversion function from string to char. Default = <see cref="Convert.ToChar(object)"/>
        /// </summary>
        public Func<string, char> ToChar
        {
            get { return _toChar; }
            set { _toChar = value; }
        }

        /// <summary>
        /// Conversion function from string to DateTime. Default = <see cref="Convert.ToDateTime(object)"/>
        /// </summary>
        public Func<string, DateTime> ToDateTime
        {
            get { return _toDateTime; }
            set { _toDateTime = value; }
        }

        /// <summary>
        /// Conversion function from string to Guid. Default = <see cref="Guid.Parse(string)"/>
        /// </summary>
        public Func<string, Guid> ToGuid
        {
            get { return _toGuid; }
            set { _toGuid = value; }
        }

        /// <summary>
        /// The default converter (uses the Convert.To[Primitive] methods when available or the [Primitive].Parse
        /// method otherwise.
        /// </summary>
        public static Converter Default
        {
            get { return new Converter(); }
        }
    }
}
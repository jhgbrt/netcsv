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

        public Func<string, bool> ToBoolean
        {
            get { return _toBoolean; }
            set { _toBoolean = value; }
        }

        public Func<string, short> ToInt16
        {
            get { return _toInt16; }
            set { _toInt16 = value; }
        }

        public Func<string, int> ToInt32
        {
            get { return _toInt32; }
            set { _toInt32 = value; }
        }

        public Func<string, long> ToInt64
        {
            get { return _toInt64; }
            set { _toInt64 = value; }
        }

        public Func<string, ushort> ToUInt16
        {
            get { return _toUInt16; }
            set { _toUInt16 = value; }
        }

        public Func<string, uint> ToUInt32
        {
            get { return _toUInt32; }
            set { _toUInt32 = value; }
        }

        public Func<string, ulong> ToUInt64
        {
            get { return _toUInt64; }
            set { _toUInt64 = value; }
        }

        public Func<string, decimal> ToDecimal
        {
            get { return _toDecimal; }
            set { _toDecimal = value; }
        }

        public Func<string, float> ToSingle
        {
            get { return _toSingle; }
            set { _toSingle = value; }
        }

        public Func<string, double> ToDouble
        {
            get { return _toDouble; }
            set { _toDouble = value; }
        }

        public Func<string, byte> ToByte
        {
            get { return _toByte; }
            set { _toByte = value; }
        }

        public Func<string, sbyte> ToSByte
        {
            get { return _toSByte; }
            set { _toSByte = value; }
        }

        public Func<string, char> ToChar
        {
            get { return _toChar; }
            set { _toChar = value; }
        }

        public Func<string, DateTime> ToDateTime
        {
            get { return _toDateTime; }
            set { _toDateTime = value; }
        }

        public Func<string, Guid> ToGuid
        {
            get { return _toGuid; }
            set { _toGuid = value; }
        }

        public static Converter Default
        {
            get { return new Converter(); }
        }
    }
}
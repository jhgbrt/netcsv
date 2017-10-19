using System;

namespace Net.Code.Csv
{
    public interface IConverter
    {
        bool ToBoolean(string value);
        byte ToByte(string value);
        char ToChar(string value);
        DateTime ToDateTime(string value);
        decimal ToDecimal(string value);
        Guid ToGuid(string value);
        short ToInt16(string value);
        int ToInt32(string value);
        long ToInt64(string value);
        sbyte ToSByte(string value);
        float ToSingle(string value);
        double ToDouble(string value);
        ushort ToUInt16(string value);
        uint ToUInt32(string value);
        ulong ToUInt64(string value);
    }
}
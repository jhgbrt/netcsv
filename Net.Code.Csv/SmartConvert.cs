
using System;
using System.Globalization;

namespace Net.Code.Csv
{
    public static class SmartConvert
    {
        public static bool ToBool(string s) => s switch
        {
            "yes" or "y" or "YES" or "Y" or "True" or "true" or "TRUE" or "1" => true,
            "no" or "n" or "NO" or "N" or "False" or "false" or "FALSE" or "0" => false,
            _ => throw new ArgumentException($"{nameof(s)} = {s} could not be converted to a boolean value")
        };
        public static DateTime ToDateTime(string s)
        {
            DateTime result;
            if (DateTime.TryParse(s, out result)) return result;
            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) return result;
            if (DateTime.TryParseExact(s, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) return result;
            if (DateTime.TryParseExact(s, "yyyy_MM_dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) return result;
            throw new ArgumentException($"{nameof(s)} = {s} could not be converted to a DateTime value");
        }
    }
}
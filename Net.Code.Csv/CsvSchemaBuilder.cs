
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

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
    public record CsvSchema(params CsvColumn[] Columns);
    public record CsvColumn(string Name, Type Type, Func<string, object> Convert);
    public class CsvSchemaBuilder
    {
        List<CsvColumn> _columns = new List<CsvColumn>();
        public CsvSchemaBuilder AddColumn<T>(string name, Func<string, T> convert)
        {
            _columns.Add(new CsvColumn(name, typeof(T), s => convert(s)));
            return this;
        }
        public CsvSchemaBuilder AddString(string name) => AddColumn(name, s => s);
        public CsvSchemaBuilder AddBoolean(string name, Func<string, bool> convert = null) => AddColumn(name, convert ?? Convert.ToBoolean);
        public CsvSchemaBuilder AddInt16(string name, Func<string, short> convert = null) => AddColumn(name, convert ?? Convert.ToInt16);
        public CsvSchemaBuilder AddInt32(string name, Func<string, int> convert = null) => AddColumn(name, convert ?? Convert.ToInt32);
        public CsvSchemaBuilder AddInt64(string name, Func<string, long> convert = null) => AddColumn(name, convert ?? Convert.ToInt64);
        public CsvSchemaBuilder AddUInt16(string name, Func<string, ushort> convert = null) => AddColumn(name, convert ?? Convert.ToUInt16);
        public CsvSchemaBuilder AddUInt32(string name, Func<string, uint> convert = null) => AddColumn(name, convert ?? Convert.ToUInt32);
        public CsvSchemaBuilder AddUInt64(string name, Func<string, ulong> convert = null) => AddColumn(name, convert ?? Convert.ToUInt64);
        public CsvSchemaBuilder AddSingle(string name, Func<string, float> convert = null) => AddColumn(name, convert ?? Convert.ToSingle);
        public CsvSchemaBuilder AddDouble(string name, Func<string, double> convert = null) => AddColumn(name, convert ?? Convert.ToDouble);
        public CsvSchemaBuilder AddChar(string name, Func<string, char> convert = null) => AddColumn(name, convert ?? Convert.ToChar);
        public CsvSchemaBuilder AddByte(string name, Func<string, byte> convert = null) => AddColumn(name, convert ?? Convert.ToByte);
        public CsvSchemaBuilder AddSByte(string name, Func<string, sbyte> convert = null) => AddColumn(name, convert ?? Convert.ToSByte);
        public CsvSchemaBuilder AddGuid(string name, Func<string, Guid> convert = null) => AddColumn(name, convert ?? Guid.Parse);
        public CsvSchemaBuilder AddDecimal(string name, Func<string, decimal> convert = null) => AddColumn(name, convert ?? Convert.ToDecimal);
        public CsvSchemaBuilder AddDateTime(string name, Func<string, DateTime> convert = null) => AddColumn(name, convert ?? Convert.ToDateTime);
        public CsvSchema Schema => new CsvSchema(_columns.ToArray());
    }

    public static class Extensions
    {
        public static IEnumerable<T> As<T>(this IDataReader reader)
        {
            while (reader.Read())
            {
                var values = from prop in typeof(T).GetProperties()
                             select reader.GetValue(reader.GetOrdinal(prop.Name));
                T item = (T)Activator.CreateInstance(typeof(T), values.ToArray());
                yield return item;
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Net.Code.Csv
{
    public record CsvSchema(params CsvColumn[] Columns);
    public record CsvColumn(string Name, string PropertyName, Type Type, Func<string, object> Convert);
    public class CsvSchemaBuilder
    {
        List<CsvColumn> _columns = new List<CsvColumn>();
        public CsvSchemaBuilder AddColumn<T>(string name, Func<string, T> convert)
        {
            _columns.Add(new CsvColumn(name, name, typeof(T), s => convert(s)));
            return this;
        }
        public CsvSchemaBuilder AddColumn<T>(string name, PropertyInfo property, Func<string, T> convert)
        {
            _columns.Add(new CsvColumn(name, property.Name, typeof(T), s => convert(s)));
            return this;
        }
        public CsvSchemaBuilder AddColumn<T>(string name, PropertyInfo property, Func<string, object> convert)
        {
            _columns.Add(new CsvColumn(name, property.Name, typeof(T), s => convert(s)));
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
        public CsvSchemaBuilder From<T>()
        {
            foreach (var p in typeof(T).GetProperties())
            {
                if (p.PropertyType == typeof(DateTime) && p.GetCustomAttribute<CsvFormatAttribute>()?.Format is string format)
                {
                    AddDateTime(p.Name, s => DateTime.ParseExact(s, format, DateTimeFormatInfo.InvariantInfo));
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(p.PropertyType);
                    _columns.Add(new CsvColumn(p.Name, p.Name, p.PropertyType, s => converter.ConvertFromString(s)));
                }
            }
            return this;
        }
        public CsvSchema Schema => new CsvSchema(_columns.ToArray());
    }

    public class CsvFormatAttribute : Attribute
    {
        public string Format { get; set; }
        public CsvFormatAttribute(string format)
        {
            Format = format;
        }
    }

}
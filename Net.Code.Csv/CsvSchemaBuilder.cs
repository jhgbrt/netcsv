
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Net.Code.Csv
{
    public record CsvSchema(params CsvColumn[] Columns)
    {
        public CsvColumn this[int i] => Columns[i];
    }
    
    public record CsvColumn(string Name, string PropertyName, Type Type, Func<string, object> Convert, bool AllowNull);
    
    public class CsvSchemaBuilder
    {
        public CsvSchemaBuilder() : this(CultureInfo.InvariantCulture)
        {
        }
        public CsvSchemaBuilder(CultureInfo cultureInfo) 
        {
            _cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }


        List<CsvColumn> _columns = new List<CsvColumn>();
        private CultureInfo _cultureInfo;

        public CsvSchemaBuilder Add<T>(string name, Func<string, T> convert, bool allowNull)
        {
            _columns.Add(new CsvColumn(name, name, typeof(T), s => convert(s), allowNull));
            return this;
        }
        public CsvSchemaBuilder AddString(string name, bool allowNull = false) => Add(name, s => s, allowNull);
        public CsvSchemaBuilder AddBoolean(string name, bool allowNull = false) => Add(name, Convert.ToBoolean, allowNull);
        public CsvSchemaBuilder AddBoolean(string name, string @true, string @false, bool allowNull = false) 
            => Add(name, s => s switch 
            {
                string v when v == @true => true,
                string v when v == @false => false, 
                _ => throw new FormatException($"Unrecognized value '{s}' for true/false. Expected {@true} or {@false}.") 
            }, allowNull);
        public CsvSchemaBuilder AddInt16(string name, bool allowNull = false) => Add(name, Convert.ToInt16, allowNull);
        public CsvSchemaBuilder AddInt32(string name, bool allowNull = false) => Add(name, Convert.ToInt32, allowNull);
        public CsvSchemaBuilder AddInt64(string name, bool allowNull = false) => Add(name, Convert.ToInt64, allowNull);
        public CsvSchemaBuilder AddUInt16(string name, bool allowNull = false) => Add(name, Convert.ToUInt16, allowNull);
        public CsvSchemaBuilder AddUInt32(string name, bool allowNull = false) => Add(name, Convert.ToUInt32, allowNull);
        public CsvSchemaBuilder AddUInt64(string name, bool allowNull = false) => Add(name, Convert.ToUInt64, allowNull);
        public CsvSchemaBuilder AddSingle(string name, bool allowNull = false) => Add(name, ToSingle, allowNull);
        private float ToSingle(string s) => Convert.ToSingle(s, _cultureInfo.NumberFormat);
        public CsvSchemaBuilder AddDouble(string name, bool allowNull = false) => Add(name, ToDouble, allowNull);
        private double ToDouble(string s) => Convert.ToDouble(s, _cultureInfo.NumberFormat);
        public CsvSchemaBuilder AddDecimal(string name, bool allowNull = false) => Add(name, ToDecimal, allowNull);
        private decimal ToDecimal(string s) => Convert.ToDecimal(s, _cultureInfo.NumberFormat);
        public CsvSchemaBuilder AddChar(string name, bool allowNull = false) => Add(name, Convert.ToChar, allowNull);
        public CsvSchemaBuilder AddByte(string name, bool allowNull = false) => Add(name, Convert.ToByte, allowNull);
        public CsvSchemaBuilder AddSByte(string name, bool allowNull = false) => Add(name, Convert.ToSByte, allowNull);
        public CsvSchemaBuilder AddGuid(string name, bool allowNull = false) => Add(name, Guid.Parse, allowNull);
        public CsvSchemaBuilder AddDateTime(string name, string format = null, bool allowNull = false) => format switch
        {
            not null => Add(name, s => DateTime.ParseExact(s, format, _cultureInfo.DateTimeFormat), allowNull),
            _ => Add(name, s => DateTime.Parse(s, _cultureInfo.DateTimeFormat), allowNull)
        };

        public CsvSchemaBuilder AddDateTimeOffset(string name, string format = null, bool allowNull = false) => format switch
        {
            not null => Add(name, s => DateTimeOffset.ParseExact(s, format, _cultureInfo.DateTimeFormat), allowNull),
            _ => Add(name, s => DateTimeOffset.Parse(s, _cultureInfo.DateTimeFormat), allowNull)
        };

        public CsvSchemaBuilder From<T>()
        {
            var properties = typeof(T).GetPropertiesWithCsvFormat();
            foreach (var x in properties)
            {
                var (p, format) = x;
                bool allowNull = p.PropertyType.IsNullableType() || !p.PropertyType.IsValueType;
                var underlyingType = p.PropertyType.GetUnderlyingType();
                if (underlyingType == typeof(DateTime))
                {
                    AddDateTime(p.Name, format, allowNull);
                }
                else if (underlyingType == typeof(DateTimeOffset))
                {
                    AddDateTimeOffset(p.Name, format, allowNull);
                }
                else if (underlyingType == typeof(decimal))
                {
                    AddDecimal(p.Name, allowNull);
                }
                else if (underlyingType == typeof(double))
                {
                    AddDouble(p.Name, allowNull);
                }
                else if (underlyingType == typeof(float))
                {
                    AddSingle(p.Name, allowNull);
                }
                else if (underlyingType == typeof(long))
                {
                    AddInt64(p.Name, allowNull);
                }
                else if (underlyingType == typeof(int))
                {
                    AddInt32(p.Name, allowNull);
                }
                else if (underlyingType == typeof(short))
                {
                    AddInt16(p.Name, allowNull);
                }
                else if (underlyingType == typeof(sbyte))
                {
                    AddSByte(p.Name, allowNull);
                }
                else if (underlyingType == typeof(char))
                {
                    AddChar(p.Name, allowNull);
                }
                else if (underlyingType == typeof(ushort))
                {
                    AddUInt16(p.Name, allowNull);
                }
                else if (underlyingType == typeof(uint))
                {
                    AddUInt32(p.Name, allowNull);
                }
                else if (underlyingType == typeof(ulong))
                {
                    AddUInt64(p.Name, allowNull);
                }
                else if (underlyingType == typeof(ushort))
                {
                    AddUInt16(p.Name, allowNull);
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(p.PropertyType);
                    _columns.Add(new CsvColumn(p.Name, p.Name, p.PropertyType, s => converter.ConvertFromString(null, _cultureInfo, s), allowNull));
                }
            }
            return this;
        }

        public CsvSchema Schema => new(_columns.ToArray());
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
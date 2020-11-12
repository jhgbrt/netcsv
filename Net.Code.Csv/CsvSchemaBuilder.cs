
using Net.Code.Csv.Impl;

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Net.Code.Csv
{
    public record CsvSchema(IList<CsvColumn> Columns)
    {
        public CsvColumn this[int i] => Columns[i];
    }
    
    public record CsvColumn(
        string Name, 
        string PropertyName, 
        Type Type, 
        Func<string, object> FromString, 
        bool AllowNull);
    
    public class CsvSchemaBuilder
    {
        public CsvSchemaBuilder() : this(CultureInfo.InvariantCulture)
        {
        }
        public CsvSchemaBuilder(CultureInfo cultureInfo) 
        {
            _converter = new Converter(cultureInfo ?? CultureInfo.InvariantCulture);
        }

        List<CsvColumn> _columns = new List<CsvColumn>();
        private Converter _converter;

        public CsvSchemaBuilder Add<T>(string name, Func<string, T> convert, bool allowNull)
        {
            _columns.Add(new CsvColumn(name, name, typeof(T), s => convert(s), allowNull));
            return this;
        }
        private CsvSchemaBuilder Add<T>(string name, string format, Func<string, string, T> convert, bool allowNull)
        {
            _columns.Add(new CsvColumn(name, name, typeof(T), s => convert(s, format), allowNull));
            return this;
        }
        private CsvSchemaBuilder Add(string name, Type type, bool allowNull)
        {
            _columns.Add(new CsvColumn(name, name, type, s => _converter.FromString(type, s), allowNull));
            return this;
        }
        public CsvSchemaBuilder AddString(string name, bool allowNull = false) => Add(name, s => s, allowNull);
        public CsvSchemaBuilder AddBoolean(string name, bool allowNull = false) => Add(name, _converter.ToBoolean, allowNull);
        public CsvSchemaBuilder AddBoolean(string name, string @true, string @false, bool allowNull = false) 
            => Add(name, $"{@true}|{@false}", _converter.ToBoolean, allowNull);
        public CsvSchemaBuilder AddBoolean(string name, string format, bool allowNull = false)
            => Add(name, format, _converter.ToBoolean, allowNull);
        public CsvSchemaBuilder AddInt16(string name, bool allowNull = false) => Add(name, _converter.ToInt16, allowNull);
        public CsvSchemaBuilder AddInt32(string name, bool allowNull = false) => Add(name, _converter.ToInt32, allowNull);
        public CsvSchemaBuilder AddInt64(string name, bool allowNull = false) => Add(name, _converter.ToInt64, allowNull);
        public CsvSchemaBuilder AddUInt16(string name, bool allowNull = false) => Add(name, _converter.ToUInt16, allowNull);
        public CsvSchemaBuilder AddUInt32(string name, bool allowNull = false) => Add(name, _converter.ToUInt32, allowNull);
        public CsvSchemaBuilder AddUInt64(string name, bool allowNull = false) => Add(name, _converter.ToUInt64, allowNull);
        public CsvSchemaBuilder AddSingle(string name, bool allowNull = false) => Add(name, _converter.ToSingle, allowNull);
        public CsvSchemaBuilder AddDouble(string name, bool allowNull = false) => Add(name, _converter.ToDouble, allowNull);
        public CsvSchemaBuilder AddDecimal(string name, bool allowNull = false) => Add(name, _converter.ToDecimal, allowNull);
        public CsvSchemaBuilder AddChar(string name, bool allowNull = false) => Add(name, _converter.ToChar, allowNull);
        public CsvSchemaBuilder AddByte(string name, bool allowNull = false) => Add(name, _converter.ToByte, allowNull);
        public CsvSchemaBuilder AddSByte(string name, bool allowNull = false) => Add(name, _converter.ToSByte, allowNull);
        public CsvSchemaBuilder AddGuid(string name, bool allowNull = false) => Add(name, _converter.ToGuid, allowNull);
        public CsvSchemaBuilder AddDateTime(string name, string format = null, bool allowNull = false) => Add(name, format, _converter.ToDateTime, allowNull);
        public CsvSchemaBuilder AddDateTimeOffset(string name, string format = null, bool allowNull = false) => Add(name, format, _converter.ToDateTimeOffset, allowNull);
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
                else if (underlyingType == typeof(bool))
                {
                    AddBoolean(p.Name, format, allowNull);
                }
                else
                {
                    Add(p.Name, underlyingType, allowNull);
                }
            }
            return this;
        }

        public CsvSchema Schema => new(_columns.ToArray());
    }

    /// <summary>
    /// Use this attribute to specify the format for boolean values (form: "True|False", e.g. "yes|no")
    /// or DateTime or DateTimeOffset values (form: .Net format string)
    /// </summary>
    public class CsvFormatAttribute : Attribute
    {
        public string Format { get; set; }
        public CsvFormatAttribute(string format)
        {
            Format = format;
        }
    }

}
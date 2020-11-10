using Net.Code.Csv;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;

Console.WriteLine(SmartConvert.ToBool("yes"));
Console.WriteLine(SmartConvert.ToBool("1"));
Console.WriteLine(SmartConvert.ToBool("Y"));
Console.WriteLine(SmartConvert.ToBool("true"));
Console.WriteLine(SmartConvert.ToDateTime("2020-11-15"));
Console.WriteLine(SmartConvert.ToDateTime("20201115"));
Console.WriteLine(SmartConvert.ToDateTime("2020_11_15"));

var schema = new CsvSchemaBuilder()
    .AddString(nameof(MyItem.First))
    .AddColumn(nameof(MyItem.Last), s => new Custom(s))
    .AddDateTime(nameof(MyItem.BirthDate), SmartConvert.ToDateTime)
    .AddInt32(nameof(MyItem.Quantity))
    .AddDecimal(nameof(MyItem.Price), s => decimal.Parse(s.Replace(",", "."), CultureInfo.InvariantCulture))
    .Schema;

Convert.ToString(new DateTime(), new MyFormatProvider());


var schema2 = new CsvSchemaBuilder().From<MyItem>().Schema;

using var reader = ReadCsv.FromFile(
    "test.csv",
    encoding: Encoding.UTF8,
    delimiter: ';',
    hasHeaders: true,
    schema: schema2
);

foreach (var item in reader.AsEnumerable<MyItem>())
    Console.WriteLine(item);

class MyFormatProvider : IFormatProvider
{
    public object GetFormat(Type formatType) => formatType == typeof(DateTimeFormatInfo) ? this : null;
}

public class CustomTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string s) return new Custom(s);
        return base.ConvertFrom(context, culture, value);
    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is Custom c) return c.Value;
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(CustomTypeConverter))]
public struct Custom
{
    public Custom(string value) { Value = value; }
    public string Value { get; set; }
    public override string ToString() => Value;
}

public record MyItem(string First, Custom Last, DateTime BirthDate, int Quantity, decimal Price);

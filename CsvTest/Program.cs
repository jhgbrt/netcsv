using Net.Code.Csv;
using System.ComponentModel;
using System.Globalization;
using System.Text;

//record Person(string FirstName, string LastName, [CsvFormat("yyyy-MM-dd")] DateTime BirthDate);

//var people = new[] { new Person("John", "Peterson", new DateTime(1980, 5, 14)) };

//WriteCsv.ToFile("out.csv", people);

var be = CultureInfo.CreateSpecificCulture("be");

var amount = Amount.Parse("$ 123.58", CultureInfo.InvariantCulture);
Console.WriteLine(amount.ToString(CultureInfo.InvariantCulture));
var converter = TypeDescriptor.GetConverter(typeof(Amount));
Console.WriteLine(converter.ConvertToString(null, be, amount));
Console.WriteLine(converter.ConvertFrom(null, be, "$ 123,58"));

var schema = new CsvSchemaBuilder()
    .AddString(nameof(MyItem.First))
    .Add(nameof(MyItem.Last), s => new Custom(s), true)
    .AddDateTime(nameof(MyItem.BirthDate), "yyyyMMdd")
    .AddInt32(nameof(MyItem.Quantity))
    .AddDecimal(nameof(MyItem.Price))
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

[TypeConverter(typeof(AmountConverter))]
public struct Amount
{
    public string Currency { get; set; }
    public decimal Value { get; set; }
    public static Amount Parse(string s, IFormatProvider provider)
    {
        var parts = s.Split(' ');
        var currency = parts[0];
        var decimalValue = decimal.Parse(parts[1], provider);
        return new Amount { Currency = currency, Value = decimalValue } ;
    }
    public string ToString(IFormatProvider provider)
    {
        return $"{Currency} {Value.ToString(provider)}";
    }

    public override string ToString() => ToString(CultureInfo.CurrentCulture);
}

public class AmountConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(Amount);
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string s) return Amount.Parse(s, culture);
        return base.ConvertFrom(context, culture, value);
    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is Amount a && destinationType == typeof(string)) return a.ToString(culture);
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

public record MyItem(string First, Custom Last, DateTime BirthDate, int Quantity, decimal Price);

using Net.Code.Csv;

using System;
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

using var reader = ReadCsv.FromFile(
    "test.csv",
    encoding: Encoding.UTF8,
    delimiter: ';',
    hasHeaders: true,
    schema: schema
);

foreach (var item in reader.As<MyItem>())
    Console.WriteLine(item);


public struct Custom
{
    public Custom(string value) { Value = value; }
    public string Value { get; set; }
    public override string ToString() => Value;
}

public record MyItem(string First, Custom Last, DateTime BirthDate, int Quantity, decimal Price);
//{
//    public string First { get; set; }
//    public Custom Last { get; set; }
//    public DateTime BirthDate { get; set; }
//    public int Quantity { get; set; }
//    public decimal Price { get; set; }
//    public override string ToString() => $"{First} {Last} {BirthDate} {Quantity} {Price}";
//}

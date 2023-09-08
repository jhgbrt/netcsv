
using System.IO;
using System.Threading.Tasks;

using static System.Environment;

namespace Net.Code.Csv.Tests.Unit.Csv;

[TestFixture]
public class WriteCsvTests
{
    private readonly string expected =
            $"First;Last;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset;IsActive;NullableCustom{NewLine}" +
            $"John;Peters;19701115;123;US$ 5,98;;2147483647;2020-11-13T10:20:30.0000000+02:00;yes;{NewLine}";
    private readonly MyClass[] classItems = new[]
    {
                new MyClass
                {
                    First = "John",
                    Last = new Custom("Peters"),
                    BirthDate = new DateTime(1970,11,15),
                    Quantity = 123,
                    Price = new Amount("US$", 5.98m) ,
                    Count = null,
                    LargeValue = int.MaxValue,
                    SomeDateTimeOffset = new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)),
                    IsActive = true
                }
            };

    private async IAsyncEnumerable<MyRecord> GetRecordsAsync()
    {
        foreach (var item in recordItems)
        {
            await Task.Yield();
            yield return item;
        }
    }
    private readonly MyRecord[] recordItems = new[]
    {
                new MyRecord(
                    First: "John",
                    Last: new Custom("Peters"),
                    BirthDate: new DateTime(1970,11,15),
                    Quantity: 123,
                    Price: new Amount("US$", 5.98m),
                    Count: null,
                    LargeValue: int.MaxValue,
                    SomeDateTimeOffset: new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)),
                    IsActive: true
                    )
            };


    [Test]
    public void WriteCsv_Class_ToString()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("be");
        var result = WriteCsv.ToString(classItems, ';', '"', '\\', true, cultureInfo: cultureInfo);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void WriteCsv_Record_ToString()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("be");
        var result = WriteCsv.ToString(recordItems, ';', '"', '\\', true, cultureInfo: cultureInfo);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void WriteCsv_Record_WithCultureInfo_ToString()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("be");
        var result = WriteCsv.ToString(recordItems, ';', '"', '\\', true, cultureInfo: cultureInfo);
        Assert.AreEqual(expected, result);
    }
    [Test]
    public async Task WriteCsv_ToStream_Record_WithCultureInfo_ToString()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("be");
        var stream = new MemoryStream();
        await WriteCsv.ToStream(GetRecordsAsync(), stream, delimiter: ';', quote: '"', escape: '\\', hasHeaders: true, cultureInfo: cultureInfo);
        stream.Position = 0;
        var sr = new StreamReader(stream);
        var result = sr.ReadToEnd();
        Assert.AreEqual(expected, result);
    }

    class Item { public decimal Value { get; set; } }
    [Test]
    public void WriteCsv_ReadBack_ReturnsExpectedResult()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("be");
        var schema = new CsvSchemaBuilder(cultureInfo).From<Item>().Schema;
        var result = WriteCsv.ToString(new[] { new Item { Value = 123.5m } }, cultureInfo: cultureInfo);
        Assert.AreEqual($"\"123,5\"{NewLine}", result);
        var readback = ReadCsv.FromString(result, schema: schema).AsEnumerable<Item>().Single().Value;
        Assert.AreEqual(123.5m, readback);
    }

    [Test]
    public void BooleanValue_SerializedAsYesOrNo_WhenYes_IsTrue()
    {
        var data = "yes";
        var reader = ReadCsv.FromString(data, schema: new CsvSchemaBuilder().AddBoolean("BooleanColumn", "yes", "no", false).Schema);
        reader.Read();
        Assert.AreEqual(true, reader[0]);
    }
    [Test]
    public void BooleanValue_SerializedAsYesOrNo_WhenNo_IsFalse()
    {
        var data = "no";
        var reader = ReadCsv.FromString(data, schema: new CsvSchemaBuilder().AddBoolean("BooleanColumn", "yes", "no", false).Schema);
        reader.Read();
        Assert.AreEqual(false, reader[0]);
    }
    [Test]
    public void BooleanValue_SerializedAsYesOrNo_WhenInvalid_Throws()
    {
        var data = "foo";
        var reader = ReadCsv.FromString(data, schema: new CsvSchemaBuilder().AddBoolean("BooleanColumn", "yes", "no", false).Schema);
        reader.Read();
        Assert.Throws<FormatException>(() => _ = reader[0]);
    }

    class ItemWithBoolean
    {
        [CsvFormat("yes|no")]
        public bool BooleanProperty { get; set; }
    }
    [Test]
    public void BooleanValue_True_CanBeSerializedAsYesNo()
    {
        var expected = $"yes{NewLine}";
        var result = WriteCsv.ToString(new[] { new ItemWithBoolean { BooleanProperty = true } });
        Assert.AreEqual(expected, result);
    }

}
[TestFixture]
public class WriteCsvByDefaultConformsToRFC4180
{
    [Test]
    public void Fields_Containing_Quote_Should_Be_Quoted_And_Quotes_In_Field_Should_Be_Escaped()
    {
        var result = WriteCsv.ToString(new[] { new { Value = "abc\"def" } });
        Assert.AreEqual($"\"abc\"\"def\"{NewLine}", result);
    }

    [Test]
    public void Fields_Containing_Delimiter_Should_Be_Quoted()
    {
        var result = WriteCsv.ToString(new[] { new { Value = "abc,def" } });
        Assert.AreEqual($"\"abc,def\"{NewLine}", result);
    }
    [Test]
    public void WriteCsv_StringWithQuotesAndDelimiters_GetsQuotedAndEscapesQuotes()
    {
        var result = WriteCsv.ToString(new[] { new { Value = "ab\"c,def" } });
        Assert.AreEqual($"\"ab\"\"c,def\"{NewLine}", result);
    }
    [Test]
    public void Fields_Containing_LineBreak_Should_Be_Quoted()
    {
        var result = WriteCsv.ToString(new[] { new { Value = "abc\r\ndef" } });
        Assert.AreEqual($"\"abc\r\ndef\"{NewLine}", result);
    }
}

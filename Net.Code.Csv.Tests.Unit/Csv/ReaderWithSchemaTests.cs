using NUnit.Framework;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace System.Runtime.CompilerServices { public class IsExternalInit { } }

namespace Net.Code.Csv.Tests.Unit.Csv
{
    [TestFixture]
    public class WriteCsvTests
    {
        [Test]
        public void WriteCsv_ToString()
        {
            var items = new[]
            {
                new MyClass{First = "first", Last = new Custom("last"), BirthDate = new DateTime(1970,11,15), Quantity = 123, Price = 5.98m }
            };

            var result = WriteCsv.ToString(items, ';', '"', '\\', true);

            Assert.AreEqual("First;Last;BirthDate;Quantity;Price\r\n\"first\";last;19701115;123;5.98\r\n", result);
        }
    }

    [TestFixture]
    public class ReaderWithSchemaTests
    {


        [Test]
        public void WhenSchemaIsManuallyCreated_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder()
                .AddString(nameof(MyRecord.First))
                .AddColumn(nameof(MyRecord.Last), s => new Custom(s))
                .AddDateTime(nameof(MyRecord.BirthDate), SmartConvert.ToDateTime)
                .AddInt32(nameof(MyRecord.Quantity))
                .AddDecimal(nameof(MyRecord.Price), s => decimal.Parse(s.Replace(",", "."), CultureInfo.InvariantCulture))
                .Schema;

            string input = 
                "First;Last;BirthDate;Quantity;Price\r\n" +
                "John;Peters;19700523;123;5.89";

            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .AsEnumerable<MyRecord>()
                .Single();

            Assert.AreEqual("John", item.First);
            Assert.AreEqual("Peters", item.Last.Value);
            Assert.AreEqual(new DateTime(1970,5,23), item.BirthDate);
            Assert.AreEqual(123, item.Quantity);
            Assert.AreEqual(5.89m, item.Price);
        }

        [Test]
        public void WhenSchemaCreatedFromRecord_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder().From<MyRecord>().Schema;

            string input = 
                "First;Last;BirthDate;Quantity;Price\r\n" +
                "John;Peters;1970-05-23;123;5.89";
            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .AsEnumerable<MyRecord>()
                .Single();

            Assert.AreEqual("John", item.First);
            Assert.AreEqual("Peters", item.Last.Value);
            Assert.AreEqual(new DateTime(1970, 5, 23), item.BirthDate);
            Assert.AreEqual(123, item.Quantity);
            Assert.AreEqual(5.89m, item.Price);

        }
        [Test]
        public void WhenSchemaCreatedFromClass_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder().From<MyClass>().Schema;

            string input = 
                "First;Last;BirthDate;Quantity;Price\r\n" +
                "John;Peters;19700523;123;5.89";

            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .AsEnumerable<MyRecord>()
                .Single();

            Assert.AreEqual("John", item.First);
            Assert.AreEqual("Peters", item.Last.Value);
            Assert.AreEqual(new DateTime(1970, 5, 23), item.BirthDate);
            Assert.AreEqual(123, item.Quantity);
            Assert.AreEqual(5.89m, item.Price);

        }
    }
    public class CustomTypeConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new Custom((string)value);
    }

    [TypeConverter(typeof(CustomTypeConverter))]
    public struct Custom
    {
        public Custom(string value) { Value = value; }
        public string Value { get; set; }
        public override string ToString() => Value;
    }

    public record MyRecord(string First, Custom Last, DateTime BirthDate, int Quantity, decimal Price);

    public class MyClass
    {
        public string First { get; set; }
        public Custom Last { get; set; }
        [CsvFormat("yyyyMMdd")]
        public DateTime BirthDate { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

}

using NUnit.Framework;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace System.Runtime.CompilerServices { public class IsExternalInit { } }

namespace Net.Code.Csv.Tests.Unit.Csv
{

    [TestFixture]
    public class ReaderWithSchemaTests
    {
        string input =
                "First;Last;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset\r\n" +
                "\"John\";Peters;19701115;123;5.98;;2147483647;2020-11-13T10:20:30.0000000+02:00\r\n";

        static void Verify(IMyItem item)
        {
            Assert.AreEqual("John", item.First);
            Assert.AreEqual("Peters", item.Last.Value.Value);
            Assert.AreEqual(new DateTime(1970, 11, 15), item.BirthDate);
            Assert.AreEqual(123, item.Quantity);
            Assert.AreEqual(5.98m, item.Price);
            Assert.AreEqual(null, item.Count);
            Assert.AreEqual(new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)), item.SomeDateTimeOffset);

        }

        [Test]
        public void WhenSchemaIsManuallyCreated_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder()
                .AddString(nameof(MyRecord.First))
                .Add(nameof(MyRecord.Last), s => new Custom(s), true)
                .AddDateTime(nameof(MyRecord.BirthDate), "yyyyMMdd")
                .AddInt32(nameof(MyRecord.Quantity))
                .AddDecimal(nameof(MyRecord.Price))
                .AddInt16(nameof(MyRecord.Count))
                .AddDecimal(nameof(MyRecord.LargeValue))
                .AddDateTimeOffset(nameof(MyRecord.SomeDateTimeOffset))
                .Schema;


            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .AsEnumerable<MyRecord>()
                .Single();

            Verify(item);
        }

        [Test]
        public void WhenSchemaCreatedFromRecord_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder().From<MyRecord>().Schema;

            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .AsEnumerable<MyRecord>()
                .Single();

            Verify(item);
        }
        [Test]
        public void WhenSchemaCreatedFromClass_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder().From<MyClass>().Schema;

            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .AsEnumerable<MyClass>()
                .Single();

            Verify(item);
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
    public interface IMyItem
    {
        public string First { get; }
        public Custom? Last { get; }
        public DateTime BirthDate { get; }
        public int Quantity { get; }
        public decimal Price { get; }
        public int? Count { get; }
        public decimal LargeValue { get; }
        public DateTimeOffset SomeDateTimeOffset { get; }

    }
    public record MyRecord (
        string First, 
        Custom? Last, 
        [CsvFormat("yyyyMMdd")]DateTime BirthDate, 
        int Quantity, 
        decimal Price, 
        int? Count,
        decimal LargeValue,
        DateTimeOffset SomeDateTimeOffset) : IMyItem;

    public class MyClass : IMyItem
    {
        public string First { get; set; }
        public Custom? Last { get; set; }
        [CsvFormat("yyyyMMdd")]
        public DateTime BirthDate { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int? Count { get; set; }
        public decimal LargeValue { get; set; }
        public DateTimeOffset SomeDateTimeOffset { get; set;}
    }

}

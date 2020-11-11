using NUnit.Framework;

using System;
using System.Globalization;
using System.Linq;

namespace Net.Code.Csv.Tests.Unit.Csv
{
    [TestFixture]
    public class WriteCsvTests
    {
        string expected =
                "First;Last;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset\r\n" +
                "\"John\";Peters;19701115;123;5.98;;2147483647;2020-11-13T10:20:30.0000000+02:00\r\n";
        MyClass[] classItems = new[]
        {
                new MyClass
                {
                    First = "John",
                    Last = new Custom("Peters"),
                    BirthDate = new DateTime(1970,11,15),
                    Quantity = 123,
                    Price = 5.98m ,
                    Count = null,
                    LargeValue = int.MaxValue,
                    SomeDateTimeOffset = new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2))
                }
            };

        MyRecord[] recordItems = new[]
        {
                new MyRecord(
                    First: "John",
                    Last: new Custom("Peters"),
                    BirthDate: new DateTime(1970,11,15),
                    Quantity: 123,
                    Price: 5.98m,
                    Count: null,
                    LargeValue: int.MaxValue,
                    SomeDateTimeOffset: new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2))
                    )
            };


        [Test]
        public void WriteCsv_Class_ToString()
        {
            var result = WriteCsv.ToString(classItems, ';', '"', '\\', true);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void WriteCsv_Record_ToString()
        {
            var result = WriteCsv.ToString(recordItems, ';', '"', '\\', true);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void WriteCsv_Record_WithCultureInfo_ToString()
        {
            var cultureInfo = CultureInfo.CreateSpecificCulture("be");
            // be uses ',' as decimal separator, which can be overriden like so:
            cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
            var result = WriteCsv.ToString(recordItems, ';', '"', '\\', true, cultureInfo);
            Assert.AreEqual(expected, result);
        }

        class Item { public decimal Value { get; set; } }
        [Test]
        public void WriteCsv_()
        {
            var cultureInfo = CultureInfo.CreateSpecificCulture("be");
            var schema = new CsvSchemaBuilder(cultureInfo).From<Item>().Schema;
            var result = WriteCsv.ToString(new[] { new Item { Value = 123.5m } }, delimiter: ';', cultureInfo: cultureInfo);
            Assert.AreEqual("123,5\r\n", result);
            var readback = ReadCsv.FromString("\"123,5\"\r\n", schema: schema).AsEnumerable<Item>().Single().Value;
            Assert.AreEqual(123.5m, readback);
        }

    }

}

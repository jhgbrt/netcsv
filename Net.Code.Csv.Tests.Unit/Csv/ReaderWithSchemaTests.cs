using NUnit.Framework;

using System;
using System.Globalization;
using System.Linq;
namespace System.Runtime.CompilerServices { public class IsExternalInit { } }

namespace Net.Code.Csv.Tests.Unit.Csv
{
    [TestFixture]
    public class ReaderWithSchemaTests
    {
        string input = @"First;Last;BirthDate;Quantity;Price
John;Peters;19700523;123;5.89";

        CsvSchema schema = new CsvSchemaBuilder()
            .AddString(nameof(MyItem.First))
            .AddColumn(nameof(MyItem.Last), s => new Custom(s))
            .AddDateTime(nameof(MyItem.BirthDate), SmartConvert.ToDateTime)
            .AddInt32(nameof(MyItem.Quantity))
            .AddDecimal(nameof(MyItem.Price), s => decimal.Parse(s.Replace(",", "."), CultureInfo.InvariantCulture))
            .Schema;

        [Test]
        public void CanCreateRecord()
        {
            var item = ReadCsv
                .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
                .As<MyItem>()
                .Single();

            Assert.AreEqual("John", item.First);
            Assert.AreEqual("Peters", item.Last.Value);
            Assert.AreEqual(new DateTime(1970,5,23), item.BirthDate);
            Assert.AreEqual(123, item.Quantity);
            Assert.AreEqual(5.89m, item.Price);
        }
    }
    public struct Custom
    {
        public Custom(string value) { Value = value; }
        public string Value { get; set; }
        public override string ToString() => Value;
    }
    public record MyItem(string First, Custom Last, DateTime BirthDate, int Quantity, decimal Price);

}

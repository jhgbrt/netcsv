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
                "First;Last;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset;IsActive\r\n" +
                "\"John\";Peters;19701115;123;US$ 5.98;;2147483647;2020-11-13T10:20:30.0000000+02:00;yes\r\n";

        static void Verify(IMyItem item)
        {
            Assert.AreEqual("John", item.First);
            Assert.AreEqual("Peters", item.Last.Value.Value);
            Assert.AreEqual(new DateTime(1970, 11, 15), item.BirthDate);
            Assert.AreEqual(123, item.Quantity);
            Assert.AreEqual(new Amount("US$", 5.98m), item.Price);
            Assert.AreEqual(null, item.Count);
            Assert.AreEqual(new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)), item.SomeDateTimeOffset);
            Assert.IsTrue(item.IsActive);
        }

        [Test]
        public void WhenSchemaIsManuallyCreated_ExpectedValuesAreReturned()
        {
            CsvSchema schema = new CsvSchemaBuilder()
                .AddString(nameof(MyRecord.First))
                .Add(nameof(MyRecord.Last), s => new Custom(s), true)
                .AddDateTime(nameof(MyRecord.BirthDate), "yyyyMMdd")
                .AddInt32(nameof(MyRecord.Quantity))
                .Add(nameof(MyRecord.Price), s => Amount.Parse(s, CultureInfo.InvariantCulture), false)
                .AddInt16(nameof(MyRecord.Count))
                .AddDecimal(nameof(MyRecord.LargeValue))
                .AddDateTimeOffset(nameof(MyRecord.SomeDateTimeOffset))
                .AddBoolean(nameof(MyRecord.IsActive), "yes", "no")
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
        public void WhenSchemaImplicitlyCreatedFromRecord_ExpectedValuesAreReturned()
        {
            var item = ReadCsv
                .FromString<MyRecord>(input, delimiter: ';', hasHeaders: true)
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
        [Test]
        public void WhenSchemaImplicitlyCreatedFromClass_ExpectedValuesAreReturned()
        {
            var item = ReadCsv
                .FromString<MyClass>(input, delimiter: ';', hasHeaders: true)
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
        public Amount Price { get; }
        public int? Count { get; }
        public decimal LargeValue { get; }
        public DateTimeOffset SomeDateTimeOffset { get; }
        public bool IsActive { get; }

    }
    public record MyRecord (
        string First, 
        Custom? Last, 
        [CsvFormat("yyyyMMdd")]DateTime BirthDate, 
        int Quantity, 
        Amount Price, 
        int? Count,
        decimal LargeValue,
        DateTimeOffset SomeDateTimeOffset,
        [CsvFormat("yes|no")]bool IsActive) : IMyItem;

    public class MyClass : IMyItem
    {
        public string First { get; set; }
        public Custom? Last { get; set; }
        [CsvFormat("yyyyMMdd")]
        public DateTime BirthDate { get; set; }
        public int Quantity { get; set; }
        public Amount Price { get; set; }
        public int? Count { get; set; }
        public decimal LargeValue { get; set; }
        public DateTimeOffset SomeDateTimeOffset { get; set;}
        [CsvFormat("yes|no")]
        public bool IsActive { get; set; }
    }

    [TypeConverter(typeof(AmountConverter))]
    public struct Amount
    {
        public Amount(string currency, decimal value)
        {
            Currency = currency;
            Value = value;
        }
        public string Currency { get; set; }
        public decimal Value { get; set; }
        public static Amount Parse(string s, IFormatProvider provider)
        {
            var parts = s.Split(' ');
            var currency = parts[0];
            var decimalValue = decimal.Parse(parts[1], provider);
            return new Amount { Currency = currency, Value = decimalValue };
        }
        public string ToString(IFormatProvider provider)
        {
            return $"{Currency} {Value.ToString(provider)}";
        }

        public override string ToString() => ToString(CultureInfo.CurrentCulture);
    }

    public class AmountConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
            => sourceType == typeof(string) || sourceType == typeof(Amount);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) 
            => destinationType == typeof(string) || destinationType == typeof(Amount);
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
}

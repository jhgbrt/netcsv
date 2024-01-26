using System.ComponentModel;

namespace Net.Code.Csv.Tests.Unit.Csv;


public class ReadCsvWithSchemaTests
{
    readonly string input =
            """
            First;Last;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset;IsActive;NullableCustom
            "John";Peters;19701115;123;US$ 5.98;;2147483647;2020-11-13T10:20:30.0000000+02:00;yes;
            """;

    static void Verify(IMyItem item)
    {
        Assert.Equal("John", item.First);
        Assert.Equal("Peters", item.Last.Value);
        Assert.Equal(new DateTime(1970, 11, 15), item.BirthDate);
        Assert.Equal(123, item.Quantity);
        Assert.Equal(new Amount("US$", 5.98m), item.Price);
        Assert.Null(item.Count);
        Assert.Equal(new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)), item.SomeDateTimeOffset);
        Assert.True(item.IsActive);
        Assert.Null(item.NullableCustom);
    }
    static void Verify(dynamic item)
    {
        Assert.Equal("John", item.First);
        Assert.Equal("Peters", item.Last.Value);
        Assert.Equal(new DateTime(1970, 11, 15), item.BirthDate);
        Assert.Equal(123, item.Quantity);
        Assert.Equal(new Amount("US$", 5.98m), item.Price);
        Assert.Null(item.Count);
        Assert.Equal(new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)), item.SomeDateTimeOffset);
        Assert.True(item.IsActive);
        Assert.Null(item.NullableCustom);
    }


    [Fact]
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
            .Add(nameof(MyRecord.NullableCustom), s => new Custom(s), true)
            .Schema;


        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<MyRecord>()
            .Single();

        Verify(item);
    }

    [Fact]
    public void WithSchema_CanReturnAsDynamicAndAllPropertiesAreTyped()
    {
        CsvSchema schema = new CsvSchemaBuilder()
            .AddString(nameof(MyRecord.First))
            .Add(nameof(MyRecord.Last), s => new Custom(s), false)
            .AddDateTime(nameof(MyRecord.BirthDate), "yyyyMMdd")
            .AddInt32(nameof(MyRecord.Quantity))
            .Add(nameof(MyRecord.Price), s => Amount.Parse(s, CultureInfo.InvariantCulture), false)
            .AddInt16(nameof(MyRecord.Count))
            .AddDecimal(nameof(MyRecord.LargeValue))
            .AddDateTimeOffset(nameof(MyRecord.SomeDateTimeOffset))
            .AddBoolean(nameof(MyRecord.IsActive), "yes", "no")
            .Add(nameof(MyRecord.NullableCustom), s => new Custom(s), true)
            .Schema;

        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable()
            .Single();

        Verify(item);
    }

    [Fact]
    public void WithoutSchema_AllPropertiesAreStrings()
    {
        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true)
            .AsEnumerable()
            .Single();

        Assert.Equal("John", item.First);
        Assert.Equal("Peters", item.Last);
        Assert.Equal("19701115", item.BirthDate);
        Assert.Equal("123", item.Quantity);
        Assert.Equal("US$ 5.98", item.Price);
        Assert.Equal(string.Empty, item.Count);
        Assert.Equal("2020-11-13T10:20:30.0000000+02:00", item.SomeDateTimeOffset);
        Assert.Equal("yes", item.IsActive);
        Assert.Equal(string.Empty, item.NullableCustom);
    }
    [Fact]
    public void WithSchemaFromRecord_CanReturnAsDynamicWithType()
    {
        CsvSchema schema = new CsvSchemaBuilder()
            .From<MyRecord>()
            .Schema;

        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable(typeof(MyRecord))
            .Single();

        Verify(item);
    }

    [Fact]
    public void WithSchemaFromClass_CanReturnAsDynamicWithType()
    {
        CsvSchema schema = new CsvSchemaBuilder()
            .From<MyClass>()
            .Schema;

        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable(typeof(MyClass))
            .Single();

        Verify(item);
    }

    [Fact]
    public void WithSchemaFromClassWithIncompleteColumns_CanReturnAsDynamicWithType()
    {
        CsvSchema schema = new CsvSchemaBuilder()
            .From<MyClass>()
            .Schema;
        var input =
            """
            First;Ignored;Last;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset;IsActive;NullableCustom
            "John";SomeIgnoredValue;Peters;19701115;123;US$ 5.98;;2147483647;2020-11-13T10:20:30.0000000+02:00;yes;

            """;


        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable(typeof(MyClass))
            .Single();

        Verify(item);
    }

    [Fact]
    public void WithSchemaFromClassWithColumnsInOtherOrder_CanReturnAsDynamicWithType()
    {
        CsvSchema schema = new CsvSchemaBuilder()
            .From<MyClass>()
            .Schema;
        var input =
            "Last;First;BirthDate;Quantity;Price;Count;LargeValue;SomeDateTimeOffset;IsActive;NullableCustom\r\n" +
            "\"Peters\";John;19701115;123;US$ 5.98;;2147483647;2020-11-13T10:20:30.0000000+02:00;yes;\r\n";


        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable(typeof(MyClass))
            .Single();

        Verify(item);
    }

    [Fact]
    public void WhenSchemaCreatedFromRecord_ExpectedValuesAreReturned()
    {
        CsvSchema schema = new CsvSchemaBuilder().From<MyRecord>().Schema;

        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<MyRecord>()
            .Single();

        Verify(item);
    }
    [Fact]
    public void WhenSchemaImplicitlyCreatedFromRecord_ExpectedValuesAreReturned()
    {
        var item = ReadCsv
            .FromString<MyRecord>(input, delimiter: ';', hasHeaders: true)
            .Single();

        Verify(item);
    }
    [Fact]
    public void WhenSchemaCreatedFromClass_ExpectedValuesAreReturned()
    {
        CsvSchema schema = new CsvSchemaBuilder().From<MyClass>().Schema;

        var item = ReadCsv
            .FromString(input, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<MyClass>()
            .Single();

        Verify(item);
    }
    [Fact]
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
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string v && !string.IsNullOrEmpty(v) ? new Custom(v) : null;
}

[TypeConverter(typeof(CustomTypeConverter))]
public class Custom
{
    public Custom(string value) { Value = value; }
    public string Value { get; set; }
    public override string ToString() => Value;
}
public interface IMyItem
{
    public string First { get; }
    public Custom Last { get; }
    public DateTime BirthDate { get; }
    public int Quantity { get; }
    public Amount Price { get; }
    public int? Count { get; }
    public decimal LargeValue { get; }
    public DateTimeOffset SomeDateTimeOffset { get; }
    public bool IsActive { get; }
    public Custom NullableCustom { get; }

}
public record MyRecord(
    string First,
    Custom Last,
    [CsvFormat("yyyyMMdd")] DateTime BirthDate,
    int Quantity,
    Amount Price,
    int? Count,
    decimal LargeValue,
    DateTimeOffset SomeDateTimeOffset,
    [CsvFormat("yes|no")] bool IsActive,
    Custom NullableCustom = null) : IMyItem;

public class MyClass : IMyItem
{
    public string First { get; set; }
    public Custom Last { get; set; }
    [CsvFormat("yyyyMMdd")]
    public DateTime BirthDate { get; set; }
    public int Quantity { get; set; }
    public Amount Price { get; set; }
    public int? Count { get; set; }
    public decimal LargeValue { get; set; }
    public DateTimeOffset SomeDateTimeOffset { get; set; }
    [CsvFormat("yes|no")]
    public bool IsActive { get; set; }
    public Custom NullableCustom { get; set; }
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

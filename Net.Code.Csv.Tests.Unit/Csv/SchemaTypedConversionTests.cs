using System.IO;
using Net.Code.Csv.Impl;

namespace Net.Code.Csv.Tests.Unit.Csv;

public class SchemaTypedConversionTests
{
    const string Csv =
        """
        Id;Name;Born;Amount;Flag;Count;Text
        42;x;19701115;12.5;yes;7;123
        43;y;19801231;-1.25;no;;456
        """;

    static CsvSchema CreateSchema() => new CsvSchemaBuilder()
        .AddInt32("Id")
        .AddString("Name")
        .AddDateTime("Born", "yyyyMMdd")
        .AddDecimal("Amount")
        .AddBoolean("Flag", "yes", "no")
        .AddInt32("Count", allowNull: true)
        .AddString("Text")
        .Schema;

    [Fact]
    public void TypedBuilderColumns_ExposeTypedConverter()
    {
        var schema = CreateSchema();

        Assert.NotNull(TypedSpanConverter<int>.TryGet(schema[0].FromSpan));
        Assert.NotNull(TypedSpanConverter<string>.TryGet(schema[1].FromSpan));
        Assert.NotNull(TypedSpanConverter<DateTime>.TryGet(schema[2].FromSpan));
        Assert.NotNull(TypedSpanConverter<decimal>.TryGet(schema[3].FromSpan));
        Assert.NotNull(TypedSpanConverter<bool>.TryGet(schema[4].FromSpan));
        Assert.NotNull(TypedSpanConverter<int>.TryGet(schema[5].FromSpan));
        Assert.Null(TypedSpanConverter<long>.TryGet(schema[0].FromSpan));
    }

    [Fact]
    public void ColumnWithReplacedFromSpan_DoesNotUseOriginalTypedConverter()
    {
        var schema = CreateSchema();
        var column = schema[0] with { FromSpan = s => -1 };

        Assert.Null(TypedSpanConverter<int>.TryGet(column.FromSpan));

        var copy = schema[0] with { Name = "Other" };
        Assert.NotNull(TypedSpanConverter<int>.TryGet(copy.FromSpan));
    }

    [Fact]
    public void ColumnEqualityAndToString_AreUnaffected()
    {
        var schema = CreateSchema();
        var column = schema[0];
        var copy = column with { };

        Assert.Equal(column, copy);
        Assert.Equal(column.GetHashCode(), copy.GetHashCode());
        Assert.NotEqual(column, CreateSchema()[0]);
        Assert.StartsWith("CsvColumn { Name = Id, PropertyName = Id, Type = System.Int32, FromSpan = ", column.ToString());
        Assert.Equal(42, column.FromSpan("42".AsSpan()));
    }

    [Fact]
    public void TypedGetters_OnSchemaReader_ReturnConvertedValues()
    {
        using var reader = ReadCsv.FromString(Csv, delimiter: ';', hasHeaders: true, schema: CreateSchema());

        Assert.True(reader.Read());
        Assert.Equal(42, reader.GetInt32(0));
        Assert.Equal("x", reader.GetString(1));
        Assert.Equal(new DateTime(1970, 11, 15), reader.GetDateTime(2));
        Assert.Equal(12.5m, reader.GetDecimal(3));
        Assert.True(reader.GetBoolean(4));
        Assert.Equal(7, reader.GetInt32(5));
        Assert.False(reader.IsDBNull(5));

        Assert.True(reader.Read());
        Assert.Equal(43, reader.GetInt32(0));
        Assert.Equal(new DateTime(1980, 12, 31), reader.GetDateTime(2));
        Assert.Equal(-1.25m, reader.GetDecimal(3));
        Assert.False(reader.GetBoolean(4));
        Assert.Equal(0, reader.GetInt32(5));
        Assert.True(reader.IsDBNull(5));
        Assert.Null(reader.GetValue(5));
    }

    [Fact]
    public void TypedGetter_OnStringColumn_ConvertsRawValue()
    {
        using var reader = ReadCsv.FromString(Csv, delimiter: ';', hasHeaders: true, schema: CreateSchema());

        Assert.True(reader.Read());
        Assert.Equal(123, reader.GetInt32(6));
        Assert.Equal(123L, reader.GetInt64(6));
        Assert.Equal("123", reader.GetString(6));
    }

    [Fact]
    public void TypedGetter_OfOtherType_OnTypedColumn_Throws()
    {
        using var reader = ReadCsv.FromString(Csv, delimiter: ';', hasHeaders: true, schema: CreateSchema());

        Assert.True(reader.Read());
        Assert.Throws<InvalidCastException>(() => reader.GetDecimal(0));
        Assert.Throws<InvalidCastException>(() => reader.GetInt64(0));
    }

    [Fact]
    public void GetString_WhenCustomStringConverterReturnsNull_ReturnsEmptyString()
    {
        var schema = new CsvSchemaBuilder()
            .Add<string>("Name", s => null, true)
            .Schema;

        using var reader = ReadCsv.FromString("Name\nx", hasHeaders: true, schema: schema);

        Assert.True(reader.Read());
        Assert.Equal(string.Empty, reader.GetString(0));
        Assert.Null(reader.GetValue(0));
    }

    [Fact]
    public void CustomTypedConverter_IsUsedByGettersAndActivators()
    {
        var schema = new CsvSchemaBuilder()
            .Add("Amount", s => Amount.Parse(s.ToString(), CultureInfo.InvariantCulture), false)
            .Add("Value", s => int.Parse(s, CultureInfo.InvariantCulture) * 10, true)
            .Schema;
        const string csv =
            """
            Amount;Value
            EUR 1.5;4
            USD 2;
            """;

        var items = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<AmountRecord>()
            .ToList();

        Assert.Equal([new AmountRecord(new Amount("EUR", 1.5m), 40), new AmountRecord(new Amount("USD", 2m), null)], items);

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema);
        Assert.True(reader.Read());
        Assert.Equal(40, reader.GetInt32(1));
        Assert.Equal(new Amount("EUR", 1.5m), reader.GetValue(0));
    }

    [Fact]
    public void FromString_WithNullableTypedColumns_ReadsValuesAndNulls()
    {
        const string csv =
            """
            Id;Count;When;Ratio;Flag
            1;5;20240102;0.5;true
            2;;;;
            """;

        var items = ReadCsv.FromString<NullableRecord>(csv, delimiter: ';', hasHeaders: true).ToList();

        Assert.Equal(
            [
                new NullableRecord(1, 5, new DateTime(2024, 1, 2), 0.5, true),
                new NullableRecord(2, null, null, null, null),
            ],
            items);
    }

    [Fact]
    public void DataReaderAsEnumerable_WithNullableTypedColumns_ReadsValuesAndNulls()
    {
        const string csv =
            """
            Flag;Ratio;When;Count;Id
            true;0.5;20240102;5;1
            ;;;;2
            """;
        var schema = new CsvSchemaBuilder().From<NullableRecord>().Schema;

        var items = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<NullableRecord>()
            .ToList();

        Assert.Equal(
            [
                new NullableRecord(1, 5, new DateTime(2024, 1, 2), 0.5, true),
                new NullableRecord(2, null, null, null, null),
            ],
            items);
    }

    [Fact]
    public void FromFile_WithTypedRecord_ReadsValues()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,
                """
                Count;Id;When;Ratio;Flag
                5;1;20240102;0.5;true
                ;2;;;
                """);

            var items = ReadCsv.FromFile<NullableRecord>(path, delimiter: ';', hasHeaders: true).ToList();

            Assert.Equal(
                [
                    new NullableRecord(1, 5, new DateTime(2024, 1, 2), 0.5, true),
                    new NullableRecord(2, null, null, null, null),
                ],
                items);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromFile_WithMutableClass_ReadsValues()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,
                """
                Id;Count;When
                1;5;20240102
                2;;
                """);

            var items = ReadCsv.FromFile<NullableClass>(path, delimiter: ';', hasHeaders: true).ToList();

            Assert.Equal(2, items.Count);
            Assert.Equal((1, 5, (DateTime?)new DateTime(2024, 1, 2)), (items[0].Id, items[0].Count, items[0].When));
            Assert.Equal((2, (int?)null, (DateTime?)null), (items[1].Id, items[1].Count, items[1].When));
        }
        finally
        {
            File.Delete(path);
        }
    }

    public record AmountRecord(Amount Amount, int? Value);

    public record NullableRecord(
        int Id,
        int? Count,
        [CsvFormat("yyyyMMdd")] DateTime? When,
        double? Ratio,
        [CsvFormat("true|false")] bool? Flag);

    public class NullableClass
    {
        public int Id { get; set; }
        public int? Count { get; set; }
        [CsvFormat("yyyyMMdd")]
        public DateTime? When { get; set; }
    }
}

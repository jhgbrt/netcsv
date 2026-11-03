namespace Net.Code.Csv.Tests.Unit.Csv;

public class SchemaReaderOrdinalTests
{
    [Fact]
    public void WithSchema_WhenColumnsInOtherOrderThanHeader_ValuesAreResolvedByName()
    {
        const string csv =
            """
            A;B;C
            x;1.5;42
            """;
        var schema = new CsvSchemaBuilder()
            .AddInt32("C")
            .AddString("A")
            .AddDecimal("B")
            .Schema;

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema);
        Assert.True(reader.Read());

        Assert.Equal("C", reader.GetName(0));
        Assert.Equal("A", reader.GetName(1));
        Assert.Equal("B", reader.GetName(2));
        Assert.Equal(42, reader.GetInt32(0));
        Assert.Equal("x", reader.GetString(1));
        Assert.Equal(1.5m, reader.GetDecimal(2));
        Assert.Equal(42, reader.GetValue(0));
        Assert.Equal("x", reader.GetValue(1));
        Assert.Equal(1.5m, reader.GetValue(2));
        Assert.Equal(1.5m, reader["B"]);
    }

    [Fact]
    public void WithSchema_WhenColumnsInOtherOrderThanHeader_AsEnumerableMapsByName()
    {
        const string csv =
            """
            Name;Value;Id
            x;1.5;42
            y;2.5;43
            """;
        var schema = new CsvSchemaBuilder().From<OrdinalRecord>().Schema;

        var items = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<OrdinalRecord>()
            .ToList();

        Assert.Equal([new OrdinalRecord(42, "x", 1.5m), new OrdinalRecord(43, "y", 2.5m)], items);
    }

    [Fact]
    public void WithSchema_WhenColumnNamesNotInHeader_FallsBackToSchemaOrdinal()
    {
        const string csv =
            """
            A;B;C
            1;2;3
            """;
        var schema = new CsvSchemaBuilder()
            .AddInt32("X")
            .AddInt32("Y")
            .AddInt32("Z")
            .Schema;

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema);
        Assert.True(reader.Read());

        Assert.Equal("Y", reader.GetName(1));
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(2, reader.GetInt32(1));
        Assert.Equal(3, reader.GetValue(2));
    }

    [Fact]
    public void WithSchema_WhenSomeColumnNamesNotInHeader_MixesNameAndOrdinalResolution()
    {
        const string csv =
            """
            A;B;C
            1;2;3
            """;
        var schema = new CsvSchemaBuilder()
            .AddInt32("C")
            .AddInt32("Unknown")
            .AddInt32("A")
            .Schema;

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema);
        Assert.True(reader.Read());

        Assert.Equal(3, reader.GetInt32(0));
        Assert.Equal(2, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
    }

    [Fact]
    public void WithSchema_WithoutHeaders_UsesSchemaOrdinal()
    {
        const string csv =
            """
            1;x
            2;y
            """;
        var schema = new CsvSchemaBuilder()
            .AddInt32("Id")
            .AddString("Name")
            .Schema;

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: false, schema: schema);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("x", reader.GetString(1));
        Assert.True(reader.Read());
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("y", reader.GetString(1));
    }

    [Fact]
    public void WithSchema_WhenOrdinalBeyondSchemaColumns_Throws()
    {
        const string csv =
            """
            A;B;C
            1;2;3
            """;
        var schema = new CsvSchemaBuilder()
            .AddInt32("A")
            .AddInt32("B")
            .Schema;

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, schema: schema);
        Assert.True(reader.Read());

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => reader.GetValue(2));
    }

    [Fact]
    public void WithSingleSchema_WhenNextResultHasDifferentHeaderOrder_ValuesAreResolvedAgainstNewHeader()
    {
        const string csv =
            """
            A;B
            1;x

            B;A
            y;2
            """;
        var schema = new CsvSchemaBuilder()
            .AddInt32("A")
            .AddString("B")
            .Schema;

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, emptyLineAction: EmptyLineAction.NextResult, schema: schema);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("x", reader.GetString(1));
        Assert.False(reader.Read());

        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("y", reader.GetString(1));
        Assert.Equal(2, reader.GetValue(0));
        Assert.Equal("y", reader.GetValue(1));
        Assert.False(reader.Read());
    }

    [Fact]
    public void WithMultipleSchemas_WhenNextResult_EachResultSetUsesItsOwnSchemaAndHeader()
    {
        const string csv =
            """
            Name;Id
            x;1

            Amount;Code;When
            12.5;ABC;20240102
            """;
        CsvSchema[] schemas =
        [
            new CsvSchemaBuilder().AddInt32("Id").AddString("Name").Schema,
            new CsvSchemaBuilder().AddString("Code").AddDateTime("When", "yyyyMMdd").AddDecimal("Amount").Schema,
        ];

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, emptyLineAction: EmptyLineAction.NextResult, schema: schemas);

        Assert.True(reader.Read());
        Assert.Equal("Id", reader.GetName(0));
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("x", reader.GetString(1));
        Assert.False(reader.Read());

        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("Code", reader.GetName(0));
        Assert.Equal("ABC", reader.GetString(0));
        Assert.Equal(new DateTime(2024, 1, 2), reader.GetDateTime(1));
        Assert.Equal(12.5m, reader.GetDecimal(2));
        Assert.Equal(12.5m, reader.GetValue(2));
        Assert.False(reader.Read());
    }

    [Fact]
    public void WithMultipleSchemas_WhenNextResult_AsEnumerableMapsEachResultSetByName()
    {
        const string csv =
            """
            Value;Name;Id
            1.5;x;42

            Id;Value;Name
            43;2.5;y
            """;
        CsvSchema[] schemas =
        [
            new CsvSchemaBuilder().From<OrdinalRecord>().Schema,
            new CsvSchemaBuilder().From<OrdinalRecord>().Schema,
        ];

        using var reader = ReadCsv.FromString(csv, delimiter: ';', hasHeaders: true, emptyLineAction: EmptyLineAction.NextResult, schema: schemas);

        var first = reader.AsEnumerable<OrdinalRecord>().ToList();
        Assert.True(reader.NextResult());
        var second = reader.AsEnumerable<OrdinalRecord>().ToList();

        Assert.Equal([new OrdinalRecord(42, "x", 1.5m)], first);
        Assert.Equal([new OrdinalRecord(43, "y", 2.5m)], second);
    }

    public record OrdinalRecord(int Id, string Name, decimal Value);
}

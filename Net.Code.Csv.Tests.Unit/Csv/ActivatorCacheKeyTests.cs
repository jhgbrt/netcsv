namespace Net.Code.Csv.Tests.Unit.Csv;

public class ActivatorCacheKeyTests
{
    private record NamePair(string First, string Last);
    private record DateItem(DateTime BirthDate);

    [Fact]
    public void TypedActivator_UsesHeaderOrderInCacheKey()
    {
        const string csvA = """
            First;Last
            John;Doe
            """;
        const string csvB = """
            Last;First
            Doe;John
            """;

        var schema = new CsvSchemaBuilder().From<NamePair>().Schema;

        var first = ReadCsv.FromString(csvA, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<NamePair>()
            .Single();

        var second = ReadCsv.FromString(csvB, delimiter: ';', hasHeaders: true, schema: schema)
            .AsEnumerable<NamePair>()
            .Single();

        Assert.Equal("John", first.First);
        Assert.Equal("Doe", first.Last);
        Assert.Equal("John", second.First);
        Assert.Equal("Doe", second.Last);
    }

    [Fact]
    public void TypedActivator_UsesSchemaSignatureInCacheKey()
    {
        const string csvA = """
            BirthDate
            19700102
            """;
        const string csvB = """
            BirthDate
            02-01-1970
            """;

        var schemaA = new CsvSchemaBuilder(CultureInfo.InvariantCulture)
            .AddDateTime("BirthDate", "yyyyMMdd")
            .Schema;

        var schemaB = new CsvSchemaBuilder(CultureInfo.InvariantCulture)
            .AddDateTime("BirthDate", "dd-MM-yyyy")
            .Schema;

        var first = ReadCsv.FromString(csvA, delimiter: ';', hasHeaders: true, schema: schemaA)
            .AsEnumerable<DateItem>()
            .Single();

        var second = ReadCsv.FromString(csvB, delimiter: ';', hasHeaders: true, schema: schemaB)
            .AsEnumerable<DateItem>()
            .Single();

        Assert.Equal(new DateTime(1970, 1, 2), first.BirthDate);
        Assert.Equal(new DateTime(1970, 1, 2), second.BirthDate);
    }
}

using System.Data;
using System.Linq;

using Xunit;

namespace Net.Code.Csv.Tests.Unit.Csv;

public class SchemaBooleanTests
{
    public record Flags(string Name, bool Active, bool? Optional);

    private const string Data = "Name,Active,Optional\nfoo,true,False\nbar,False,\n";

    private static IDataReader Reader(CsvSchema schema)
        => ReadCsv.FromString(Data, hasHeaders: true, schema: schema);

    [Fact]
    public void AsEnumerable_WithSchemaFromType_BoolWithoutCsvFormat_ParsesTrueFalse()
    {
        using var reader = Reader(Schema.From<Flags>());

        var items = reader.AsEnumerable<Flags>().ToArray();

        Assert.Equal([new Flags("foo", true, false), new Flags("bar", false, null)], items);
    }

    [Fact]
    public void GetBoolean_WithSchemaFromType_BoolWithoutCsvFormat_ParsesTrueFalse()
    {
        using var reader = Reader(Schema.From<Flags>());

        Assert.True(reader.Read());
        Assert.True(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
    }

    [Fact]
    public void GetValue_WithBuilderAddBooleanNullFormat_ParsesTrueFalse()
    {
        var schema = new CsvSchemaBuilder()
            .AddString("Name")
            .AddBoolean("Active", format: null)
            .AddBoolean("Optional", format: null, allowNull: true)
            .Schema;
        using var reader = Reader(schema);

        Assert.True(reader.Read());
        Assert.Equal(true, reader.GetValue(1));
        Assert.Equal(false, reader.GetValue(2));
    }
}

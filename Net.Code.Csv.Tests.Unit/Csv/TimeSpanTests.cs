namespace Net.Code.Csv.Tests.Unit.Csv;

public class TimeSpanTests
{
    [Fact]
    public void FromString_WithTimeSpanProperty_ParsesValue()
    {
        const string csv =
            """
            Duration;Optional
            01:02:03;
            """;

        var item = ReadCsv
            .FromString<TimeSpanRecord>(csv, delimiter: ';', hasHeaders: true)
            .Single();

        Assert.Equal(new TimeSpan(1, 2, 3), item.Duration);
        Assert.Null(item.Optional);
    }

    [Fact]
    public void FromString_WithFormattedTimeSpan_UsesFormat()
    {
        const string csv =
            """
            Duration
            02hrs30m
            """;

        var item = ReadCsv
            .FromString<TimeSpanFormatRecord>(csv, delimiter: ';', hasHeaders: true)
            .Single();

        Assert.Equal(TimeSpan.FromMinutes(150), item.Duration);
    }

    [Fact]
    public void SchemaBuilder_AddTimeSpan_ParsesValue()
    {
        const string csv =
            """
            Duration
            02hrs30m
            """;

        var schema = new CsvSchemaBuilder()
            .AddTimeSpan("Duration", "hh'hrs'mm'm'")
            .Schema;

        using var reader = ReadCsv.FromString(csv, hasHeaders: true, schema: schema);
        Assert.True(reader.Read());
        Assert.Equal(TimeSpan.FromMinutes(150), reader[0]);
    }

    [Fact]
    public void WriteCsv_UsesTimeSpanFormat()
    {
        var result = WriteCsv.ToString(
            new[] { new TimeSpanWriteItem { Duration = TimeSpan.FromMinutes(150) } },
            delimiter: ';',
            hasHeaders: true);

        Assert.Equal($"Duration{Environment.NewLine}02hrs30m{Environment.NewLine}", result);
    }

    private record TimeSpanRecord(TimeSpan Duration, TimeSpan? Optional);

    public record TimeSpanFormatRecord([CsvFormat("hh'hrs'mm'm'")] TimeSpan Duration);

    private sealed class TimeSpanWriteItem
    {
        [CsvFormat("hh'hrs'mm'm'")]
        public TimeSpan Duration { get; set; }
    }
}

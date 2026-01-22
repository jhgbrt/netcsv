using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using Xunit;

namespace Net.Code.Csv.Tests.Unit.Csv;

public class ParserV2RegressionTests
{
    private const string ParserEnvVar = "NETCSV_PARSER";

    private const string Sample = """
        First;Last;BirthDate;Quantity;Price
        Kevin;Spacey;1960-07-26;789;3.99
        Mary;Smith;1975-03-14;456;8.95
        """;

    private const string BenchmarkHeader = "First;Last;BirthDate;Quantity;Price";
    private const string BenchmarkRowA = "John;Peters;1970-05-23;123;5.89";
    private const string BenchmarkRowB = "Mary;Smith;1975-03-14;456;8.95";
    private const string BenchmarkTemplate = """
        First;Last;BirthDate;Quantity;Price
        John;Peters;1970-05-23;123;5.89
        Mary;Smith;1975-03-14;456;8.95
        Kevin;Spacey;1960-07-26;789;3.99
        Scott;Tiger;1980-09-15;234;9.99
        Peter;Pan;1990-11-11;567;2.99
        John;Doe;1970-05-23;123;5.89
        Jane;Doe;1975-03-14;456;8.95
        Alice;Doe;1960-07-26;789;3.99
        Bob;Doe;1980-09-15;234;9.99
        Charlie;Doe;1990-11-11;567;2.99
        Ursula;Levy;1970-05-23;123;5.89
        Linda;Levy;1975-03-14;456;8.95
        Eve;Levy;1960-07-26;789;3.99
        Adam;Levy;1980-09-15;234;9.99
        Oscar;Levy;1990-11-11;567;2.99
        Poppy;Smith;1970-05-23;123;5.89
        Sandrine;Smith;1975-03-14;456;8.95
        Prokopis;Smith;1960-07-26;789;3.99
        Sofia;Smith;1980-09-15;234;9.99
        Petros;Smith;1990-11-11;567;2.99
        """;

    private record BenchmarkItem(string First, string Last, DateTime BirthDate, int Quantity, decimal Price);

    [Fact]
    public void ReadCsv_WithSchema_WhenParserV2_ShouldParseTypedFields()
    {
        var original = Environment.GetEnvironmentVariable(ParserEnvVar);
        Environment.SetEnvironmentVariable(ParserEnvVar, "V2");
        try
        {
            var schema = new CsvSchemaBuilder(CultureInfo.InvariantCulture)
                .AddString("First")
                .AddString("Last")
                .AddDateTime("BirthDate")
                .AddInt32("Quantity")
                .AddDecimal("Price")
                .Schema;

            using var reader = ReadCsv.FromString(Sample, delimiter: ';', hasHeaders: true, schema: schema);

            Assert.True(reader.Read());
            Assert.Equal(new DateTime(1960, 7, 26), reader.GetDateTime(reader.GetOrdinal("BirthDate")));
            Assert.Equal(3.99m, reader.GetDecimal(reader.GetOrdinal("Price")));

            Assert.True(reader.Read());
            Assert.Equal(new DateTime(1975, 3, 14), reader.GetDateTime(reader.GetOrdinal("BirthDate")));
            Assert.Equal(8.95m, reader.GetDecimal(reader.GetOrdinal("Price")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(ParserEnvVar, original);
        }
    }

    [Fact]
    public void ReadCsv_WithLongLine_WhenParserV2_ShouldNotCorruptEarlyFields()
    {
        var longField = new string('x', 5000);
        var input = $"BirthDate;Blob;Price{Environment.NewLine}1960-07-26;{longField};3.99{Environment.NewLine}";

        var schema = new CsvSchemaBuilder(CultureInfo.InvariantCulture)
            .AddDateTime("BirthDate")
            .AddString("Blob")
            .AddDecimal("Price")
            .Schema;

        using var reader = ReadCsv.FromString(input, delimiter: ';', hasHeaders: true, schema: schema);

        Assert.True(reader.Read());
        Assert.Equal(new DateTime(1960, 7, 26), reader.GetDateTime(reader.GetOrdinal("BirthDate")));
        Assert.Equal(longField.Length, reader.GetString(reader.GetOrdinal("Blob")).Length);
        Assert.Equal(3.99m, reader.GetDecimal(reader.GetOrdinal("Price")));
    }

    [Fact]
    public void ReadCsv_WithRepeatedBenchmarkRows_WhenParserV2_ShouldParseAllTypedRecords()
    {
        var builder = new StringBuilder(128 * 1024);
        builder.AppendLine(BenchmarkHeader);
        const int repeatCount = 5000;
        for (var i = 0; i < repeatCount; i++)
        {
            builder.AppendLine(i % 2 == 0 ? BenchmarkRowA : BenchmarkRowB);
        }

        var schema = Schema.From<BenchmarkItem>();
        using var reader = ReadCsv.FromString(builder.ToString(), delimiter: ';', hasHeaders: true, schema: schema);

        decimal total = 0;
        foreach (var item in reader.AsEnumerable<BenchmarkItem>())
        {
            total += item.Price;
        }

        Assert.True(total > 0);
    }

    [Fact]
    public void ReadCsv_FromFile_WithBenchmarkRows_WhenParserV2_ShouldParseAllTypedRecords()
    {
        var schema = Schema.From<BenchmarkItem>();
        var tempFile = Path.Combine(Path.GetTempPath(), $"netcodecsv-bench-{Guid.NewGuid():N}.csv");

        try
        {
            var templateLines = BenchmarkTemplate
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.TrimEnd('\r'))
                .ToArray();
            var templateRows = templateLines.Skip(1).ToArray();

            using (var writer = new StreamWriter(tempFile, false, Encoding.UTF8, bufferSize: 32 * 1024))
            {
                writer.WriteLine(templateLines[0]);
                const int repeatCount = 100_000;
                for (var i = 0; i < repeatCount; i++)
                {
                    writer.WriteLine(templateRows[i % templateRows.Length]);
                }
            }

            using var reader = ReadCsv.FromFile(
                tempFile,
                encoding: Encoding.UTF8,
                delimiter: ';',
                hasHeaders: true,
                schema: schema);

            decimal total = 0;
            foreach (var item in reader.AsEnumerable<BenchmarkItem>())
            {
                total += item.Price;
            }

            Assert.True(total > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

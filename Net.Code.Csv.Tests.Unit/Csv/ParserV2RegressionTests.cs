using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using Xunit;

namespace Net.Code.Csv.Tests.Unit.Csv;

public class ParserV2RegressionTests
{
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
    public void ReadCsv_WithSchema_ShouldParseTypedFields()
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

    // BufferedCharReader reads the input in chunks of this size; tests below place
    // interesting characters around this boundary.
    private const int ReaderBufferSize = 16384;
    private const int RawWindow = 32;

    private static string Last32(string s) => s.Length <= RawWindow ? s : s.Substring(s.Length - RawWindow);

    // Builds lines of `fields` unquoted fields (1 or 2) with a total length of exactly `length` chars.
    private static string Filler(int length, int fields)
    {
        if (length == 0)
        {
            return string.Empty;
        }
        var sb = new StringBuilder(length);
        var remaining = length;
        while (remaining > 20)
        {
            sb.Append(fields == 1 ? "0123456789abcde\n" : "0123456,89abcde\n");
            remaining -= 16;
        }
        var n = remaining - 1;
        sb.Append(fields == 1 ? new string('b', n) : "a," + new string('b', n - 2));
        sb.Append('\n');
        Assert.Equal(length, sb.Length);
        return sb.ToString();
    }

    private static MalformedCsvException ReadAllExpectingError(Func<System.Data.IDataReader> open)
    {
        return Assert.ThrowsAny<MalformedCsvException>(() =>
        {
            using var reader = open();
            do
            {
                while (reader.Read())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        _ = reader.GetValue(i);
                    }
                }
            } while (reader.NextResult());
        });
    }

    public static TheoryData<int> BoundaryOffsets => new()
    {
        0, 10, 100,
        ReaderBufferSize - 40, ReaderBufferSize - 20, ReaderBufferSize - 3, ReaderBufferSize - 1,
        ReaderBufferSize, ReaderBufferSize + 1, ReaderBufferSize + 2, ReaderBufferSize + 40,
        2 * ReaderBufferSize - 10
    };

    [Theory]
    [MemberData(nameof(BoundaryOffsets))]
    public void MalformedQuote_RawData_IsLastCharsUpToAndIncludingOffendingChar(int prefixLength)
    {
        var input = Filler(prefixLength, 1) + "\"abc\"x\n";
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(
            input, escape: '\\', quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.ThrowException));

        var end = input.LastIndexOf('x') + 1;
        Assert.Equal(Last32(input.Substring(0, end)), ex.RawData);
    }

    [Theory]
    [MemberData(nameof(BoundaryOffsets))]
    public void StrictWhitespaceBeforeQuote_RawData_ExcludesOffendingQuote(int prefixLength)
    {
        var input = Filler(prefixLength, 2) + "a, \"b\"\n";
        var behaviour = Net.Code.Csv.Impl.CsvBehaviour.Strict() with { TrimmingOptions = ValueTrimmingOptions.All };
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(input, behaviour: behaviour));

        var end = input.LastIndexOf(", \"") + 2;
        Assert.Equal(Last32(input.Substring(0, end)), ex.RawData);
    }

    [Theory]
    [MemberData(nameof(BoundaryOffsets))]
    public void MissingFieldAtNewLine_RawData_IncludesNewLine(int prefixLength)
    {
        if (prefixLength == 0) prefixLength = 4;
        var input = Filler(prefixLength, 2) + "1\n2,2\n";
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(input));

        Assert.IsType<MissingFieldCsvException>(ex);
        var end = prefixLength + 2;
        Assert.Equal(Last32(input.Substring(0, end)), ex.RawData);
    }

    [Theory]
    [MemberData(nameof(BoundaryOffsets))]
    public void MissingFieldAtEndOfInput_RawData_IsTailOfInput(int prefixLength)
    {
        if (prefixLength == 0) prefixLength = 4;
        var input = Filler(prefixLength, 2) + "1";
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(input));

        Assert.IsType<MissingFieldCsvException>(ex);
        Assert.Equal(Last32(input), ex.RawData);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(100)]
    [InlineData(ReaderBufferSize - 60)]
    [InlineData(ReaderBufferSize - 20)]
    [InlineData(ReaderBufferSize - 5)]
    [InlineData(ReaderBufferSize - 1)]
    [InlineData(ReaderBufferSize)]
    [InlineData(ReaderBufferSize + 3)]
    public void MissingFieldAfterComment_RawData_ExcludesCommentBody(int prefixLength)
    {
        var filler = Filler(prefixLength, 2);
        const string Comment = "#this comment body is longer than the raw window\n";
        var input = filler + Comment + "1\n";
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(input));

        Assert.IsType<MissingFieldCsvException>(ex);
        Assert.Equal(Last32(filler + "#" + "1\n"), ex.RawData);
    }

    [Fact]
    public void MissingFieldAfterCrLfComment_RawData_ExcludesCommentBody()
    {
        var input = "a,b\r\n#c\r\n1\r\n";
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(input));

        Assert.IsType<MissingFieldCsvException>(ex);
        Assert.Equal("a,b\r\n#1\r\n", ex.RawData);
    }

    [Fact]
    public void MissingFieldInSecondResultSet_RawData_StartsAtResultSet()
    {
        var input = "a,b\n1,2\n\nc,d\n3\n";
        var ex = ReadAllExpectingError(() => ReadCsv.FromString(input, hasHeaders: true, emptyLineAction: EmptyLineAction.NextResult));

        Assert.IsType<MissingFieldCsvException>(ex);
        Assert.Equal("c,d\n3\n", ex.RawData);
    }

    // Golden-output checks for random inputs that mix quotes, escapes, delimiters, whitespace,
    // comments and line endings, long enough to cross BufferedCharReader refills. The expected
    // results were recorded from the original (V1) parser before it was removed (see
    // ParserV2Golden.txt). Each entry holds the line count, the error (if any) and a hash of
    // the parsed fields.
    private static (List<string[]> Lines, string Error) Parse(string input, Net.Code.Csv.Impl.CsvLayout layout, Net.Code.Csv.Impl.CsvBehaviour behaviour)
    {
        var lines = new List<string[]>();
        try
        {
            var text = new StringReader(input);
            var buffered = new Net.Code.Csv.Impl.BufferedCharReader(text);
            var parser = new Net.Code.Csv.Impl.CsvParser(text, buffered, layout, behaviour);
            lines.Add(parser.Header.Fields);
            var enumerator = parser.GetEnumerator();
            while (enumerator.MoveNext())
            {
                // Materialize before advancing: a line is only valid until the next MoveNext.
                lines.Add(enumerator.Current.GetStrings());
            }
            return (lines, null);
        }
        catch (MalformedCsvException ex)
        {
            return (lines, $"{ex.GetType().Name} {ex.LineNumber},{ex.FieldNumber},{ex.ColumnNumber}");
        }
    }

    private static string Fingerprint((List<string[]> Lines, string Error) result)
    {
        var sb = new StringBuilder();
        foreach (var line in result.Lines)
        {
            sb.Append(line is null ? "<null>" : string.Join("\u001F", line)).Append('\u001E');
        }
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return $"{result.Lines.Count}\t{result.Error}\t{Convert.ToHexString(hash, 0, 8)}";
    }

    private static readonly Lazy<Dictionary<string, string>> Golden = new(() =>
    {
        using var stream = typeof(ParserV2RegressionTests).Assembly
            .GetManifestResourceStream("Net.Code.Csv.Tests.Unit.Csv.ParserV2Golden.txt");
        using var reader = new StreamReader(stream);
        var golden = new Dictionary<string, string>();
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;
            var tab = line.IndexOf('\t');
            golden.Add(line[..tab], line[(tab + 1)..]);
        }
        return golden;
    });

    private static void AssertMatchesGolden(string key, (List<string[]> Lines, string Error) actual)
    {
        Assert.True(Golden.Value.TryGetValue(key, out var expected), $"No golden entry for '{key}'");
        Assert.Equal(expected, Fingerprint(actual));
    }

    private static string RandomCsv(Random random, int length, char[] alphabet)
    {
        var sb = new StringBuilder(length);
        while (sb.Length < length)
        {
            // Mostly runs of plain text so fields get long enough to straddle buffer boundaries.
            if (random.Next(4) == 0)
            {
                sb.Append('x', random.Next(1, 200));
            }
            else
            {
                sb.Append(alphabet[random.Next(alphabet.Length)]);
            }
        }
        return sb.ToString();
    }

    public static TheoryData<int> FuzzSeeds()
    {
        var data = new TheoryData<int>();
        for (var i = 0; i < 60; i++) data.Add(i);
        return data;
    }

    private static (string Input, Net.Code.Csv.Impl.CsvLayout Layout, Net.Code.Csv.Impl.CsvBehaviour Behaviour) FuzzCase(int seed)
    {
        var random = new Random(seed);
        var escape = random.Next(2) == 0 ? '"' : '\\';
        var alphabet = new[] { 'a', 'b', ' ', ' ', '\t', '"', '"', '"', ',', ',', ';', '\n', '\n', '\r', '\\', '#' };
        var length = random.Next(3) == 0 ? random.Next(1, 200) : random.Next(ReaderBufferSize - 500, 3 * ReaderBufferSize);
        var input = RandomCsv(random, length, alphabet);

        var trimming = (ValueTrimmingOptions)random.Next(4);
        var quotesAction = (QuotesInsideQuotedFieldAction)random.Next(3);
        var layout = new Net.Code.Csv.Impl.CsvLayout(Quote: '"', Delimiter: ',', Escape: escape, Comment: '#', HasHeaders: random.Next(2) == 0);
        var behaviour = new Net.Code.Csv.Impl.CsvBehaviour(trimming, MissingFieldAction.ReplaceByEmpty, EmptyLineAction.Skip, quotesAction);
        return (input, layout, behaviour);
    }

    [Theory]
    [MemberData(nameof(FuzzSeeds))]
    public void RandomInput_MatchesGolden(int seed)
    {
        var (input, layout, behaviour) = FuzzCase(seed);
        AssertMatchesGolden($"fuzz:{seed}", Parse(input, layout, behaviour));
    }

    private static readonly string[] BoundaryFields =
    [
        "\"  quoted value  \"",
        "\"ab\"\"cd\"",
        "\"\"\"\"",
        "\"\"",
        "\"multi\nline;\r\nvalue\"",
        "  \"padded\"  ",
        "\"x\" trailing\"",
        "\"" + new string('q', 100) + "\"\"" + new string('r', 100) + "\"",
    ];

    private static readonly int[] QuotedFieldOffsets = [-120, -12, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3];

    public static TheoryData<int, int, ValueTrimmingOptions> QuotedFieldsAtBoundary()
    {
        var data = new TheoryData<int, int, ValueTrimmingOptions>();
        for (var field = 0; field < BoundaryFields.Length; field++)
        {
            foreach (var offset in QuotedFieldOffsets)
            {
                foreach (ValueTrimmingOptions trimming in Enum.GetValues(typeof(ValueTrimmingOptions)))
                {
                    data.Add(offset, field, trimming);
                }
            }
        }
        return data;
    }

    private static (string Input, Net.Code.Csv.Impl.CsvLayout Layout, Net.Code.Csv.Impl.CsvBehaviour Behaviour) BoundaryCase(int offset, int field, ValueTrimmingOptions trimming)
    {
        // Place the field so that it starts `offset` chars from the first buffer refill.
        var prefixLength = ReaderBufferSize + offset - 2;
        var input = Filler(prefixLength, 2).Replace(',', ';') + "a;" + BoundaryFields[field] + "\nb;\"next\"\n";
        var layout = new Net.Code.Csv.Impl.CsvLayout(Quote: '"', Delimiter: ';', Escape: '"', Comment: '#', HasHeaders: false);
        var behaviour = new Net.Code.Csv.Impl.CsvBehaviour(trimming, MissingFieldAction.ReplaceByEmpty, EmptyLineAction.Skip, QuotesInsideQuotedFieldAction.Ignore);
        return (input, layout, behaviour);
    }

    [Theory]
    [MemberData(nameof(QuotedFieldsAtBoundary))]
    public void QuotedFieldNearBufferBoundary_MatchesGolden(int offset, int field, ValueTrimmingOptions trimming)
    {
        var (input, layout, behaviour) = BoundaryCase(offset, field, trimming);
        var actual = Parse(input, layout, behaviour);

        Assert.Null(actual.Error);
        AssertMatchesGolden($"boundary:{offset}:{field}:{trimming}", actual);
    }

    [Theory]
    [InlineData(ValueTrimmingOptions.None, "  a  ")]
    [InlineData(ValueTrimmingOptions.UnquotedOnly, "  a  ")]
    [InlineData(ValueTrimmingOptions.QuotedOnly, "a")]
    [InlineData(ValueTrimmingOptions.All, "a")]
    public void QuotedField_IsTrimmedAccordingToOptions(ValueTrimmingOptions trimming, string expected)
    {
        using var reader = ReadCsv.FromString("x,\"  a  \",y\n", trimmingOptions: trimming);
        Assert.True(reader.Read());
        Assert.Equal(expected, reader.GetString(1));
        Assert.Equal("y", reader.GetString(2));
    }

    [Fact]
    public void QuotedField_WithWhitespaceAndTextAfterClosingQuote_KeepsContentOrder()
    {
        using var reader = ReadCsv.FromString("\"abc\"  x\",d\n", quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.Ignore);
        Assert.True(reader.Read());
        Assert.Equal("abc\"  x", reader.GetString(0));
        Assert.Equal("d", reader.GetString(1));
    }

    [Fact]
    public void QuotedField_AdvanceToNextLine_DoesNotLeakIntoNextLine()
    {
        const string Data = "\"abc\"x,1\n\"def\",2\n";
        using var reader = ReadCsv.FromString(Data, quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.AdvanceToNextLine);
        Assert.True(reader.Read());
        Assert.Equal("def", reader.GetString(0));
        Assert.Equal("2", reader.GetString(1));
        Assert.False(reader.Read());
    }

    [Fact]
    public void QuotedFirstLine_WithoutHeaders_IsValidWhenYielded()
    {
        var input = "\"first\";\"line\"\n" + string.Concat(Enumerable.Repeat("\"" + new string('z', 50) + "\";\"v\"\n", 2000));
        using var reader = ReadCsv.FromString(input, delimiter: ';', hasHeaders: false);
        Assert.True(reader.Read());
        Assert.Equal("first", reader.GetString(0));
        Assert.Equal("line", reader.GetString(1));
        var count = 1;
        while (reader.Read())
        {
            Assert.Equal(new string('z', 50), reader.GetString(0));
            count++;
        }
        Assert.Equal(2001, count);
    }

    [Fact]
    public void QuotedHeader_IsReadCorrectly_AndFirstRowIsValid()
    {
        var input = "\"h1\";\"h 2\"\n\"v1\";\"v2\"\n";
        using var reader = ReadCsv.FromString(input, delimiter: ';', hasHeaders: true);
        Assert.Equal("h 2", reader.GetName(1));
        Assert.True(reader.Read());
        Assert.Equal("v1", reader.GetString(0));
        Assert.Equal("v2", reader["h 2"]);
    }

    [Fact]
    public void WideLines_WithoutHeaders_EachLineHasItsOwnValues()
    {
        const int FieldCount = 40;
        var lines = Enumerable.Range(0, 3)
            .Select(l => string.Join(",", Enumerable.Range(0, FieldCount).Select(f => $"\"{l}-{f}\"")))
            .ToArray();
        using var reader = ReadCsv.FromString(string.Join("\n", lines) + "\n", hasHeaders: false);

        for (var l = 0; l < lines.Length; l++)
        {
            Assert.True(reader.Read());
            Assert.Equal(FieldCount, reader.FieldCount);
            for (var f = 0; f < FieldCount; f++)
            {
                Assert.Equal($"{l}-{f}", reader.GetString(f));
            }
        }
        Assert.False(reader.Read());
    }
}

using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BenchmarkDotNet.Attributes;

using CsvHelper;
using CsvHelper.Configuration;

using CsvTest;

using Net.Code.Csv;
using Net.Code.Csv.Impl;

using nietras.SeparatedValues;

[MemoryDiagnoser]
public class CsvReaderBenchmark
{
    private const char Separator = ';';
    private static readonly string SeparatorString = Separator.ToString();
    private static readonly string[] FirstNames = ["John", "Mary", "Kevin", "Scott", "Alice", "Bob", "Emily", "Liam", "Olivia", "Noah"];
    private static readonly string[] LastNames = ["Peters", "Smith", "Spacey", "Tiger", "Brown", "Johnson", "Miller", "Davis", "Wilson", "Taylor"];
    private static readonly string[] LoremSnippets =
    [
        "Lorem ipsum dolor sit amet",
        "consectetur adipiscing elit",
        "sed do eiusmod tempor incididunt",
        "ut labore et dolore magna aliqua",
        "Ut enim ad minim veniam",
        "quis nostrud exercitation ullamco",
        "laboris nisi ut aliquip ex ea commodo consequat",
        "Duis aute irure dolor in reprehenderit",
        "in voluptate velit esse cillum dolore",
        "eu fugiat nulla pariatur"
    ];

    //[Params(CsvBenchmarkParser.NetCodeCsvV1, CsvBenchmarkParser.NetCodeCsvV2, CsvBenchmarkParser.CsvHelper, CsvBenchmarkParser.Sep)]
    [Params(CsvBenchmarkParser.NetCodeCsvV2, CsvBenchmarkParser.CsvHelper, CsvBenchmarkParser.Sep)]
    public CsvBenchmarkParser ParserKind { get; set; }

    [Params(10000)]
    public int Rows { get; set; }

    private string _tempFile = string.Empty;
    private CsvSchema _schema = null!;
    private string[] _templateRows = [];
    private string _header = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _schema = Schema.From<MyItem>();
        LoadTemplateRows();
        _tempFile = Path.Combine(Path.GetTempPath(), $"netcodecsv-reader-bench-{Rows}-{Guid.NewGuid():N}.csv");
        GenerateInputFile();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (!string.IsNullOrEmpty(_tempFile) && File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    private IDataReader GetReader(CsvLayout layout, CsvBehaviour behaviour)
    {
        var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        var sr = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var cultureInfo = CultureInfo.InvariantCulture;
        return  new Net.Code.Csv.Impl.CsvDataReader(sr, layout, behaviour, cultureInfo);
    }

    [Benchmark(Description = "IDataReader (no schema)")]
    public decimal ReadDataReader()
    {
        if (ParserKind == CsvBenchmarkParser.CsvHelper)
        {
            return ReadDataReaderCsvHelper();
        }
        if (ParserKind == CsvBenchmarkParser.Sep)
        {
            return ReadDataReaderSep();
        }

        var layout = new CsvLayout(Delimiter: Separator, HasHeaders: true);
        var behaviour = new CsvBehaviour(
            TrimmingOptions: ValueTrimmingOptions.All,
            Parser: ParserKind == CsvBenchmarkParser.NetCodeCsvV1 ? CsvParserKind.V1 : CsvParserKind.V2);
        using var reader = GetReader(layout, behaviour);
       
        var priceOrdinal = reader.GetOrdinal("Price");
        decimal total = 0;
        while (reader.Read())
        {
            total += reader.GetDecimal(priceOrdinal);
        }
        return total;
    }

    [Benchmark(Description = "Typed records (schema)")]
    public decimal ReadTypedRecords()
    {
        if (ParserKind == CsvBenchmarkParser.CsvHelper)
        {
            return ReadTypedRecordsCsvHelper();
        }
        if (ParserKind == CsvBenchmarkParser.Sep)
        {
            return ReadTypedRecordsSep();
        }
        if (ParserKind == CsvBenchmarkParser.NetCodeCsvV2)
        {
            var behaviour = new CsvBehaviour(TrimmingOptions: ValueTrimmingOptions.All);
            decimal total = 0;
            foreach (var item in ReadCsv.FromFile<MyItem>(_tempFile, delimiter: Separator, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.All, behaviour: behaviour))
            {
                total += item.Price;
            }
            return total;
        }
        else
        {
            var layout = new CsvLayout(Delimiter: Separator, HasHeaders: true, Schema: _schema);
            var behaviour = new CsvBehaviour(
                TrimmingOptions: ValueTrimmingOptions.All,
                Parser: ParserKind == CsvBenchmarkParser.NetCodeCsvV1 ? CsvParserKind.V1 : CsvParserKind.V2);
            using var reader = GetReader(layout, behaviour);

            decimal total = 0;
            foreach (var item in reader.AsEnumerable<MyItem>())
            {
                total += item.Price;
            }
            return total;
        }
    }

    [Benchmark(Description = "No headers / layout detection")]
    public int ReadWithoutHeaders()
    {
        if (ParserKind == CsvBenchmarkParser.CsvHelper)
        {
            return ReadWithoutHeadersCsvHelper();
        }
        if (ParserKind == CsvBenchmarkParser.Sep)
        {
            return ReadWithoutHeadersSep();
        }

        var layout = new CsvLayout(Delimiter: Separator, HasHeaders: false);
        var behaviour = new CsvBehaviour(
            TrimmingOptions: ValueTrimmingOptions.All,
            Parser: ParserKind == CsvBenchmarkParser.NetCodeCsvV1 ? CsvParserKind.V1 : CsvParserKind.V2);
        using var reader = GetReader(layout, behaviour);

        int rows = 0;
        while (reader.Read())
        {
            rows++;
        }
        return rows;
    }

    private decimal ReadDataReaderCsvHelper()
    {
        using var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = SeparatorString,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        using var dataReader = new CsvHelper.CsvDataReader(csv);
        var priceOrdinal = dataReader.GetOrdinal("Price");
        decimal total = 0;
        while (dataReader.Read())
        {
            total += dataReader.GetDecimal(priceOrdinal);
        }
        return total;
    }

    private decimal ReadDataReaderSep()
    {
        using var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var sep = new Sep(Separator);
        using var csv = sep.Reader(options => options with { HasHeader = true, Trim = SepTrim.All }).From(reader);
        var priceIndex = csv.Header.IndexOf("Price");
        decimal total = 0;
        foreach (var row in csv)
        {
            total += row[priceIndex].Parse<decimal>();
        }
        return total;
    }

    private decimal ReadTypedRecordsCsvHelper()
    {
        using var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = SeparatorString,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        decimal total = 0;
        foreach (var item in csv.GetRecords<MyItem>())
        {
            total += item.Price;
        }
        return total;
    }

    private decimal ReadTypedRecordsSep()
    {
        using var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var sep = new Sep(Separator);
        using var csv = sep.Reader(options => options with { HasHeader = true, Trim = SepTrim.All }).From(reader);

        var firstIndex = csv.Header.IndexOf("First");
        var lastIndex = csv.Header.IndexOf("Last");
        var birthIndex = csv.Header.IndexOf("BirthDate");
        var quantityIndex = csv.Header.IndexOf("Quantity");
        var priceIndex = csv.Header.IndexOf("Price");
        var descriptionIndex = csv.Header.IndexOf("Description");
        var intIndex = csv.Header.IndexOf("IntValue");
        var doubleIndex = csv.Header.IndexOf("DoubleValue");
        var durationIndex = csv.Header.IndexOf("Duration");

        decimal total = 0;
        foreach (var row in csv)
        {
            var item = new MyItem(
                row[firstIndex].ToString(),
                row[lastIndex].ToString(),
                row[birthIndex].Parse<DateTime>(),
                row[quantityIndex].Parse<int>(),
                row[priceIndex].Parse<decimal>(),
                row[descriptionIndex].ToString(),
                row[intIndex].Parse<int>(),
                row[doubleIndex].Parse<double>(),
                row[durationIndex].Parse<TimeSpan>());
            total += item.Price;
        }
        return total;
    }

    private int ReadWithoutHeadersCsvHelper()
    {
        using var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = SeparatorString,
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        int rows = 0;
        while (csv.Read())
        {
            rows++;
        }
        return rows;
    }

    private int ReadWithoutHeadersSep()
    {
        using var stream = new FileStream(
            _tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, bufferSize: 32 * 1024);
        var sep = new Sep(Separator);
        using var csv = sep.Reader(options => options with { HasHeader = false, Trim = SepTrim.All }).From(reader);

        int rows = 0;
        foreach (var row in csv)
        {
            rows++;
        }
        return rows;
    }

    private void LoadTemplateRows()
    {
        _header = "First;Last;BirthDate;Quantity;Price;Description;IntValue;DoubleValue;Duration";

        var random = new Random(12345);
        var templateCount = Math.Min(256, Math.Max(16, Rows / 10));
        var rows = new string[templateCount];
        for (var i = 0; i < templateCount; i++)
        {
            rows[i] = BuildRow(random);
        }
        _templateRows = rows;
    }

    private void GenerateInputFile()
    {
        using var stream = File.Create(_tempFile);
        using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 32 * 1024);

        writer.WriteLine(_header);
        for (int i = 0; i < Rows; i++)
        {
            writer.WriteLine(_templateRows[i % _templateRows.Length]);
        }
    }

    private static string BuildRow(Random random)
    {
        var first = FirstNames[random.Next(FirstNames.Length)];
        var trailingSpaces = random.Next(0, 4);
        if (trailingSpaces > 0)
        {
            first += new string(' ', trailingSpaces);
        }

        var last = LastNames[random.Next(LastNames.Length)];
        var birthDate = new DateTime(1950 + random.Next(0, 50), 1 + random.Next(0, 12), 1 + random.Next(0, 28));
        var quantity = random.Next(1, 1000);
        var price = Math.Round(random.NextDouble() * 100, 2);

        var description = BuildDescription(random);
        var intValue = random.Next(0, 1_000_000);
        var doubleValue = Math.Round(random.NextDouble() * 10_000, 3);
        var duration = TimeSpan.FromSeconds(random.Next(0, 6 * 3600) + random.Next(0, 60));

        return string.Join(SeparatorString,
            first,
            last,
            birthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            quantity.ToString(CultureInfo.InvariantCulture),
            price.ToString("0.00", CultureInfo.InvariantCulture),
            Quote(description),
            intValue.ToString(CultureInfo.InvariantCulture),
            doubleValue.ToString("0.###", CultureInfo.InvariantCulture),
            duration.ToString("c", CultureInfo.InvariantCulture));
    }

    private static string BuildDescription(Random random)
    {
        var part1 = LoremSnippets[random.Next(LoremSnippets.Length)];
        var part2 = LoremSnippets[random.Next(LoremSnippets.Length)];
        var part3 = LoremSnippets[random.Next(LoremSnippets.Length)];
        var extra = random.NextDouble() < 0.3 ? " \"quoted\"" : string.Empty;
        return $"{part1} {part2};{Environment.NewLine}{part3}{extra}";
    }

    private static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

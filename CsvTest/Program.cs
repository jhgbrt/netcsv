using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvTest;
using Net.Code.Csv;
using Net.Code.Csv.Impl;
using nietras.SeparatedValues;

BenchmarkSwitcher.FromAssembly(typeof(CsvReaderBenchmark).Assembly).Run(args);

public enum CsvBenchmarkParser
{
    NetCodeCsvV1,
    NetCodeCsvV2,
    CsvHelper,
    Sep
}

[MemoryDiagnoser]
public class CsvReaderBenchmark
{
    private const char Separator = ';';
    private static readonly string SeparatorString = Separator.ToString();
    private static readonly CsvHelperCustomConverter CsvHelperCustomConverterInstance = new();

    [Params(CsvBenchmarkParser.NetCodeCsvV1, CsvBenchmarkParser.NetCodeCsvV2, CsvBenchmarkParser.CsvHelper, CsvBenchmarkParser.Sep)]
    public CsvBenchmarkParser ParserKind { get; set; }

    [Params(1_000, 100_000)]
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
        var behaviour = new CsvBehaviour(Parser: ParserKind == CsvBenchmarkParser.NetCodeCsvV1 ? CsvParserKind.V1 : CsvParserKind.V2);
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

        var layout = new CsvLayout(Delimiter: Separator, HasHeaders: true, Schema: _schema);
        var behaviour = new CsvBehaviour(Parser: ParserKind == CsvBenchmarkParser.NetCodeCsvV1 ? CsvParserKind.V1 : CsvParserKind.V2);
        using var reader = GetReader(layout, behaviour);

        decimal total = 0;
        foreach (var item in reader.AsEnumerable<MyItem>())
        {
            total += item.Price;
        }
        return total;
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
        var behaviour = new CsvBehaviour(Parser: ParserKind == CsvBenchmarkParser.NetCodeCsvV1 ? CsvParserKind.V1 : CsvParserKind.V2);
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
            HasHeaderRecord = true
        };

        using var csv = new CsvReader(reader, config);
        csv.Context.TypeConverterCache.AddConverter<Custom>(CsvHelperCustomConverterInstance);

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
        using var csv = sep.Reader(options => options with { HasHeader = true }).From(reader);
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
            HasHeaderRecord = true
        };

        using var csv = new CsvReader(reader, config);
        csv.Context.TypeConverterCache.AddConverter<Custom>(CsvHelperCustomConverterInstance);

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
        using var csv = sep.Reader(options => options with { HasHeader = true }).From(reader);

        var firstIndex = csv.Header.IndexOf("First");
        var lastIndex = csv.Header.IndexOf("Last");
        var birthIndex = csv.Header.IndexOf("BirthDate");
        var quantityIndex = csv.Header.IndexOf("Quantity");
        var priceIndex = csv.Header.IndexOf("Price");

        decimal total = 0;
        foreach (var row in csv)
        {
            var item = new MyItem(
                row[firstIndex].ToString(),
                new Custom(row[lastIndex].ToString()),
                row[birthIndex].Parse<DateTime>(),
                row[quantityIndex].Parse<int>(),
                row[priceIndex].Parse<decimal>());
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
            HasHeaderRecord = false
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
        using var csv = sep.Reader(options => options with { HasHeader = false }).From(reader);

        int rows = 0;
        foreach (var row in csv)
        {
            rows++;
        }
        return rows;
    }

    private void LoadTemplateRows()
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory, "test.csv");
        if (!File.Exists(samplePath))
        {
            throw new FileNotFoundException("Sample CSV not found for benchmark input generation.", samplePath);
        }

        var lines = File.ReadAllLines(samplePath);
        _header = lines.First();
        _templateRows = lines.Skip(1).ToArray();
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
}

namespace CsvTest
{
    class MyFormatProvider : IFormatProvider
    {
        public object GetFormat(Type formatType) => formatType == typeof(DateTimeFormatInfo) ? this : null;
    }

    public class CustomTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s) return new Custom(s);
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is Custom c) return c.Value;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public sealed class CsvHelperCustomConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            return new Custom(text ?? string.Empty);
        }

        public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is Custom c)
            {
                return c.Value;
            }

            return base.ConvertToString(value, row, memberMapData);
        }
    }

    [TypeConverter(typeof(CustomTypeConverter))]
    public struct Custom
    {
        public Custom(string value) { Value = value; }
        public string Value { get; set; }
        public override string ToString() => Value;
    }

    [TypeConverter(typeof(AmountConverter))]
    public struct Amount
    {
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
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(Amount);
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

    public record MyItem(string First, Custom Last, DateTime BirthDate, int Quantity, decimal Price);
}

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvTest;
using Net.Code.Csv;

BenchmarkSwitcher.FromAssembly(typeof(CsvReaderBenchmark).Assembly).Run(args);

[MemoryDiagnoser]
public class CsvReaderBenchmark
{
    private const char Separator = ';';

    [Params(1_000, 100_000)]
    public int Rows { get; set; }

    private string _tempFile = string.Empty;
    private CsvSchema _schema = null!;
    private string[] _templateRows = Array.Empty<string>();
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

    [Benchmark(Description = "IDataReader (no schema)")]
    public decimal ReadDataReader()
    {
        using var reader = ReadCsv.FromFile(
            _tempFile,
            encoding: Encoding.UTF8,
            delimiter: Separator,
            hasHeaders: true);

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
        using var reader = ReadCsv.FromFile(
            _tempFile,
            encoding: Encoding.UTF8,
            delimiter: Separator,
            hasHeaders: true,
            schema: _schema);

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
        using var reader = ReadCsv.FromFile(
            _tempFile,
            encoding: Encoding.UTF8,
            delimiter: Separator,
            hasHeaders: false);

        int rows = 0;
        while (reader.Read())
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

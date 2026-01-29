using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Net.Code.Csv;

namespace CsvTest;

[MemoryDiagnoser]
public class CsvWriterBenchmark
{
    private const char Separator = ';';

    [Params(1_000, 100_000)]
    public int Rows { get; set; }

    private List<MyItem> _items = null!;
    private CultureInfo _culture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _culture = CultureInfo.InvariantCulture;
        _items = new List<MyItem>(Rows);
        var startDate = new DateTime(1990, 1, 1);
        for (var i = 0; i < Rows; i++)
        {
            _items.Add(new MyItem(
                $"First{i}",
                new Custom($"Last{i}"),
                startDate.AddDays(i % 3650),
                i % 100,
                i * 1.23m,
                $"Lorem ipsum {i}",
                i,
                i * 0.5,
                TimeSpan.FromSeconds(i % 3600)));
        }
    }

    [Benchmark(Description = "Write stream (headers)")]
    public long WriteStream()
    {
        using var stream = new MemoryStream();
        WriteCsv.ToStream(
            _items,
            stream,
            encoding: Encoding.UTF8,
            delimiter: Separator,
            hasHeaders: true,
            cultureInfo: _culture);
        return stream.Length;
    }
}

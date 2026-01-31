using System;
using System.Linq;

using BenchmarkDotNet.Running;

if (args.Contains("--profile-v2", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Running profiler modus");
    RunProfilerWorkload(CsvBenchmarkParser.NetCodeCsvV2);
    return;
}
else
{ 
    BenchmarkSwitcher.FromAssembly(typeof(CsvReaderBenchmark).Assembly).Run(args); 
}

static void RunProfilerWorkload(CsvBenchmarkParser parser)
{
    var bench = new CsvReaderBenchmark
    {
        ParserKind = parser,
        Rows = 100000
    };

    bench.Setup();
    try
    {
        _ = bench.ReadDataReader();
        _ = bench.ReadTypedRecords();
        _ = bench.ReadWithoutHeaders();
    }
    finally
    {
        bench.Cleanup();
    }
}

public enum CsvBenchmarkParser
{
    NetCodeCsvV1,
    NetCodeCsvV2,
    CsvHelper,
    Sep
}


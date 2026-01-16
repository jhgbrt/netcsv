using System;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace Net.Code.Csv.Tests.Unit.SampleFiles;

[Collection("A_NoCodePages")]
public class EncodingFallbackTests
{
    [Fact]
    public void AutoDetect_UnsupportedCodePage_FallsBackToUtf8()
    {
        Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(1252));

        using var stream = GetResourceStream("ansi.txt");
        using var reader = ReadCsv.FromStream(stream, delimiter: ';', hasHeaders: true);

        reader.Read();
        Assert.Equal("Belgi\uFFFD", reader[1]);
    }

    private static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"Net.Code.Csv.Tests.Unit.SampleFiles.{resourceName}");
    }
}

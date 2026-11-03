using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Xunit;

namespace Net.Code.Csv.Tests.Unit.Csv;

public class EncodingDetectionTests
{
    // Encoding detection only samples the first 16 KB. When that sample is pure ASCII,
    // non-ASCII UTF-8 characters later in the file must still be decoded correctly.
    private static byte[] AsciiPrefixThenUtf8(string tail)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 2000; i++)
        {
            sb.Append("abcdefghij\n");
        }
        sb.Append(tail).Append('\n');
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(sb.ToString());
    }

    private static string LastValue(IDataReader reader)
    {
        string last = null;
        while (reader.Read())
        {
            last = reader.GetString(0);
        }
        return last;
    }

    [Fact]
    public void FromFile_WhenEncodingNotSpecified_AndFirst16KbIsAscii_DecodesLaterUtf8()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, AsciiPrefixThenUtf8("café"));
            using var reader = ReadCsv.FromFile(path);
            Assert.Equal("café", LastValue(reader));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromStream_WhenEncodingNotSpecified_AndFirst16KbIsAscii_DecodesLaterUtf8()
    {
        using var stream = new MemoryStream(AsciiPrefixThenUtf8("café"));
        using var reader = ReadCsv.FromStream(stream);
        Assert.Equal("café", LastValue(reader));
    }

    [Fact]
    public void FromFileTyped_WhenEncodingNotSpecified_AndFirst16KbIsAscii_DecodesLaterUtf8()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, AsciiPrefixThenUtf8("café"));
            var last = ReadCsv.FromFile<Row>(path).Last();
            Assert.Equal("café", last.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public record Row(string Value);
}

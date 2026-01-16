using System;
using System.IO;
using System.Text;
using System.Reflection;
using Xunit;

namespace Net.Code.Csv.Tests.Unit.SampleFiles;

[Collection("B_WithCodePages")]
public class EncodingDetectionTests
{
    static EncodingDetectionTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    
    [Fact]
    public void DetectsUtf8EncodingAutomatically()
    {
        // Get the UTF-8 file from the embedded resources
        using var stream = GetResourceStream("utf8.txt");
        using var reader = ReadCsv.FromStream(stream, delimiter: ';', hasHeaders: true);
        
        reader.Read();
        Assert.Equal("België", reader[1]);
    }
    
    [Fact]
    public void DetectsAnsiEncodingAutomatically()
    {
        using var stream = GetResourceStream("ansi.txt");
        using var reader = ReadCsv.FromStream(stream, delimiter: ';', hasHeaders: true);
        
        reader.Read();
        Assert.Equal("België", reader[1]); 
    }
    
    [Fact]
    public void DetectsWindows1253EncodingAutomatically()
    {
        using var stream = GetResourceStream("1253.txt");
        using var reader = ReadCsv.FromStream(stream, delimiter: ';', hasHeaders: true);
        
        reader.Read();
        Assert.Equal("Βέλγιο", reader[1]);
    }
    
    [Fact]
    public void ExplicitEncodingOverridesDetection()
    {
        using var stream = GetResourceStream("utf8.txt");
        using var reader = ReadCsv.FromStream(stream, encoding: Encoding.ASCII, delimiter: ';', hasHeaders: true);
        
        reader.Read();
        // When forcing ASCII, the ë in België will be replaced with a fallback character
        Assert.NotEqual("België", reader[1]);
    }
    
    [Fact]
    public void HandlesEmptyStream()
    {
        using var emptyStream = new MemoryStream();
        using var reader = ReadCsv.FromStream(emptyStream);
        
        // Should not throw, just return empty result
        Assert.False(reader.Read());
    }

    [Fact]
    public void HandlesTextWithMultipleEncodings()
    {
        // Comma in CSV is a delimiter, so we need to use something else as text separator
        string mixedText = "English text| Немецкий текст| 日本語テキスト"; 
        byte[] bytes = Encoding.UTF8.GetBytes(mixedText);
        
        using var stream = new MemoryStream(bytes);
        using var reader = ReadCsv.FromStream(stream);
        
        reader.Read();
        Assert.Equal(mixedText, reader[0]);
    }

    [Fact]
    public void HandlesStreamWithoutSeek()
    {
        string content = "test1,test2\nvalue1,value2";
        var bytes = Encoding.UTF8.GetBytes(content);
        
        using var memoryStream = new MemoryStream(bytes);
        using var nonSeekableStream = new NonSeekableStream(memoryStream);
        using var reader = ReadCsv.FromStream(nonSeekableStream);
        
        // Should default to UTF8 and read correctly without seeking
        Assert.True(reader.Read());
        Assert.Equal("test1", reader[0]);
        Assert.Equal("test2", reader[1]);
    }

    [Theory]
    [InlineData("ASCII")]
    [InlineData("ISO-8859-1")]
    [InlineData("Windows-1252")]
    [InlineData("utf-8")]
    [InlineData("utf-16")]
    public void DetectsCommonEncodings(string encodingName)
    {
        string testData = "Column1,Column2\nValue1,Value2";
        var encoding = Encoding.GetEncoding(encodingName);
        var bytes = encoding.GetBytes(testData);
        
        using var stream = new MemoryStream(bytes);
        using var reader = ReadCsv.FromStream(stream, hasHeaders: true);
        
        reader.Read();
        Assert.Equal("Value1", reader[0]);
        Assert.Equal("Value2", reader[1]);
    }

    [Fact]
    public void DetectsBomCorrectlyWhenPresent()
    {
        // Create a UTF-8 file with BOM
        string text = "Header1,Header2\nData1,Data2";
        byte[] bomBytes = Encoding.UTF8.GetPreamble(); // Get the UTF-8 BOM
        byte[] contentBytes = Encoding.UTF8.GetBytes(text);
        
        byte[] bytesWithBom = new byte[bomBytes.Length + contentBytes.Length];
        Buffer.BlockCopy(bomBytes, 0, bytesWithBom, 0, bomBytes.Length);
        Buffer.BlockCopy(contentBytes, 0, bytesWithBom, bomBytes.Length, contentBytes.Length);
        
        using var stream = new MemoryStream(bytesWithBom);
        using var reader = ReadCsv.FromStream(stream, hasHeaders: true);
        
        reader.Read();
        Assert.Equal("Data1", reader[0]);
        Assert.Equal("Data2", reader[1]);
    }

    private Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"Net.Code.Csv.Tests.Unit.SampleFiles.{resourceName}");
    }
}

// A helper stream class that doesn't support seeking
public class NonSeekableStream : Stream
{
    private readonly Stream _baseStream;

    public NonSeekableStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => false; // This is the key - we don't support seeking
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;
    public override long Position
    {
        get => _baseStream.Position;
        set => throw new NotSupportedException("Seek not supported");
    }

    public override void Flush() => _baseStream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Seek not supported");
    public override void SetLength(long value) => _baseStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
}

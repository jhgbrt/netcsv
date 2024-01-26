using System.Reflection;

namespace Net.Code.Csv.Tests.Unit.SampleFiles;


public class EncodingTests
{
    static EncodingTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }


    [Fact]
    public void Cp1253()
    {
        Test("1253", Encoding.GetEncoding(1253), "Βέλγιο");
    }
    [Fact]
    public void Utf8()
    {
        Test("utf8", Encoding.UTF8, "België");
    }
    [Fact]
    public void Ansi()
    {
        Test("ansi", Encoding.GetEncoding(1252), "België");
    }
    [Fact]
    public void Utf7()
    {
#pragma warning disable SYSLIB0001 // Type or member is obsolete
        Test("utf7", Encoding.UTF7, "België");
#pragma warning restore SYSLIB0001 // Type or member is obsolete
    }

    private static void Test(string encodingName, Encoding encoding, string expected)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Net.Code.Csv.Tests.Unit.SampleFiles." + encodingName + ".txt";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = ReadCsv.FromStream(stream, encoding: encoding, delimiter: ';', hasHeaders: true);
        reader.Read();
        Assert.Equal(expected, reader[1]);
    }
}

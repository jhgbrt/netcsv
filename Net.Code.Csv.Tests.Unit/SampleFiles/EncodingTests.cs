using System.Reflection;

namespace Net.Code.Csv.Tests.Unit.SampleFiles;

[TestFixture]
public class EncodingTests
{
    [OneTimeSetUp]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }


    [Test]
    public void Cp1253()
    {
        Test("1253", Encoding.GetEncoding(1253), "Βέλγιο");
    }
    [Test]
    public void Utf8()
    {
        Test("utf8", Encoding.UTF8, "België");
    }
    [Test]
    public void Ansi()
    {
        Test("ansi", Encoding.GetEncoding(1252), "België");
    }
    [Test]
    public void Utf7()
    {
        Test("utf7", Encoding.UTF7, "België");
    }

    private static void Test(string encodingName, Encoding encoding, string expected)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Net.Code.Csv.Tests.Unit.SampleFiles." + encodingName + ".txt";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = ReadCsv.FromStream(stream, encoding: encoding, delimiter: ';', hasHeaders: true))
        {
            reader.Read();
            Assert.AreEqual(expected, reader[1]);
        }
    }
}

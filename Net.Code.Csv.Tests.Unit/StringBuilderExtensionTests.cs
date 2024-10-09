namespace Net.Code.Csv.Tests.Unit;

[TestFixture]
public class StringBuilderExtensionTests
{
    [Test]
    public void Quote_Null_Throws()
    {
        StringBuilder sb = null;
        Assert.Throws<NullReferenceException>(() => sb.QuoteIfNecessary('"', ';', '\\'));
    }

    [TestCase("", "")]
    [TestCase("abc", "abc")]
    [TestCase("ab;c", "'ab;c'")]
    [TestCase("ab\rc", "'ab\rc'")]
    [TestCase("ab\nc", "'ab\nc'")]
    [TestCase("ab\'c", "'ab\\'c'")]
    public void QuoteIfNecessaryTests(string input, string expectedResult)
    {
        var sb = new StringBuilder(input);
        var result = sb.QuoteIfNecessary('\'', ';', '\\').ToString();
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("  ", "")]
    [TestCase("  x", "x")]
    [TestCase("x  ", "x")]
    [TestCase("  x  ", "x")]
    public void TrimTests(string input, string expected)
    {
        var sb = new StringBuilder(input);
        var result = sb.Trim();
        Assert.That(result, Is.EqualTo(expected));
    }
}


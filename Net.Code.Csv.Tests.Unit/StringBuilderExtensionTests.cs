namespace Net.Code.Csv.Tests.Unit;


public class StringBuilderExtensionTests
{
    [Fact]
    public void Quote_Null_Throws()
    {
        StringBuilder sb = null;
        Assert.Throws<NullReferenceException>(() => sb.QuoteIfNecessary('"', ';', '\\'));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("ab;c", "'ab;c'")]
    [InlineData("ab\rc", "'ab\rc'")]
    [InlineData("ab\nc", "'ab\nc'")]
    [InlineData("ab\'c", "'ab\\'c'")]
    public void QuoteIfNecessaryTests(string input, string expectedResult)
    {
        var sb = new StringBuilder(input);
        var result = sb.QuoteIfNecessary('\'', ';', '\\').ToString();
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("  x", "x")]
    [InlineData("x  ", "x")]
    [InlineData("  x  ", "x")]
    public void TrimTests(string input, string expected)
    {
        var sb = new StringBuilder(input);
        var result = sb.Trim();
        Assert.Equal(expected, result);
    }
}


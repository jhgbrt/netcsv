using Xunit;

using Net.Code.Csv.Impl;

namespace Net.Code.Csv.Tests.Unit.Csv;

public class ReadCsvTests
{
    private const char VisibleSpace = '\u00B7';

    private static string DotToSpace(string value) => value.Replace(VisibleSpace, ' ');

    private static string[] Read(string data,
        char? quote = '"',
        char delimiter = ',',
        char escape = '"',
        char comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.None,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        QuotesInsideQuotedFieldAction quoteInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        bool skipEmptyLines = false,
        CsvBehaviour behaviour = null)
    {
        var reader = ReadCsv.FromString(data,
            quote,
            delimiter,
            escape,
            comment,
            hasHeaders,
            trimmingOptions,
            missingFieldAction,
            skipEmptyLines ? EmptyLineAction.Skip : EmptyLineAction.None,
            quoteInsideQuotedFieldAction,
            behaviour: behaviour);
        reader.Read();
        string[] results = new string[reader.FieldCount];
        reader.GetValues(results);
        return results;
    }

    [Fact]
    public void NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Read(null));
    }

    [Fact]
    public void EmptyString_WhenSkipEmptyLinesIsFalse_ReadsEmptyStringIntoEmptyField()
    {
        var data = "";
        var result = Read(data, skipEmptyLines: false);
        Assert.Equal(new string[] { string.Empty }, result);
    }

    [Fact]
    public void EmptyString_WhenSkipEmptyLinesIsTrue_EmptyStringIsNotRead()
    {
        var data = "";
        var result = ReadCsv.FromString(data, emptyLineAction: EmptyLineAction.Skip);
        Assert.False(result.Read());
    }

    [Fact]
    public void SingleDelimiter_IsReadAsTwoEmptyFields()
    {
        var data = ",";
        var result = Read(data);
        Assert.Equal(new string[] { string.Empty, string.Empty }, result);
    }

    [Theory]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData('\t')]
    [InlineData('|')]
    [InlineData('x')]
    public void AllowedDelimiterCharacters(char delimiter)
    {
        var data = $"{delimiter}";
        var result = Read(data, delimiter: delimiter);
        Assert.Equal(new string[] { string.Empty, string.Empty }, result);
    }

    [Fact]
    public void SimpleStringField_IsReadAsString()
    {
        var data = "x";
        var result = Read(data);
        Assert.Equal(new string[] { "x" }, result);
    }
    [Fact]
    public void QuotedStringField_IsReadAsString()
    {
        var data = """
            "x"
            """;
        var result = Read(data);
        Assert.Equal(new string[] { "x" }, result);
    }

    [Fact]
    public void QuotedStringCanContainNewline()
    {
        var data = """
            "x
            "
            """;
        var result = Read(data);
        Assert.Equal(new string[] { """
            x

            """ }, result);
    }

    [Fact]
    public void QuotedStringCanContainEscapedQuote()
    {
        var data = """
            "a""b"
            """;
        var result = Read(data);
        Assert.Equal(["a\"b"], result);
    }

    [Fact]
    public void QuotedString_WhenUnescapedQuoteEncounterd_AndShouldIgnore_ThenFieldContainsQuote()
    {
        var data = """
            "a"b"
            """;
        var result = Read(data, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.Ignore);
        Assert.Equal(["a\"b"], result);
    }
    [Fact]
    public void QuotedString_WhenUnescapedQuoteEncounterd_AndShouldThrow_ThenThrows()
    {
        var data = """
            "a"b"
            """;
        Assert.Throws<MalformedCsvException>(() => Read(data, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.ThrowException));
    }
    [Fact]
    public void QuotedString_WhenUnescapedQuoteEncounterd_AndAdvanceToNextLine_ThenIgnoresRestOfField()
    {
        var data = """
            "a"b"
            """;
        var result = Read(data, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.AdvanceToNextLine);
        Assert.Equal(["a"], result);
    }

    [Fact]
    public void CommentIsIgnored()
    {
        var data = "#whatever";
        var result = Read(data);
        Assert.Equal([string.Empty], result);
    }

    [Fact]
    public void TwoFields()
    {
        var data = "a,b";
        var result = Read(data);
        Assert.Equal(["a", "b"], result);
    }
    [Fact]
    public void TwoFields_FirstFieldWithNewLine()
    {
        var data = """
            "a
            ",b
            """;
        var result = Read(data);
        Assert.Equal(["""
            a

            """, "b"], result);
    }
    [Fact]
    public void TwoFields_SecondFieldWithQuotes()
    {
        var data = """
            "a
            ","b"
            """;
        var result = Read(data);
        Assert.Equal(["""
            a

            """, "b"], result);
    }
    [Fact]
    public void TwoEmptyLines_ResultInEmptyResult()
    {
        var data = """


            """;
        var result = Read(data);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("\"\"", "")]
    [InlineData("\"\"\"\"", "\"")]
    [InlineData("\"\"\"\"\"\"", "\"\"")]
    [InlineData("\"a\"", "a")]
    [InlineData("\"a\"\"a\"", "a\"a")]
    [InlineData("\"a\"\"a\"\"a\"", "a\"a\"a")]
    [InlineData("a\"\"a", "a\"\"a")]
    [InlineData("a\"a\"a", "a\"a\"a")]
    [InlineData("\u00B7\"\"\u00B7", "\u00B7\"\"\u00B7")]
    [InlineData("\u00B7\"a\"\u00B7", "\u00B7\"a\"\u00B7")]
    [InlineData("\u00B7\"\"", "\u00B7\"\"")]
    [InlineData("\u00B7\"a\"", "\u00B7\"a\"")]
    [InlineData("a\"\"\"a", "a\"\"\"a")]
    [InlineData("\"a\"a\"a\"", "a\"a\"a")]
    [InlineData("\"\"\u00B7", "")]
    [InlineData("\"a\"\u00B7", "a")]
    [InlineData("\"a\"\"\"a", "a\"\"a")]
    [InlineData("\"a\"\"\"a\"", "a\"\"a")]
    [InlineData("\"\"a\"", "\"a")]
    [InlineData("\"a\"a\"", "a\"a")]
    [InlineData("\"\"a\"a\"\"", "\"a\"a\"")]
    [InlineData("\"\"\"", "\"")]
    [InlineData("\"\"\"\"\"", "\"\"")]
    public void UnescapeEdgeCases_AreParsedAsExpected(string data, string expected)
    {
        var result = Read(DotToSpace(data), trimmingOptions: ValueTrimmingOptions.None, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.Ignore);
        expected = DotToSpace(expected);
        Assert.Equal([expected], result);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("\u00B7a", "a")]
    [InlineData("a\u00B7", "a")]
    [InlineData("\u00B7a\u00B7", "a")]
    [InlineData("\u00B7a\u00B7a\u00B7", "a\u00B7a")]
    [InlineData("\"a\"", "a")]
    [InlineData("\"\u00B7a\"", "a")]
    [InlineData("\"a\u00B7\"", "a")]
    [InlineData("\"\u00B7a\u00B7\"", "a")]
    [InlineData("\"\u00B7a\u00B7a\u00B7\"", "a\u00B7a")]
    [InlineData("\u00B7\"a\"\u00B7", "\"a\"")]
    [InlineData("\u00B7\"\u00B7a\"\u00B7", "\"\u00B7a\"")]
    [InlineData("\u00B7\"a\u00B7\"\u00B7", "\"a\u00B7\"")]
    [InlineData("\u00B7\"\u00B7a\u00B7\"\u00B7", "\"\u00B7a\u00B7\"")]
    [InlineData("\u00B7\"\u00B7a\u00B7a\u00B7\"\u00B7", "\"\u00B7a\u00B7a\u00B7\"")]
    public void TrimmingEdgeCases_AreParsedAsExpected(string data, string expected)
    {
        var result = Read(DotToSpace(data), trimmingOptions: ValueTrimmingOptions.All);
        expected = DotToSpace(expected);
        Assert.Equal([expected], result);
    }

    [Fact]
    public void StrictMode_WithTrimAll_WhitespaceBeforeOpeningQuote_Throws()
    {
        var behaviour = CsvBehaviour.Strict() with { TrimmingOptions = ValueTrimmingOptions.All };
        Assert.Throws<MalformedCsvException>(() => Read(" \"a\"", behaviour: behaviour));
    }

    [Fact]
    public void StrictMode_WithTrimAll_WhitespaceAfterClosingQuote_Throws()
    {
        var behaviour = CsvBehaviour.Strict() with { TrimmingOptions = ValueTrimmingOptions.All };
        Assert.Throws<MalformedCsvException>(() => Read("\"a\" ", behaviour: behaviour));
    }

    [Fact]
    public void LiteralMode_DisablesQuoting_ReturnsRawFields()
    {
        var behaviour = CsvBehaviour.Literal();
        var result = Read("\"a\",\"b\"", quote: null, behaviour: behaviour);
        Assert.Equal(["\"a\"", "\"b\""], result);
    }

    [Fact]
    public void LiteralMode_PreservesWhitespaceWhenTrimmingNone()
    {
        var behaviour = CsvBehaviour.Literal();
        var result = Read(" \"a\" ", quote: null, behaviour: behaviour);
        Assert.Equal([" \"a\" "], result);
    }
}

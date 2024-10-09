namespace Net.Code.Csv.Tests.Unit.Csv;

[TestFixture]
public class ReadCsvTests
{
    private static string[] Read(string data,
        char quote = '"',
        char delimiter = ',',
        char escape = '"',
        char comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.None,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        QuotesInsideQuotedFieldAction quoteInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        bool skipEmptyLines = false)
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
            quoteInsideQuotedFieldAction);
        reader.Read();
        string[] results = new string[reader.FieldCount];
        reader.GetValues(results);
        return results;
    }

    [Test]
    public void NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Read(null));
    }

    [Test]
    public void EmptyString_WhenSkipEmptyLinesIsFalse_ReadsEmptyStringIntoEmptyField()
    {
        var data = "";
        var result = Read(data, skipEmptyLines: false);
        Assert.That(result, Is.EqualTo(new string[] { string.Empty }));
    }

    [Test]
    public void EmptyString_WhenSkipEmptyLinesIsTrue_EmptyStringIsNotRead()
    {
        var data = "";
        var result = ReadCsv.FromString(data, emptyLineAction: EmptyLineAction.Skip);
        Assert.That(result.Read(), Is.False);
    }

    [Test]
    public void SingleDelimiter_IsReadAsTwoEmptyFields()
    {
        var data = ",";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new string[] { string.Empty, string.Empty }) );
    }

    [Test]
    [TestCase(',')]
    [TestCase(';')]
    [TestCase('\t')]
    [TestCase('|')]
    [TestCase('x')]
    public void AllowedDelimiterCharacters(char delimiter)
    {
        var data = $"{delimiter}";
        var result = Read(data, delimiter: delimiter);
        Assert.That(result, Is.EqualTo(new string[] { string.Empty, string.Empty }));
    }

    [Test]
    public void SimpleStringField_IsReadAsString()
    {
        var data = "x";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new [] { "x" }));
    }
    [Test]
    public void QuotedStringField_IsReadAsString()
    {
        var data = "\"x\"";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new [] { "x" }));
    }

    [Test]
    public void QuotedStringCanContainNewline()
    {
        var data = "\"x\r\n\"";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new [] { "x\r\n" }));
    }

    [Test]
    public void QuotedStringCanContainEscapedQuote()
    {
        var data = "\"a\"\"b\"";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new[] { "a\"b" }));
    }

    [Test]
    public void QuotedString_WhenUnescapedQuoteEncountered_AndShouldIgnore_ThenFieldContainsQuote()
    {
        var data = "\"a\"b\"";
        var result = Read(data, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.Ignore);
        Assert.That(result, Is.EqualTo(new[] { "a\"b" }));
    }
    [Test]
    public void QuotedString_WhenUnescapedQuoteEncountered_AndShouldThrow_ThenThrows()
    {
        var data = "\"a\"b\"";
        Assert.Throws<MalformedCsvException>(() => Read(data, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.ThrowException));
    }
    [Test]
    public void QuotedString_WhenUnescapedQuoteEncountered_AndAdvanceToNextLine_ThenIgnoresRestOfField()
    {
        var data = "\"a\"b\"";
        var result = Read(data, quoteInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.AdvanceToNextLine);
        Assert.That(result, Is.EqualTo(new[] { "a" }));
    }

    [Test]
    public void CommentIsIgnored()
    {
        var data = "#whatever";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new[] { "" }));
    }

    [Test]
    public void TwoFields()
    {
        var data = "a,b";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new string[] { "a", "b" }));
    }
    [Test]
    public void TwoFields_FirstFieldWithNewLine()
    {
        var data = "\"a\r\n\",b";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new [] { "a\r\n", "b" }));
    }
    [Test]
    public void TwoFields_SecondFieldWithQuotes()
    {
        var data = "\"a\r\n\",\"b\"";
        var result = Read(data);
        Assert.That(result, Is.EqualTo(new [] { "a\r\n", "b" }));
    }
    [Test]
    public void TwoLines()
    {
        var data = "\r\n";
        var result = Read(data);
        Assert.That(result, Is.Empty);
    }
}

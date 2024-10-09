using System.IO;

using Net.Code.Csv.Impl;
using Net.Code.Csv.Tests.Unit.IO.Csv;

namespace Net.Code.Csv.Tests.Unit.Csv;

[TestFixture]
public class CsvStateMachineTests
{
    private static string[] Split(string line) => Split(line, new CsvLayout(), new CsvBehaviour());
    private static string[] Split(string line, CsvLayout splitLineParams) => Split(line, splitLineParams, new CsvBehaviour());
    private static string[] Split(string line, CsvLayout splitLineParams, CsvBehaviour behaviour)
    {
        var splitter = new CsvStateMachine(new StringReader(line), splitLineParams, behaviour);
        var result = splitter.Lines();
        return result.First().Fields;
    }

    [Test]
    public void SplitsSimpleDelimitedLine()
    {
        var splitLineParams = new CsvLayout(Quote: '"', Delimiter: ';');
        var result = Split("1;2;3", splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "1", "2", "3" }));
    }

    [Test]
    public void TrimsTrailingWhitespaceOfUnquotedField()
    {
        var splitLineParams = new CsvLayout('"', ';');
        var result = Split("1;2;3 \t", splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "1", "2", "3" }));
    }

    [Test]
    public void DoesNotTrimTrailingWhitespaceOfQuotedField()
    {
        var splitLineParams = new CsvLayout('"', ';');
        var result = Split("1;2;\"3 \t\"", splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "1", "2", "3 \t" }));
    }

    [Test]
    public void ValueTrimmingOptions_All_TrimsWhiteSpace()
    {
        var input = "\" x \"";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.All);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.That(result.Single(), Is.EqualTo("x"));
    }

    [Test]
    public void ValueTrimmingOptions_QuotedOnly_TrimsWhiteSpaceWhenQuoted()
    {
        var input = "\" x \"";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.QuotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.That(result.Single(), Is.EqualTo("x"));
    }

    [Test]
    public void ValueTrimmingOptions_QuotedOnly_DoesNotTrimWhiteSpaceWhenNotQuoted()
    {
        var input = " x ";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.QuotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.That(result.Single(), Is.EqualTo(" x "));
    }

    [Test]
    public void ValueTrimmingOptions_UnQuotedOnly_TrimsWhiteSpaceWhenNotQuoted()
    {
        var input = " x ";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.UnquotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.That(result.Single(), Is.EqualTo("x"));
    }

    [Test]
    public void ValueTrimmingOptions_UnQuotedOnly_DoesNotTrimWhiteSpaceWhenQuoted()
    {
        var input = "\" x \"";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.UnquotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.That(result.Single(), Is.EqualTo(" x "));
    }

    [Test]
    public void StripsQuotes()
    {
        const string line = @"""FieldContent""";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "FieldContent" }));
    }

    [Test]
    public void TrimsLeadingWhitespaceFromUnquotedField()
    {
        const string line = @"x, y,z";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "x", "y", "z" }));
    }

    [Test]
    public void TrimsTrailingWhitespaceFromUnquotedField()
    {
        const string line = @"x,y   ,z";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "x", "y", "z" }));
    }

    [Test]
    public void SupportsFieldsWithEscapedQuotes()
    {
        const string line = "x \"y\",z";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "x \"y\"", "z" }));
    }

    [Test]
    public void EmptyFields()
    {
        const string line = @",x,,y";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "", "x", "", "y" }));
    }

    [Test]
    public void EmptyString()
    {
        const string line = "";
        var splitter = new CsvStateMachine(new StringReader(line), new CsvLayout(), new CsvBehaviour());
        var result = splitter.Lines();
        Assert.That(result, Is.Empty);
    }
    [Test]
    public void EmptyFields_Comma()
    {
        const string line = ",,";
        var result = Split(line, new CsvLayout());
        Assert.That(result, Is.EqualTo(new[] { "", "", "" }));
    }
    [Test]
    public void EmptyFields_Tab()
    {
        const string line = "\t\t";
        var result = Split(line, new CsvLayout(Delimiter: '\t'));
        Assert.That(result, Is.EqualTo(new[] { "", "", "" }));
    }
    [Test]
    public void QuotedStringWithDelimiter()
    {
        // "x ""y"", z"
        const string line = "\"x \"y\" z, u\",v";
        var splitLineParams = new CsvLayout();
        var result = Split(line, splitLineParams);
        Assert.That(result, Is.EqualTo(new[] { "x \"y\" z, u", "v" }));

    }

    [Test]
    public void WhenValueTrimmingIsNone_LastFieldHasLeadingAndTrailingWhitespace_WhitespaceIsNotTrimmed()
    {
        const string line = "x,y, z ";
        var splitLineParams = new CsvLayout('"', ',', '"');
        var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.That(result, Is.EqualTo(new[] { @"x", "y", " z " }));

    }


    [Test]
    public void EscapeCharacterInsideQuotedStringIsEscaped()
    {
        const string line = @"""\\""";
        var splitLineParams = new CsvLayout('"', ',', '\\');
        var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.That(result.Single(), Is.EqualTo(@"\"));
    }

    [Test]
    public void LineWithOnlySeparatorIsSplitIntoTwoEmptyStrings()
    {
        const string line = ",";
        var splitLineParams = new CsvLayout('"', ',', '\\');
        var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.That(result, Is.EqualTo(new[] { "", "" }));
    }

    [Test]
    public void CanWorkWithMultilineField()
    {
        const string data = """
            a,b,"line1
            line2"
            """;
        var splitLineParams = new CsvLayout('"', ',', '\\');
        var result = Split(data, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.That(result, Is.EqualTo(new[] { "a", "b", """
            line1
            line2
            """ }));
    }

    [Test]
    public void MultipleLinesAreSplitCorrectly()
    {
        var data1 = """
            1;2;3
            4;5;6
            """;

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.That(result[0].Fields, Is.EqualTo(new[] { "1", "2", "3" }));
        Assert.That(result[1].Fields, Is.EqualTo(new[] { "4", "5", "6" }));

    }

    [Test]
    public void WorksWithQuotedStringInsideQuotedFieldButOnlyWhitespaceAfterSecondQuote()
    {
        var data1 = @"""1"";"" 2  ""inside""   "";3";

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.That(result[0].Fields, Is.EqualTo(new[] { "1", @" 2  ""inside""   ", "3" }));

    }

    [Test]
    public void WorksWithQuotedStringInsideQuotedField()
    {
        var data1 = @"""1"";"" 2  ""inside""  x "";3";

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.That(result[0].Fields, Is.EqualTo(new[] { "1", @" 2  ""inside""  x ", "3" }));

    }

    [Test]
    public void WorksWithQuotedMultilineString()
    {
        var data1 = """
            "1";" 2  "in
            side"  x ";3
            """;

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.That(result[0].Fields, Is.EqualTo(new[] { "1", """
             2  "in
            side"  x 
            """, "3" }));

    }

    [Test]
    public void WhenSkipEmptyLinesIsFalse_ReturnsEmptyLines()
    {
        var input = "1\n\n2";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(),
                                       new CsvBehaviour(EmptyLineAction: EmptyLineAction.None));

        var result = splitter.Lines().ToArray();

        Assert.That(result[1].IsEmpty, Is.True);
    }

    [Test]
    public void WhenSkipEmptyLinesIsTrue_SkipsEmptyLines()
    {
        var input = "\r\n1\n\n2";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(),
                                       new CsvBehaviour(EmptyLineAction: EmptyLineAction.Skip));

        var result = splitter.Lines().ToArray();

        Assert.That(result[0].Fields[0], Is.EqualTo("1"));
        Assert.That(result[1].Fields[0], Is.EqualTo("2"));
    }

    [Test]
    public void WhenSkipEmptyLinesIsFalse_AndEmptyLineIsAtTheEnd_ReturnsEmptyLine()
    {
        var input = "a,b\n   ";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(EmptyLineAction: EmptyLineAction.None));

        var result = splitter.Lines().ToArray();

        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].Fields, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(result[1].IsEmpty, Is.True);
        Assert.That(result[1].Fields, Is.EqualTo(new[] { string.Empty, string.Empty }));
    }

    [Test]
    public void WhenInputContainsMultipleLinesWithTrailingEmptyField_ReturnsLinesWithEmptyField()
    {
        var input = "00,01,   \n10,11,   ";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour());
        var result = splitter.Lines().ToArray();
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].Fields, Is.EqualTo(new[] { "00", "01", "" }));
        Assert.That(result[1].Fields, Is.EqualTo(new[] { "10", "11", "" }));
    }

    [Test]
    public void Testing()
    {
        var input = "00,   ,02\n,,";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(TrimmingOptions: ValueTrimmingOptions.None));
        var result = splitter.Lines().ToArray();
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].Fields, Is.EqualTo(new[] { "00", "   ", "02" }));
    }

    [Test]
    public void WhenTrailingLineContainsMissingFields_MissingFieldActionIsReplaceByNull_LastLineIsAppendedWithNulls()
    {
        var input = "a,b,c,d,e"
                    + "\na,b,c,d,"
                    + "\na,b,";

        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(MissingFieldAction: MissingFieldAction.ReplaceByNull));
        var result = splitter.Lines().ToArray();
        Assert.That(result.Length, Is.EqualTo(3));

        Assert.That(result[0].Fields, Is.EqualTo(new[] { "a", "b", "c", "d", "e" }));
        Assert.That(result[1].Fields, Is.EqualTo(new[] { "a", "b", "c", "d", "" }));
        Assert.That(result[2].Fields, Is.EqualTo(new[] { "a", "b", "", null, null }));
    }

    [Test]
    public void SampleDataSplitTest()
    {
        var data = CsvReaderSampleData.SampleData1;

        var splitter = new CsvStateMachine(new StringReader(data), new CsvLayout(), new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        CsvReaderSampleData.CheckSampleData1(false, 0, result[0].Fields);

    }

    [Test]
    public void QuotedFieldCanContainNewLineCharacters()
    {
        const string data = "\"\n\r\n\n\r\r\",,\t,\n";
        var result = Split(data);
        Assert.That(result, Is.EqualTo(new[] { "\n\r\n\n\r\r", "", "", "" }));
    }

    [Test]
    public void CanSplitByTabs()
    {
        const string data = "1\t2\t3";
        var result = Split(data, new CsvLayout(Delimiter: '\t'));
        Assert.That(result, Is.EqualTo(new[] { "1", "2", "3" }));
    }

}

using System.IO;

using Net.Code.Csv.Impl;
using Net.Code.Csv.Impl.V1;
using Net.Code.Csv.Tests.Unit.IO.Csv;

namespace Net.Code.Csv.Tests.Unit.Csv;


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

    [Fact]
    public void SplitsSimpleDelimitedLine()
    {
        var splitLineParams = new CsvLayout(Quote: '"', Delimiter: ';');
        var result = Split("1;2;3", splitLineParams);
        Assert.Equal(new[] { "1", "2", "3" }, result);
    }

    [Fact]
    public void TrimsTrailingWhitespaceOfUnquotedField()
    {
        var splitLineParams = new CsvLayout('"', ';');
        var result = Split("1;2;3 \t", splitLineParams);
        Assert.Equal(new[] { "1", "2", "3" }, result);
    }

    [Fact]
    public void DoesNotTrimTrailingWhitespaceOfQuotedField()
    {
        var splitLineParams = new CsvLayout('"', ';');
        var result = Split("1;2;\"3 \t\"", splitLineParams);
        Assert.Equal(new[] { "1", "2", "3 \t" }, result);
    }

    [Fact]
    public void ValueTrimmingOptions_All_TrimsWhiteSpace()
    {
        var input = "\" x \"";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.All);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.Equal("x", result.Single());
    }

    [Fact]
    public void ValueTrimmingOptions_QuotedOnly_TrimsWhiteSpaceWhenQuoted()
    {
        var input = "\" x \"";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.QuotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.Equal("x", result.Single());
    }
    [Fact]
    public void ValueTrimmingOptions_QuotedOnly_DoesNotTrimWhiteSpaceWhenNotQuoted()
    {
        var input = " x ";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.QuotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.Equal(" x ", result.Single());
    }

    [Fact]
    public void ValueTrimmingOptions_UnQuotedOnly_TrimsWhiteSpaceWhenNotQuoted()
    {
        var input = " x ";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.UnquotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.Equal("x", result.Single());
    }
    [Fact]
    public void ValueTrimmingOptions_UnQuotedOnly_DoesNotTrimWhiteSpaceWhenQuoted()
    {
        var input = "\" x \"";
        var behaviour = new CsvBehaviour(ValueTrimmingOptions.UnquotedOnly);
        var result = Split(input, new CsvLayout(), behaviour);
        Assert.Equal(" x ", result.Single());
    }

    [Fact]
    public void StripsQuotes()
    {
        const string line = @"""FieldContent""";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.Equal(new[] { "FieldContent" }, result);
    }

    [Fact]
    public void TrimsLeadingWhitespaceFromUnquotedField()
    {
        const string line = @"x, y,z";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.Equal(new[] { "x", "y", "z" }, result);
    }

    [Fact]
    public void TrimsTrailingWhitespaceFromUnquotedField()
    {
        const string line = @"x,y   ,z";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.Equal(new[] { "x", "y", "z" }, result);
    }

    [Fact]
    public void SupportsFieldsWithEscapedQuotes()
    {
        const string line = "x \"y\",z";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.Equal(new[] { "x \"y\"", "z" }, result);
    }

    [Fact]
    public void EmptyFields()
    {
        const string line = @",x,,y";
        var splitLineParams = new CsvLayout('"', ',');
        var result = Split(line, splitLineParams);
        Assert.Equal(new[] { "", "x", "", "y" }, result);
    }

    [Fact]
    public void EmptyString()
    {
        const string line = "";
        var splitter = new CsvStateMachine(new StringReader(line), new CsvLayout(), new CsvBehaviour());
        var result = splitter.Lines();
        Assert.Empty(result);
    }
    [Fact]
    public void EmptyFields_Comma()
    {
        const string line = ",,";
        var result = Split(line, new CsvLayout());
        Assert.Equal(new[] { "", "", "" }, result);
    }
    [Fact]
    public void EmptyFields_Tab()
    {
        const string line = "\t\t";
        var result = Split(line, new CsvLayout(Delimiter: '\t'));
        Assert.Equal(new[] { "", "", "" }, result);
    }
    [Fact]
    public void QuotedStringWithDelimiter()
    {
        // "x ""y"", z"
        const string line = "\"x \"y\" z, u\",v";
        var splitLineParams = new CsvLayout();
        var result = Split(line, splitLineParams);
        Assert.Equal(new[] { "x \"y\" z, u", "v" }, result);

    }

    [Fact]
    public void WhenValueTrimmingIsNone_LastFieldHasLeadingAndTrailingWhitespace_WhitespaceIsNotTrimmed()
    {
        const string line = "x,y, z ";
        var splitLineParams = new CsvLayout('"', ',', '"');
        var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.Equal(new[] { @"x", "y", " z " }, result);

    }


    [Fact]
    public void EscapeCharacterInsideQuotedStringIsEscaped()
    {
        const string line = @"""\\""";
        var splitLineParams = new CsvLayout('"', ',', '\\');
        var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.Equal(@"\", result.Single());
    }

    [Fact]
    public void LineWithOnlySeparatorIsSplitIntoTwoEmptyStrings()
    {
        const string line = ",";
        var splitLineParams = new CsvLayout('"', ',', '\\');
        var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.Equal(new[] { "", "" }, result);
    }

    [Fact]
    public void CanWorkWithMultilineField()
    {
        const string data = """
            a,b,"line1
            line2"
            """;
        var splitLineParams = new CsvLayout('"', ',', '\\');
        var result = Split(data, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
        Assert.Equal(new[] { "a", "b", """
            line1
            line2
            """ }, result);
    }

    [Fact]
    public void MultipleLinesAreSplitCorrectly()
    {
        var data1 = """
            1;2;3
            4;5;6
            """;

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.Equal(new[] { "1", "2", "3" }, result[0].Fields);
        Assert.Equal(new[] { "4", "5", "6" }, result[1].Fields);

    }

    [Fact]
    public void WorksWithQuotedStringInsideQuotedFieldButOnlyWhitespaceAfterSecondQuote()
    {
        var data1 = @"""1"";"" 2  ""inside""   "";3";

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.Equal(new[] { "1", @" 2  ""inside""   ", "3" }, result[0].Fields);

    }

    [Fact]
    public void WorksWithQuotedStringInsideQuotedField()
    {
        var data1 = @"""1"";"" 2  ""inside""  x "";3";

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.Equal(new[] { "1", @" 2  ""inside""  x ", "3" }, result[0].Fields);

    }

    [Fact]
    public void WorksWithQuotedMultilineString()
    {
        var data1 = """
            "1";" 2  "in
            side"  x ";3
            """;

        var csvLayout = new CsvLayout('\"', ';');

        var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        Assert.Equal(new[] { "1", """
             2  "in
            side"  x 
            """, "3" }, result[0].Fields);

    }

    [Fact]
    public void WhenSkipEmptyLinesIsFalse_ReturnsEmptyLines()
    {
        var input = "1\n\n2";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(),
                                       new CsvBehaviour(EmptyLineAction: EmptyLineAction.None));

        var result = splitter.Lines().ToArray();

        Assert.True(result[1].IsEmpty);
    }

    [Fact]
    public void WhenSkipEmptyLinesIsTrue_SkipsEmptyLines()
    {
        var input = "\r\n1\n\n2";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(),
                                       new CsvBehaviour(EmptyLineAction: EmptyLineAction.Skip));

        var result = splitter.Lines().ToArray();

        Assert.Equal("1", result[0].Fields[0]);
        Assert.Equal("2", result[1].Fields[0]);
    }

    [Fact]
    public void WhenSkipEmptyLinesIsFalse_AndEmptyLineIsAtTheEnd_ReturnsEmptyLine()
    {
        var input = "a,b\n   ";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(EmptyLineAction: EmptyLineAction.None));

        var result = splitter.Lines().ToArray();

        Assert.Equal(2, result.Length);
        Assert.Equal(new[] { "a", "b" }, result[0].Fields);
        Assert.True(result[1].IsEmpty);
        Assert.Equal(new[] { string.Empty, string.Empty }, result[1].Fields);
    }

    [Fact]
    public void WhenInputContainsMultipleLinesWithTrailingEmptyField_ReturnsLinesWithEmptyField()
    {
        var input = "00,01,   \n10,11,   ";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour());
        var result = splitter.Lines().ToArray();
        Assert.Equal(2, result.Length);
        Assert.Equal(new[] { "00", "01", "" }, result[0].Fields);
        Assert.Equal(new[] { "10", "11", "" }, result[1].Fields);
    }

    [Fact]
    public void Testing()
    {
        var input = "00,   ,02\n,,";
        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(TrimmingOptions: ValueTrimmingOptions.None));
        var result = splitter.Lines().ToArray();
        Assert.Equal(2, result.Length);
        Assert.Equal(new[] { "00", "   ", "02" }, result[0].Fields);
    }

    [Fact]
    public void WhenTrailingLineContainsMissingFields_MissingFieldActionIsReplaceByNull_LastLineIsAppendedWithNulls()
    {
        var input = "a,b,c,d,e"
                    + "\na,b,c,d,"
                    + "\na,b,";

        var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(MissingFieldAction: MissingFieldAction.ReplaceByNull));
        var result = splitter.Lines().ToArray();
        Assert.Equal(3, result.Length);

        Assert.Equal(new[] { "a", "b", "c", "d", "e" }, result[0].Fields);
        Assert.Equal(new[] { "a", "b", "c", "d", "" }, result[1].Fields);
        Assert.Equal(new[] { "a", "b", "", null, null }, result[2].Fields);
    }

    [Fact]
    public void SampleDataSplitTest()
    {
        var data = CsvReaderSampleData.SampleData1;

        var splitter = new CsvStateMachine(new StringReader(data), new CsvLayout(), new CsvBehaviour());

        var result = splitter.Lines().ToArray();

        CsvReaderSampleData.CheckSampleData1(false, 0, result[0].Fields);

    }

    [Fact]
    public void QuotedFieldCanContainNewLineCharacters()
    {
        const string data = "\"\n\r\n\n\r\r\",,\t,\n";
        var result = Split(data);
        Assert.Equal(new[] { "\n\r\n\n\r\r", "", "", "" }, result);
    }

    [Fact]
    public void CanSplitByTabs()
    {
        const string data = "1\t2\t3";
        var result = Split(data, new CsvLayout(Delimiter: '\t'));
        Assert.Equal(new[] { "1", "2", "3" }, result);
    }

}

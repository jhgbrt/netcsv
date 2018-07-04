﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Net.Code.Csv.Impl;
using Net.Code.Csv.Tests.Unit.IO.Csv;
using NUnit.Framework;

namespace Net.Code.Csv.Tests.Unit.Csv
{
    [TestFixture]
    public class CsvStateMachineTests
    {
        private static IEnumerable<string> Split(string line)
        {
            return Split(line, new CsvLayout(), new CsvBehaviour());
        }
        private static IEnumerable<string> Split(string line, CsvLayout splitLineParams)
        {
            return Split(line, splitLineParams, new CsvBehaviour());
        }

        private static IEnumerable<string> Split(string line, CsvLayout splitLineParams, CsvBehaviour behaviour)
        {
            var splitter = new CsvStateMachine(new StringReader(line), splitLineParams, behaviour);
            var result = splitter.Lines();
            return result.First().Fields;
        }

        [Test]
        public void SplitsSimpleDelimitedLine()
        {
            var splitLineParams = new CsvLayout(quote: '"', delimiter: ';');
            var result = Split("1;2;3", splitLineParams);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result);
        }

        [Test]
        public void TrimsTrailingWhitespaceOfUnquotedField()
        {
            var splitLineParams = new CsvLayout('"', ';');
            var result = Split("1;2;3 \t", splitLineParams);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result);
        }

        [Test]
        public void DoesNotTrimTrailingWhitespaceOfQuotedField()
        {
            var splitLineParams = new CsvLayout('"', ';');
            var result = Split("1;2;\"3 \t\"", splitLineParams);
            CollectionAssert.AreEqual(new[] { "1", "2", "3 \t" }, result);
        }

        [Test]
        public void ValueTrimmingOptions_All_TrimsWhiteSpace()
        {
            var input = "\" x \"";
            var behaviour = new CsvBehaviour(ValueTrimmingOptions.All);
            var result = Split(input, new CsvLayout(), behaviour);
            Assert.AreEqual("x", result.Single());
        }

        [Test]
        public void ValueTrimmingOptions_QuotedOnly_TrimsWhiteSpaceWhenQuoted()
        {
            var input = "\" x \"";
            var behaviour = new CsvBehaviour(ValueTrimmingOptions.QuotedOnly);
            var result = Split(input, new CsvLayout(), behaviour);
            Assert.AreEqual("x", result.Single());
        }
        [Test]
        public void ValueTrimmingOptions_QuotedOnly_DoesNotTrimWhiteSpaceWhenNotQuoted()
        {
            var input = " x ";
            var behaviour = new CsvBehaviour(ValueTrimmingOptions.QuotedOnly);
            var result = Split(input, new CsvLayout(), behaviour);
            Assert.AreEqual(" x ", result.Single());
        }

        [Test]
        public void ValueTrimmingOptions_UnQuotedOnly_TrimsWhiteSpaceWhenNotQuoted()
        {
            var input = " x ";
            var behaviour = new CsvBehaviour(ValueTrimmingOptions.UnquotedOnly);
            var result = Split(input, new CsvLayout(), behaviour);
            Assert.AreEqual("x", result.Single());
        }
        [Test]
        public void ValueTrimmingOptions_UnQuotedOnly_DoesNotTrimWhiteSpaceWhenQuoted()
        {
            var input = "\" x \"";
            var behaviour = new CsvBehaviour(ValueTrimmingOptions.UnquotedOnly);
            var result = Split(input, new CsvLayout(), behaviour);
            Assert.AreEqual(" x ", result.Single());
        }

        [Test]
        public void StripsQuotes()
        {
            const string line = @"""FieldContent""";
            var splitLineParams = new CsvLayout('"', ',');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "FieldContent" }, result);
        }

        [Test]
        public void TrimsLeadingWhitespaceFromUnquotedField()
        {
            const string line = @"x, y,z";
            var splitLineParams = new CsvLayout('"', ',');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "x", "y", "z" }, result);
        }

        [Test]
        public void TrimsTrailingWhitespaceFromUnquotedField()
        {
            const string line = @"x,y   ,z";
            var splitLineParams = new CsvLayout('"', ',');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "x", "y", "z" }, result);
        }

        [Test]
        public void SupportsFieldsWithEscapedQuotes()
        {
            const string line = "x \"y\",z";
            var splitLineParams = new CsvLayout('"', ',');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "x \"y\"", "z" }, result);
        }

        [Test]
        public void EmptyFields()
        {
            const string line = @",x,,y";
            var splitLineParams = new CsvLayout('"', ',');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "", "x", "", "y" }, result);
        }

        [Test]
        public void EmptyString()
        {
            const string line = "";
            var splitter = new CsvStateMachine(new StringReader(line), new CsvLayout(), new CsvBehaviour());
            var result = splitter.Lines();
            CollectionAssert.IsEmpty(result);
        }
        [Test]
        public void EmptyFields_Comma()
        {
            const string line = ",,";
            var result = Split(line, new CsvLayout());
            CollectionAssert.AreEqual(new[] { "", "", "" }, result);
        }
        [Test]
        public void EmptyFields_Tab()
        {
            const string line = "\t\t";
            var result = Split(line, new CsvLayout(delimiter: '\t'));
            CollectionAssert.AreEqual(new[] { "", "", "" }, result);
        }
        [Test]
        public void QuotedStringWithDelimiter()
        {
            // "x ""y"", z"
            const string line = "\"x \"y\" z, u\",v";
            var splitLineParams = new CsvLayout();
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "x \"y\" z, u", "v" }, result);

        }

        [Test]
        public void WhenValueTrimmingIsNone_LastFieldHasLeadingAndTrailingWhitespace_WhitespaceIsNotTrimmed()
        {
            const string line = "x,y, z ";
            var splitLineParams = new CsvLayout('"', ',', '"');
            var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
            CollectionAssert.AreEqual(new[] { @"x", "y", " z " }, result);

        }

        [Test]
        public void CarriageReturnCanBeUsedAsDelimiter()
        {
            const string line = "1\r2\n";
            var splitLineParams = new CsvLayout('"', '\r');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "1", "2" }, result);
        }

        [Test]
        public void EscapeCharacterInsideQuotedStringIsEscaped()
        {
            const string line = @"""\\""";
            var splitLineParams = new CsvLayout('"', ',', '\\');
            var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
            Assert.AreEqual(@"\", result.Single());
        }

        [Test]
        public void LineWithOnlySeparatorIsSplitIntoTwoEmptyStrings()
        {
            const string line = ",";
            var splitLineParams = new CsvLayout('"', ',', '\\');
            var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
            CollectionAssert.AreEqual(new[] { "", "" }, result);
        }

        [Test]
        public void CanWorkWithMultilineField()
        {
            const string data = @"a,b,""line1
line2""";
            var splitLineParams = new CsvLayout('"', ',', '\\');
            var result = Split(data, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
            CollectionAssert.AreEqual(new[] { "a", "b", @"line1
line2" }, result);
        }

        [Test]
        public void MultipleLinesAreSplitCorrectly()
        {
            var data1 = @"1;2;3
4;5;6";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Lines().ToArray();

            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result[0].Fields);
            CollectionAssert.AreEqual(new[] { "4", "5", "6" }, result[1].Fields);

        }

        [Test]
        public void WorksWithQuotedStringInsideQuotedFieldButOnlyWhitespaceAfterSecondQuote()
        {
            var data1 = @"""1"";"" 2  ""inside""   "";3";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Lines().ToArray();

            CollectionAssert.AreEqual(new[] { "1", @" 2  ""inside""   ", "3" }, result[0].Fields);

        }

        [Test]
        public void WorksWithQuotedStringInsideQuotedField()
        {
            var data1 = @"""1"";"" 2  ""inside""  x "";3";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Lines().ToArray();

            CollectionAssert.AreEqual(new[] { "1", @" 2  ""inside""  x ", "3" }, result[0].Fields);

        }

        [Test]
        public void WorksWithQuotedMultilineString()
        {
            var data1 = @"""1"";"" 2  ""in
side""  x "";3";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new CsvStateMachine(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Lines().ToArray();

            CollectionAssert.AreEqual(new[] { "1", @" 2  ""in
side""  x ", "3" }, result[0].Fields);

        }

        [Test]
        public void WhenSkipEmptyLinesIsFalse_ReturnsEmptyLines()
        {
            var input = "1\n\n2";
            var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(),
                                           new CsvBehaviour(skipEmptyLines: false));

            var result = splitter.Lines().ToArray();

            Assert.IsTrue(result[1].IsEmpty);
        }

        [Test]
        public void WhenSkipEmptyLinesIsTrue_SkipsEmptyLines()
        {
            var input = "\r\n1\n\n2";
            var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(),
                                           new CsvBehaviour(skipEmptyLines: true));

            var result = splitter.Lines().ToArray();

            Assert.AreEqual("1", result[0].Fields[0]);
            Assert.AreEqual("2", result[1].Fields[0]);
        }

        [Test]
        public void WhenSkipEmptyLinesIsFalse_AndEmptyLineIsAtTheEnd_ReturnsEmptyLine()
        {
            var input = "a,b\n   ";
            var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(skipEmptyLines: false));

            var result = splitter.Lines().ToArray();

            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new[] { "a", "b" }, result[0].Fields);
            Assert.IsTrue(result[1].IsEmpty);
            CollectionAssert.AreEqual(new[] { "", string.Empty }, result[1].Fields);
        }

        [Test]
        public void WhenInputContainsMultipleLinesWithTrailingEmptyField_ReturnsLinesWithEmptyField()
        {
            var input = "00,01,   \n10,11,   ";
            var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour());
            var result = splitter.Lines().ToArray();
            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new[] { "00", "01", "" }, result[0].Fields);
            CollectionAssert.AreEqual(new[] { "10", "11", "" }, result[1].Fields);
        }

        [Test]
        public void Testing()
        {
            var input = "00,   ,02\n,,";
            var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(trimmingOptions: ValueTrimmingOptions.None));
            var result = splitter.Lines().ToArray();
            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new[] { "00", "   ", "02" }, result[0].Fields);
        }

        [Test]
        public void WhenTrailingLineContainsMissingFields_MissingFieldActionIsReplaceByNull_LastLineIsAppendedWithNulls()
        {
            var input = "a,b,c,d,e"
                        + "\na,b,c,d,"
                        + "\na,b,";

            var splitter = new CsvStateMachine(new StringReader(input), new CsvLayout(), new CsvBehaviour(missingFieldAction: MissingFieldAction.ReplaceByNull));
            var result = splitter.Lines().ToArray();
            Assert.AreEqual(3, result.Count());

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "e" }, result[0].Fields);
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "" }, result[1].Fields);
            CollectionAssert.AreEqual(new[] { "a", "b", "", null, null }, result[2].Fields);
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
            CollectionAssert.AreEqual(new[] { "\n\r\n\n\r\r", "", "", "" }, result);
        }

        [Test]
        public void CanSplitByTabs()
        {
            const string data = "1\t2\t3";
            var result = Split(data, new CsvLayout(delimiter: '\t'));
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result);
        }

    }
}

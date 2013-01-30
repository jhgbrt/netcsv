using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Net.Code.Csv.Tests.Unit.IO.Csv
{
	[TestFixture]
	public class BufferSplitTests
	{
        private static IEnumerable<string> Split(string line, CsvLayout splitLineParams)
        {
            return Split(line, splitLineParams, new CsvBehaviour());
        }

        private static IEnumerable<string> Split(string line, CsvLayout splitLineParams, CsvBehaviour behaviour)
        {
            var splitter = new BufferedCsvLineGenerator(new StringReader(line), splitLineParams, behaviour);
            var result = splitter.Split();
            return result.First().Fields;
        }
        
		[Test]
		public void SplitsSimpleDelimitedLine()
		{
            var splitLineParams = new CsvLayout('"', ';');
			var result = Split("1;2;3", splitLineParams);
			CollectionAssert.AreEqual(new[]{"1", "2", "3"}, result);
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
		public void StripsQuotes()
		{
			const string line = @"""FieldContent""";
			var splitLineParams = new CsvLayout('"', ',');
			var result = Split(line, splitLineParams);
			CollectionAssert.AreEqual(new[]{"FieldContent"}, result);
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
		public void QuotedStringWithDelimiter()
		{
            // "x ""y"", z"
			const string line = "\"x \"y\" z, u\",v";
			var splitLineParams = new CsvLayout('"', ',');
            var result = Split(line, splitLineParams);
            CollectionAssert.AreEqual(new[] { "x \"y\" z, u", "v" }, result);

		}

		[Test]
		public void WhenValueTrimmingIsNone_LastFieldWithLeadingAndTrailingWhitespace_WhitespaceIsNotTrimmed()
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
			const string line = "\"\\\\\"";
			var splitLineParams = new CsvLayout('"', ',', '\\');
            var result = Split(line, splitLineParams, new CsvBehaviour(ValueTrimmingOptions.None));
            Assert.AreEqual("\\", result.Single());
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

            var splitter = new BufferedCsvLineGenerator(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Split().ToArray();

            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result[0].Fields);
            CollectionAssert.AreEqual(new[] { "4", "5", "6" }, result[1].Fields);

        }

        [Test]
        public void WorksWithQuotedStringInsideQuotedFieldButOnlyWhitespaceAfterSecondQuote()
        {
            var data1 = @"""1"";"" 2  ""inside""   "";3";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new BufferedCsvLineGenerator(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Split().ToArray();

            CollectionAssert.AreEqual(new[] { "1", @" 2  ""inside""   ", "3" }, result[0].Fields);

        }

        [Test]
        public void WorksWithQuotedStringInsideQuotedField()
        {
            var data1 = @"""1"";"" 2  ""inside""  x "";3";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new BufferedCsvLineGenerator(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Split().ToArray();

            CollectionAssert.AreEqual(new[] { "1", @" 2  ""inside""  x ", "3" }, result[0].Fields);

        }

        [Test]
        public void WorksWithQuotedMultilineString()
        {
            var data1 = @"""1"";"" 2  ""in
side""  x "";3";

            var csvLayout = new CsvLayout('\"', ';');

            var splitter = new BufferedCsvLineGenerator(new StringReader(data1), csvLayout, new CsvBehaviour());

            var result = splitter.Split().ToArray();

            CollectionAssert.AreEqual(new[] { "1", @" 2  ""in
side""  x ", "3" }, result[0].Fields);

        }

        [Test]
        public void WhenSkipEmptyLinesIsFalse_ReturnsEmptyLines()
        {
            var input = "1\n\n2";
            var splitter = new BufferedCsvLineGenerator(new StringReader(input), new CsvLayout(),
                                           new CsvBehaviour(skipEmptyLines: false));

            var result = splitter.Split().ToArray();

            Assert.IsTrue(result[1].IsEmpty);
        }

        [Test]
        public void WhenSkipEmptyLinesIsTrue_SkipsEmptyLines()
        {
            var input = "\r\n1\n\n2";
            var splitter = new BufferedCsvLineGenerator(new StringReader(input), new CsvLayout(),
                                           new CsvBehaviour(skipEmptyLines: true));

            var result = splitter.Split().ToArray();

            Assert.AreEqual("1", result[0].Fields[0]);
            Assert.AreEqual("2", result[1].Fields[0]);
        }

        [Test]
        public void WhenSkipEmptyLinesIsFalse_AndEmptyLineIsAtTheEnd_ReturnsEmptyLine()
        {
            var input = "a,b\n   ";
            var splitter = new BufferedCsvLineGenerator(new StringReader(input), new CsvLayout(), new CsvBehaviour(skipEmptyLines:false));
            
            var result = splitter.Split().ToArray();

            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new[] { "a", "b" }, result[0].Fields);
            Assert.IsTrue(result[1].IsEmpty);
            CollectionAssert.AreEqual(new[] { "", string.Empty }, result[1].Fields);
        }

        [Test]
        public void WhenInputContainsMultipleLinesWithTrailingEmptyField_ReturnsLinesWithEmptyField()
        {
            var input = "00,01,   \n10,11,   ";
            var splitter = new BufferedCsvLineGenerator(new StringReader(input), new CsvLayout(), new CsvBehaviour());
            var result = splitter.Split().ToArray();
            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new[] { "00", "01", "" }, result[0].Fields);
            CollectionAssert.AreEqual(new[] { "10", "11", "" }, result[1].Fields);
        }

        [Test]
        public void WNhenTrailingLineContainsMissingFields_MissingFieldActionIsReplaceByNull_LastLineIsAppendedWithNulls()
        {
            var input = "a,b,c,d,e"
                        + "\na,b,c,d,"
                        + "\na,b,";

            var splitter = new BufferedCsvLineGenerator(new StringReader(input), new CsvLayout(), new CsvBehaviour(missingFieldAction: MissingFieldAction.ReplaceByNull));
            var result = splitter.Split().ToArray();
            Assert.AreEqual(3, result.Count());

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "e"  }, result[0].Fields);
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "" }, result[1].Fields);
            CollectionAssert.AreEqual(new[] { "a", "b", "", null, null }, result[2].Fields);
        }

        [Test]
        public void SampleDataSplitTest()
        {
            var data = CsvReaderSampleData.SampleData1;

            var splitter = new BufferedCsvLineGenerator(new StringReader(data), new CsvLayout(), new CsvBehaviour());

            var result = splitter.Split().ToArray();

            CsvReaderSampleData.CheckSampleData1(false, 0, result[0].Fields);

        }



	}
}

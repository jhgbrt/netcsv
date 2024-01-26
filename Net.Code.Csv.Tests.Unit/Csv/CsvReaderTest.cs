//	LumenWorks.Framework.Tests.Unit.IO.CSV.CsvReaderTest
//	Copyright (c) 2005 Sébastien Lorion
//
//	MIT license (http://en.wikipedia.org/wiki/MIT_License)
//
//	Permission is hereby granted, free of charge, to any person obtaining a copy
//	of this software and associated documentation files (the "Software"), to deal
//	in the Software without restriction, including without limitation the rights 
//	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//	of the Software, and to permit persons to whom the Software is furnished to do so, 
//	subject to the following conditions:
//
//	The above copyright notice and this permission notice shall be included in all 
//	copies or substantial portions of the Software.
//
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//	PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
//	FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//	ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


// A special thanks goes to "shriop" at CodeProject for providing many of the standard and Unicode parsing tests.


using System.IO;

namespace Net.Code.Csv.Tests.Unit.IO.Csv;


public class CsvReaderTest
{
    #region Argument validation tests

    #region Constructors

    [Fact]
    public void ReadCsv_FromString_WhenStringIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (ReadCsv.FromString(null))
            {
            }
        });
    }


    [Fact]
    public void ReadCsv_FromStream_WhenStreamIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (ReadCsv.FromStream(null))
            {
            }
        });
    }

    [Fact]
    public void Constructor_StreamIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (ReadCsv.FromFile(null))
            {
            }
        });
    }

    #endregion

    #region Indexers

    [Fact]
    public void Indexer_Negative_Throws()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            var s = csv[-1];
        });
    }

    [Fact]
    public void Indexer_BeyondRecordSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            var s = csv[CsvReaderSampleData.SampleData1RecordCount];
        });
    }

    [Fact]
    public void ConstructedWithoutHeaders_IndexerByInvalidHeader_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            var s = csv["asdf"];
        });
    }

    [Fact]
    public void ConstructedWithoutHeaders_IndexerByValidHeader_DataHasHeaders_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false);
            csv.Read();
            var s = csv[CsvReaderSampleData.SampleData1Header0];
        });
    }

    [Fact]
    public void ConstructedWithoutHeaders_Indexer_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false);
            csv.Read();
            var s = csv[null];
        });
    }

    [Fact]
    public void ConstructedWithoutHeaders_IndexByEmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false);
            csv.Read();
            var s = csv[string.Empty];
        });
    }

    [Fact]
    public void ConstructedWithHeaders_IndexByNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
            csv.Read();
            var s = csv[null];
        });
    }

    [Fact]
    public void ConstructedWithHeaders_IndexByEmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
            csv.Read();
            var s = csv[string.Empty];
        });
    }

    [Fact]
    public void ConstructedWithHeaders_IndexByNonExistingHeader_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
            csv.Read();
            var s = csv["asdf"];
        });
    }

    #endregion

    #region CopyCurrentRecordTo

    [Fact]
    public void GetValues_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            csv.GetValues(null);
        });
    }

    [Fact]
    public void GetValues_ArrayTooSmall_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            csv.GetValues(new string[CsvReaderSampleData.SampleData1RecordCount - 1]);
        });
    }


    #endregion

    #endregion

    #region Parsing tests

    [Fact]
    public void ParsingTest1()
    {
        const string data = "1\r\n\r\n1";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);

        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest2()
    {
        // ["Bob said, ""Hey!""",2, 3 ]
        const string data = "\"Bob said, \"\"Hey!\"\"\",2, 3 ";

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.True(csv.Read());
            Assert.Equal(@"Bob said, ""Hey!""", csv[0]);
            Assert.Equal("2", csv[1]);
            Assert.Equal("3", csv[2]);

            Assert.False(csv.Read());
        }

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.None))
        {
            Assert.True(csv.Read());
            Assert.Equal(@"Bob said, ""Hey!""", csv[0]);
            Assert.Equal("2", csv[1]);
            Assert.Equal(" 3 ", csv[2]);

            Assert.False(csv.Read());
        }
    }

    [Fact]
    public void ParsingTest3()
    {
        const string data = "1\r\n2\n";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);

        Assert.True(csv.Read());
        Assert.Equal("2", csv[0]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest4()
    {
        const string data = "\"\n\r\n\n\r\r\",,\t,\n";

        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.True(csv.Read());

        Assert.Equal(4, csv.FieldCount);

        Assert.Equal("\n\r\n\n\r\r", csv[0]);
        Assert.Equal("", csv[1]);
        Assert.Equal("", csv[2]);
        Assert.Equal("", csv[3]);

        Assert.False(csv.Read());
    }


    [Fact]
    public void ParsingTest6()
    {
        const string data = "1,2";
        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal("2", csv[1]);
        Assert.Equal(2, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest7()
    {
        const string data = "\r\n1\r\n";
        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal(1, csv.FieldCount);
        Assert.Equal("1", csv[0]);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest8()
    {
        const string data = "\"bob said, \"\"Hey!\"\"\",2, 3 ";

        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.True(csv.Read());
        Assert.Equal("bob said, \"Hey!\"", csv[0]);
        Assert.Equal("2", csv[1]);
        Assert.Equal("3", csv[2]);
        Assert.Equal(3, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest9()
    {
        const string data = ",";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal(String.Empty, csv[0]);
        Assert.Equal(String.Empty, csv[1]);
        Assert.Equal(2, csv.FieldCount);
    }

    [Fact]
    public void ParsingTest10()
    {
        const string data = "1\r\n2";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("2", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest11()
    {
        const string data = "1\n2";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("2", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest12()
    {
        const string data = "1\r\n2";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("2", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest13()
    {
        const string data = "1\r";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest14()
    {
        const string data = "1\n";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest15()
    {
        const string data = "1\r\n";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest17()
    {
        const string data = "\"July 4th, 2005\"";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("July 4th, 2005", csv[0]);
    }

    [Fact]
    public void ParsingTest18()
    {
        const string data = " 1";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal(" 1", csv[0]);
    }

    [Fact]
    public void ParsingTest19()
    {
        string data = String.Empty;

        using var csv = ReadCsv.FromString(data);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest20()
    {
        const string data = "user_id,name\r\n1,Bruce";

        using var csv = ReadCsv.FromString(data, hasHeaders: true);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal("Bruce", csv[1]);
        Assert.Equal(2, csv.FieldCount);
        Assert.Equal("1", csv["user_id"]);
        Assert.Equal("Bruce", csv["name"]);
        Assert.False(csv.Read());
    }

    [Fact]
    public void SupportsMultilineQuotedFields()
    {
        const string data = "\"data \r\n here\"";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("data \r\n here", csv[0]);
    }

    [Fact]
    public void ParsingTest22()
    {
        const string data = ",,\n1,";

        using var csv = ReadCsv.FromString(data, delimiter: ',', emptyLineAction: EmptyLineAction.None, missingFieldAction: MissingFieldAction.ReplaceByNull);
        Assert.True(csv.Read());
        Assert.Equal(3, csv.FieldCount);

        Assert.Equal(String.Empty, csv[0]);
        Assert.Equal(String.Empty, csv[1]);
        Assert.Equal(String.Empty, csv[2]);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal("1", csv[0]);
        Assert.Equal(String.Empty, csv[1]);
        Assert.Null(csv[2]);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest23()
    {
        const string data = "\"double\"\"\"\"double quotes\"";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("double\"\"double quotes", csv[0]);
    }

    [Fact]
    public void ParsingTest24()
    {
        const string data = "1\r";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
    }

    [Fact]
    public void ParsingTest25()
    {
        const string data = "1\r\n";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
    }

    [Fact]
    public void ParsingTest26()
    {
        const string data = "1\n";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
    }

    [Fact]
    public void ParsingTest27()
    {
        const string data = "'bob said, ''Hey!''',2, 3 ";

        using var csv = ReadCsv.FromString(data, quote: '\'', escape: '\'', trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.True(csv.Read());
        Assert.Equal("bob said, 'Hey!'", csv[0]);
        Assert.Equal("2", csv[1]);
        Assert.Equal("3", csv[2]);
    }

    [Fact]
    public void ParsingTest28()
    {
        const string data = "\"data \"\" here\"";

        using var csv = ReadCsv.FromString(data, quote: '\0', escape: '\\');
        Assert.True(csv.Read());
        Assert.Equal("\"data \"\" here\"", csv[0]);
    }

    [Fact]
    public void ParsingTest29()
    {
        string data = new String('a', 75) + "," + new String('b', 75);

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal(new String('a', 75), csv[0]);
        Assert.Equal(new String('b', 75), csv[1]);
        Assert.Equal(2, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest30()
    {
        const string data = "1\r\n\r\n1";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest31()
    {
        const string data = "1\r\n# bunch of crazy stuff here\r\n1";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal(1, csv.FieldCount);
    }

    [Fact]
    public void ParsingTest32()
    {
        const string data = "\"1\",Bruce\r\n\"2\n\",Toni\r\n\"3\",Brian\r\n";

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal("1", csv[0]);
        Assert.Equal("Bruce", csv[1]);
        Assert.Equal(2, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("2\n", csv[0]);
        Assert.Equal("Toni", csv[1]);
        Assert.Equal(2, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("3", csv[0]);
        Assert.Equal("Brian", csv[1]);
        Assert.Equal(2, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest33()
    {
        const string data = "\"double\\\\\\\\double backslash\"";

        using var csv = ReadCsv.FromString(data, escape: '\\');
        Assert.True(csv.Read());
        Assert.Equal("double\\\\double backslash", csv[0]);
        Assert.Equal(1, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest34()
    {
        const string data = "\"Chicane\", \"Love on the Run\", \"Knight Rider\", \"This field contains a comma, but it doesn't matter as the field is quoted\"\r\n" +
                  "\"Samuel Barber\", \"Adagio for Strings\", \"Classical\", \"This field contains a double quote character, \"\", but it doesn't matter as it is escaped\"";

        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.True(csv.Read());
        Assert.Equal("Chicane", csv[0]);
        Assert.Equal("Love on the Run", csv[1]);
        Assert.Equal("Knight Rider", csv[2]);
        Assert.Equal("This field contains a comma, but it doesn't matter as the field is quoted", csv[3]);
        Assert.Equal(4, csv.FieldCount);
        Assert.True(csv.Read());
        Assert.Equal("Samuel Barber", csv[0]);
        Assert.Equal("Adagio for Strings", csv[1]);
        Assert.Equal("Classical", csv[2]);
        Assert.Equal("This field contains a double quote character, \", but it doesn't matter as it is escaped", csv[3]);
        Assert.Equal(4, csv.FieldCount);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest35()
    {
        var data = "\t";
        using var csv = ReadCsv.FromString(data, delimiter: '\t');
        Assert.Equal(2, csv.FieldCount);

        Assert.True(csv.Read());

        Assert.Equal(string.Empty, csv[0]);
        Assert.Equal(string.Empty, csv[1]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest36()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
    }

    [Fact]
    public void ParsingTest37()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1,
            hasHeaders: true,
            trimmingOptions: ValueTrimmingOptions.UnquotedOnly,
            schema: CsvReaderSampleData.SampleData1Schema);
        CsvReaderSampleData.CheckSampleData1(csv, true, false);
    }

    [Fact]
    public void ParsingTest38()
    {
        using var reader = ReadCsv.FromString("abc,def,ghi\n");
        int fieldCount = reader.FieldCount;

        Assert.True(reader.Read());
        Assert.Equal("abc", reader[0]);
        Assert.Equal("def", reader[1]);
        Assert.Equal("ghi", reader[2]);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ParsingTest39()
    {
        using var csv = ReadCsv.FromString("00,01,   \n10,11,   ", trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.True(csv.Read());
        Assert.Equal("00", csv[0]);
        Assert.Equal("01", csv[1]);
        Assert.Equal("", csv[2]);

        Assert.True(csv.Read());
        Assert.Equal("10", csv[0]);
        Assert.Equal("11", csv[1]);
        Assert.Equal("", csv[2]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest40()
    {
        const string data = "\"00\",\n\"10\",";
        using var csv = ReadCsv.FromString(data);
        Assert.Equal(2, csv.FieldCount);

        Assert.True(csv.Read());
        Assert.Equal("00", csv[0]);
        Assert.Equal(string.Empty, csv[1]);

        Assert.True(csv.Read());
        Assert.Equal("10", csv[0]);
        Assert.Equal(string.Empty, csv[1]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void UnquotedValuesShouldBeTrimmed()
    {
        const string data = "First record          ,Second record";
        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.Equal(2, csv.FieldCount);

        Assert.True(csv.Read());
        Assert.Equal("First record", csv[0]);
        Assert.Equal("Second record", csv[1]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void SpaceBecomesEmptyField()
    {
        var data = " ";
        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly, emptyLineAction: EmptyLineAction.None);
        Assert.True(csv.Read());
        Assert.Equal(1, csv.FieldCount);
        Assert.Equal(string.Empty, csv[0]);
        Assert.False(csv.Read());
    }

    [Fact]
    public void ParsingTest43()
    {
        var data = "a,b\n   ";
        using var csv = ReadCsv.FromString(data,
            missingFieldAction: MissingFieldAction.ReplaceByNull,
            trimmingOptions: ValueTrimmingOptions.All,
            emptyLineAction: EmptyLineAction.None);
        Assert.True(csv.Read());
        Assert.Equal(2, csv.FieldCount);
        Assert.Equal("a", csv[0]);
        Assert.Equal("b", csv[1]);

        csv.Read();
        Assert.Equal(string.Empty, csv[0]);
        Assert.Null(csv[1]);
    }

    #endregion

    #region UnicodeParsing tests

    [Fact]
    public void UnicodeParsingTest1()
    {
        // control characters and comma are skipped

        char[] raw = new char[65536 - 13];

        for (int i = 0; i < raw.Length; i++)
            raw[i] = (char)(i + 14);

        raw[44 - 14] = ' '; // skip comma

        var data = new string(raw);

        using var csv = ReadCsv.FromString(data);
        Assert.True(csv.Read());
        Assert.Equal(data, csv[0]);
        Assert.False(csv.Read());
    }

    static byte[] ToByteArray(string s)
    {
        using MemoryStream stream = new();
        using (TextWriter writer = new StreamWriter(stream, Encoding.Unicode))
        {
            writer.WriteLine(s);
        }

        return stream.ToArray();
    }

    [Fact]
    public void UnicodeParsingTest2()
    {
        var test = "München";
        var buffer = ToByteArray(test);
        using var csv = ReadCsv.FromStream(new MemoryStream(buffer), encoding: Encoding.Unicode);
        Assert.True(csv.Read());
        Assert.Equal(test, csv[0]);
        Assert.False(csv.Read());
    }

    [Fact]
    public void UnicodeParsingTest3()
    {
        var test = "München";
        var buffer = ToByteArray(test);
        using var csv = ReadCsv.FromStream(new MemoryStream(buffer), encoding: Encoding.Unicode);
        Assert.True(csv.Read());
        Assert.Equal(test, csv[0]);
        Assert.False(csv.Read());
    }

    #endregion

    #region FieldCount

    [Fact]
    public void FieldCountTest1()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1,
            trimmingOptions: ValueTrimmingOptions.UnquotedOnly
            );
        CsvReaderSampleData.CheckSampleData1(csv, false, true);
    }

    #endregion

    #region GetFieldHeaders

    [Fact]
    public void GetFieldHeadersTest1()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
        string[] headers = csv.GetFieldHeaders();

        Assert.NotNull(headers);
        Assert.Equal(6, headers.Length);
    }

    [Fact]
    public void GetFieldHeadersTest2()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, schema: CsvReaderSampleData.SampleData1Schema);
        string[] headers = csv.GetFieldHeaders();

        Assert.NotNull(headers);
        Assert.Equal(CsvReaderSampleData.SampleData1RecordCount, headers.Length);

        Assert.Equal(CsvReaderSampleData.SampleData1Header0, headers[0]);
        Assert.Equal(CsvReaderSampleData.SampleData1Header1, headers[1]);
        Assert.Equal(CsvReaderSampleData.SampleData1Header2, headers[2]);
        Assert.Equal(CsvReaderSampleData.SampleData1Header3, headers[3]);
        Assert.Equal(CsvReaderSampleData.SampleData1Header4, headers[4]);
        Assert.Equal(CsvReaderSampleData.SampleData1Header5, headers[5]);

        Assert.Equal(0, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header0));
        Assert.Equal(1, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header1));
        Assert.Equal(2, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header2));
        Assert.Equal(3, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header3));
        Assert.Equal(4, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header4));
        Assert.Equal(5, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header5));
    }

    [Fact]
    public void OnlyComments()
    {
        var data = "#asdf\n\n#asdf,asdf";
        using var csv = ReadCsv.FromString(data, hasHeaders: true);
        string[] headers = csv.GetFieldHeaders();

        Assert.NotNull(headers);
        Assert.Empty(headers);
    }

    [Fact]
    public void GetFieldHeaders_WithEmptyHeaderNames()
    {
        var data = ",  ,,aaa,\"   \",,,";

        using var csv = ReadCsv.FromString(data, hasHeaders: true);
        Assert.False(csv.Read());
        Assert.Equal(8, csv.FieldCount);

        string[] headers = csv.GetFieldHeaders();
        Assert.Equal(csv.FieldCount, headers.Length);

        Assert.Equal("aaa", headers[3]);
        foreach (var index in new int[] { 0, 1, 2, 4, 5, 6, 7 })
            Assert.Equal("Column" + index.ToString(), headers[index]);
    }

    #endregion

    #region SkipEmptyLines

    [Fact]
    public void SkipEmptyLinesTest1()
    {
        var data = "00\n\n10";
        using var csv = ReadCsv.FromString(data, emptyLineAction: EmptyLineAction.None);
        Assert.Equal(1, csv.FieldCount);

        Assert.True(csv.Read());
        Assert.Equal("00", csv[0]);

        Assert.True(csv.Read());
        Assert.Equal(string.Empty, csv[0]);

        Assert.True(csv.Read());
        Assert.Equal("10", csv[0]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void SkipEmptyLinesTest2()
    {
        var data = "00\n\n10";
        using var csv = ReadCsv.FromString(data, emptyLineAction: EmptyLineAction.Skip);
        Assert.Equal(1, csv.FieldCount);

        Assert.True(csv.Read());
        Assert.Equal("00", csv[0]);

        Assert.True(csv.Read());
        Assert.Equal("10", csv[0]);

        Assert.False(csv.Read());
    }

    #endregion

    #region Trimming tests
    [Theory]
    [InlineData("", ValueTrimmingOptions.None, new string[] { })]
    [InlineData("", ValueTrimmingOptions.QuotedOnly, new string[] { })]
    [InlineData("", ValueTrimmingOptions.UnquotedOnly, new string[] { })]
    [InlineData(" aaa , bbb , ccc ", ValueTrimmingOptions.None, new string[] { " aaa ", " bbb ", " ccc " })]
    [InlineData(" aaa , bbb , ccc ", ValueTrimmingOptions.QuotedOnly, new string[] { " aaa ", " bbb ", " ccc " })]
    [InlineData(" aaa , bbb , ccc ", ValueTrimmingOptions.UnquotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [InlineData("\" aaa \",\" bbb \",\" ccc \"", ValueTrimmingOptions.None, new string[] { " aaa ", " bbb ", " ccc " })]
    [InlineData("\" aaa \",\" bbb \",\" ccc \"", ValueTrimmingOptions.QuotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [InlineData("\" aaa \",\" bbb \",\" ccc \" ", ValueTrimmingOptions.QuotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [InlineData("\" aaa \",\" bbb \" ,\" ccc \"", ValueTrimmingOptions.QuotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [InlineData("\" aaa \",\" bbb \",\" ccc \"", ValueTrimmingOptions.UnquotedOnly, new string[] { " aaa ", " bbb ", " ccc " })]
    [InlineData(" aaa , bbb ,\" ccc \"", ValueTrimmingOptions.None, new string[] { " aaa ", " bbb ", " ccc " })]
    [InlineData(" aaa , bbb ,\" ccc \"", ValueTrimmingOptions.QuotedOnly, new string[] { " aaa ", " bbb ", "ccc" })]
    [InlineData(" aaa , bbb ,\" ccc \"", ValueTrimmingOptions.UnquotedOnly, new string[] { "aaa", "bbb", " ccc " })]
    public void TrimFieldValuesTest(string data, ValueTrimmingOptions trimmingOptions, params string[] expected)
    {
        using var csv = ReadCsv.FromString(data, trimmingOptions: trimmingOptions);
        while (csv.Read())
        {
            var actual = new string[csv.FieldCount];
            csv.GetValues(actual);

            Assert.Equal(expected, actual);
        }
    }

    #endregion
}

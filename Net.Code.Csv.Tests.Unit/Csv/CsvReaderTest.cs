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

[TestFixture()]
public class CsvReaderTest
{
    #region Argument validation tests

    #region Constructors

    [Test]
    public void ReadCsv_FromString_WhenStringIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (ReadCsv.FromString(null))
            {
            }
        });
    }


    [Test]
    public void ReadCsv_FromStream_WhenStreamIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (ReadCsv.FromStream(null))
            {
            }
        });
    }

    [Test]
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

    [Test]
    public void Indexer_Negative_Throws()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            var s = csv[-1];
        });
    }

    [Test]
    public void Indexer_BeyondRecordSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            var s = csv[CsvReaderSampleData.SampleData1RecordCount];
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_IndexerByInvalidHeader_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            var s = csv["asdf"];
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_IndexerByValidHeader_DataHasHeaders_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false);
            csv.Read();
            var s = csv[CsvReaderSampleData.SampleData1Header0];
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_Indexer_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false);
            csv.Read();
            var s = csv[null];
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_IndexByEmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false);
            csv.Read();
            var s = csv[string.Empty];
        });
    }

    [Test]
    public void ConstructedWithHeaders_IndexByNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
            csv.Read();
            var s = csv[null];
        });
    }

    [Test]
    public void ConstructedWithHeaders_IndexByEmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
            csv.Read();
            var s = csv[string.Empty];
        });
    }

    [Test]
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

    [Test]
    public void GetValues_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
            csv.Read();
            csv.GetValues(null);
        });
    }

    [Test]
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

    [Test]
    public void ParsingTest1()
    {
        const string data = "1\r\n\r\n1";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest2()
    {
        // ["Bob said, ""Hey!""",2, 3 ]
        const string data = "\"Bob said, \"\"Hey!\"\"\",2, 3 ";

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.That(csv.Read(), Is.True);
            Assert.That(csv[0], Is.EqualTo(@"Bob said, ""Hey!"""));
            Assert.That(csv[1], Is.EqualTo("2"));
            Assert.That(csv[2], Is.EqualTo("3"));

            Assert.That(csv.Read(), Is.False);
        }

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.None))
        {
            Assert.That(csv.Read(), Is.True);
            Assert.That(csv[0], Is.EqualTo(@"Bob said, ""Hey!"""));
            Assert.That(csv[1], Is.EqualTo("2"));
            Assert.That(csv[2], Is.EqualTo(" 3 "));

            Assert.That(csv.Read(), Is.False);
        }
    }

    [Test]
    public void ParsingTest3()
    {
        const string data = "1\r\n2\n";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("2"));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest4()
    {
        const string data = "\"\n\r\n\n\r\r\",,\t,\n";

        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.That(csv.Read(), Is.True);

        Assert.That(csv.FieldCount, Is.EqualTo(4));

        Assert.That(csv[0], Is.EqualTo("\n\r\n\n\r\r"));
        Assert.That(csv[1], Is.EqualTo(""));
        Assert.That(csv[2], Is.EqualTo(""));
        Assert.That(csv[3], Is.EqualTo(""));

        Assert.That(csv.Read(), Is.False);
    }


    [Test]
    public void ParsingTest6()
    {
        const string data = "1,2";
        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv[1], Is.EqualTo("2"));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest7()
    {
        const string data = "\r\n1\r\n";
        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest8()
    {
        const string data = "\"bob said, \"\"Hey!\"\"\",2, 3 ";

        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("bob said, \"Hey!\""));
        Assert.That(csv[1], Is.EqualTo("2"));
        Assert.That(csv[2], Is.EqualTo("3"));
        Assert.That(csv.FieldCount, Is.EqualTo(3));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest9()
    {
        const string data = ",";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(String.Empty));
        Assert.That(csv[1], Is.EqualTo(String.Empty));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
    }

    [Test]
    public void ParsingTest10()
    {
        const string data = "1\r\n2";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("2"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest11()
    {
        const string data = "1\n2";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("2"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest12()
    {
        const string data = "1\r\n2";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("2"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest13()
    {
        const string data = "1\r";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest14()
    {
        const string data = "1\n";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest15()
    {
        const string data = "1\r\n";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest17()
    {
        const string data = "\"July 4th, 2005\"";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("July 4th, 2005"));
    }

    [Test]
    public void ParsingTest18()
    {
        const string data = " 1";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(" 1"));
    }

    [Test]
    public void ParsingTest19()
    {
        string data = String.Empty;

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest20()
    {
        const string data = "user_id,name\r\n1,Bruce";

        using var csv = ReadCsv.FromString(data, hasHeaders: true);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv[1], Is.EqualTo("Bruce"));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv["user_id"], Is.EqualTo("1"));
        Assert.That(csv["name"], Is.EqualTo("Bruce"));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void SupportsMultilineQuotedFields()
    {
        const string data = "\"data \r\n here\"";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("data \r\n here"));
    }

    [Test]
    public void ParsingTest22()
    {
        const string data = ",,\n1,";

        using var csv = ReadCsv.FromString(data, delimiter: ',', emptyLineAction: EmptyLineAction.None, missingFieldAction: MissingFieldAction.ReplaceByNull);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv.FieldCount, Is.EqualTo(3));

        Assert.That(csv[0], Is.EqualTo(String.Empty));
        Assert.That(csv[1], Is.EqualTo(String.Empty));
        Assert.That(csv[2], Is.EqualTo(String.Empty));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv[1], Is.EqualTo(String.Empty));
        Assert.That(csv[2], Is.Null);
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest23()
    {
        const string data = "\"double\"\"\"\"double quotes\"";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("double\"\"double quotes"));
    }

    [Test]
    public void ParsingTest24()
    {
        const string data = "1\r";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
    }

    [Test]
    public void ParsingTest25()
    {
        const string data = "1\r\n";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
    }

    [Test]
    public void ParsingTest26()
    {
        const string data = "1\n";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
    }

    [Test]
    public void ParsingTest27()
    {
        const string data = "'bob said, ''Hey!''',2, 3 ";

        using var csv = ReadCsv.FromString(data, quote: '\'', escape: '\'', trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("bob said, 'Hey!'"));
        Assert.That(csv[1], Is.EqualTo("2"));
        Assert.That(csv[2], Is.EqualTo("3"));
    }

    [Test]
    public void ParsingTest28()
    {
        const string data = "\"data \"\" here\"";

        using var csv = ReadCsv.FromString(data, quote: '\0', escape: '\\');
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("\"data \"\" here\""));
    }

    [Test]
    public void ParsingTest29()
    {
        string data = new String('a', 75) + "," + new String('b', 75);

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(new String('a', 75)));
        Assert.That(csv[1], Is.EqualTo(new String('b', 75)));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest30()
    {
        const string data = "1\r\n\r\n1";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest31()
    {
        const string data = "1\r\n# bunch of crazy stuff here\r\n1";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
    }

    [Test]
    public void ParsingTest32()
    {
        const string data = "\"1\",Bruce\r\n\"2\n\",Toni\r\n\"3\",Brian\r\n";

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("1"));
        Assert.That(csv[1], Is.EqualTo("Bruce"));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("2\n"));
        Assert.That(csv[1], Is.EqualTo("Toni"));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("3"));
        Assert.That(csv[1], Is.EqualTo("Brian"));
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest33()
    {
        const string data = "\"double\\\\\\\\double backslash\"";

        using var csv = ReadCsv.FromString(data, escape: '\\');
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("double\\\\double backslash"));
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest34()
    {
        const string data = "\"Chicane\", \"Love on the Run\", \"Knight Rider\", \"This field contains a comma, but it doesn't matter as the field is quoted\"\r\n" +
                  "\"Samuel Barber\", \"Adagio for Strings\", \"Classical\", \"This field contains a double quote character, \"\", but it doesn't matter as it is escaped\"";

        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("Chicane"));
        Assert.That(csv[1], Is.EqualTo("Love on the Run"));
        Assert.That(csv[2], Is.EqualTo("Knight Rider"));
        Assert.That(csv[3], Is.EqualTo("This field contains a comma, but it doesn't matter as the field is quoted"));
        Assert.That(csv.FieldCount, Is.EqualTo(4));
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("Samuel Barber"));
        Assert.That(csv[1], Is.EqualTo("Adagio for Strings"));
        Assert.That(csv[2], Is.EqualTo("Classical"));
        Assert.That(csv[3], Is.EqualTo("This field contains a double quote character, \", but it doesn't matter as it is escaped"));
        Assert.That(csv.FieldCount, Is.EqualTo(4));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest35()
    {
        var data = "\t";
        using var csv = ReadCsv.FromString(data, delimiter: '\t');
        Assert.That(csv.FieldCount, Is.EqualTo(2));

        Assert.That(csv.Read(), Is.True);

        Assert.That(csv[0], Is.EqualTo(string.Empty));
        Assert.That(csv[1], Is.EqualTo(string.Empty));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest36()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true);
    }

    [Test]
    public void ParsingTest37()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1,
            hasHeaders: true,
            trimmingOptions: ValueTrimmingOptions.UnquotedOnly,
            schema: CsvReaderSampleData.SampleData1Schema);
        CsvReaderSampleData.CheckSampleData1(csv, true, false);
    }

    [Test]
    public void ParsingTest38()
    {
        using var reader = ReadCsv.FromString("abc,def,ghi\n");
        int fieldCount = reader.FieldCount;

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader[0], Is.EqualTo("abc"));
        Assert.That(reader[1], Is.EqualTo("def"));
        Assert.That(reader[2], Is.EqualTo("ghi"));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void ParsingTest39()
    {
        using var csv = ReadCsv.FromString("00,01,   \n10,11,   ", trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("00"));
        Assert.That(csv[1], Is.EqualTo("01"));
        Assert.That(csv[2], Is.EqualTo(""));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("10"));
        Assert.That(csv[1], Is.EqualTo("11"));
        Assert.That(csv[2], Is.EqualTo(""));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest40()
    {
        const string data = "\"00\",\n\"10\",";
        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.FieldCount, Is.EqualTo(2));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("00"));
        Assert.That(csv[1], Is.EqualTo(string.Empty));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("10"));
        Assert.That(csv[1], Is.EqualTo(string.Empty));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void UnquotedValuesShouldBeTrimmed()
    {
        const string data = "First record          ,Second record";
        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        Assert.That(csv.FieldCount, Is.EqualTo(2));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("First record"));
        Assert.That(csv[1], Is.EqualTo("Second record"));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void SpaceBecomesEmptyField()
    {
        var data = " ";
        using var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly, emptyLineAction: EmptyLineAction.None);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv.FieldCount, Is.EqualTo(1));
        Assert.That(csv[0], Is.EqualTo(string.Empty));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void ParsingTest43()
    {
        var data = "a,b\n   ";
        using var csv = ReadCsv.FromString(data,
            missingFieldAction: MissingFieldAction.ReplaceByNull,
            trimmingOptions: ValueTrimmingOptions.All,
            emptyLineAction: EmptyLineAction.None);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv.FieldCount, Is.EqualTo(2));
        Assert.That(csv[0], Is.EqualTo("a"));
        Assert.That(csv[1], Is.EqualTo("b"));

        csv.Read();
        Assert.That(csv[0], Is.EqualTo(string.Empty));
        Assert.That(csv[1], Is.EqualTo(null));
    }

    #endregion

    #region UnicodeParsing tests

    [Test]
    public void UnicodeParsingTest1()
    {
        // control characters and comma are skipped

        char[] raw = new char[65536 - 13];

        for (int i = 0; i < raw.Length; i++)
            raw[i] = (char)(i + 14);

        raw[44 - 14] = ' '; // skip comma

        var data = new string(raw);

        using var csv = ReadCsv.FromString(data);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(data));
        Assert.That(csv.Read(), Is.False);
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

    [Test]
    public void UnicodeParsingTest2()
    {
        var test = "München";
        var buffer = ToByteArray(test);
        using var csv = ReadCsv.FromStream(new MemoryStream(buffer), encoding: Encoding.Unicode);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(test));
        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void UnicodeParsingTest3()
    {
        var test = "München";
        var buffer = ToByteArray(test);
        using var csv = ReadCsv.FromStream(new MemoryStream(buffer), encoding: Encoding.Unicode);
        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(test));
        Assert.That(csv.Read(), Is.False);
    }

    #endregion

    #region FieldCount

    [Test]
    public void FieldCountTest1()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1,
            trimmingOptions: ValueTrimmingOptions.UnquotedOnly
            );
        CsvReaderSampleData.CheckSampleData1(csv, false, true);
    }

    #endregion

    #region GetFieldHeaders

    [Test]
    public void GetFieldHeadersTest1()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1);
        string[] headers = csv.GetFieldHeaders();

        Assert.That(headers, Is.Not.Null);
        Assert.That(headers.Length, Is.EqualTo(6));
    }

    [Test]
    public void GetFieldHeadersTest2()
    {
        using var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, schema: CsvReaderSampleData.SampleData1Schema);
        string[] headers = csv.GetFieldHeaders();

        Assert.That(headers, Is.Not.Null);
        Assert.That(headers.Length, Is.EqualTo(CsvReaderSampleData.SampleData1RecordCount));

        Assert.That(headers[0], Is.EqualTo(CsvReaderSampleData.SampleData1Header0));
        Assert.That(headers[1], Is.EqualTo(CsvReaderSampleData.SampleData1Header1));
        Assert.That(headers[2], Is.EqualTo(CsvReaderSampleData.SampleData1Header2));
        Assert.That(headers[3], Is.EqualTo(CsvReaderSampleData.SampleData1Header3));
        Assert.That(headers[4], Is.EqualTo(CsvReaderSampleData.SampleData1Header4));
        Assert.That(headers[5], Is.EqualTo(CsvReaderSampleData.SampleData1Header5));

        Assert.That(csv.GetOrdinal(CsvReaderSampleData.SampleData1Header0), Is.EqualTo(0));
        Assert.That(csv.GetOrdinal(CsvReaderSampleData.SampleData1Header1), Is.EqualTo(1));
        Assert.That(csv.GetOrdinal(CsvReaderSampleData.SampleData1Header2), Is.EqualTo(2));
        Assert.That(csv.GetOrdinal(CsvReaderSampleData.SampleData1Header3), Is.EqualTo(3));
        Assert.That(csv.GetOrdinal(CsvReaderSampleData.SampleData1Header4), Is.EqualTo(4));
        Assert.That(csv.GetOrdinal(CsvReaderSampleData.SampleData1Header5), Is.EqualTo(5));
    }

    [Test]
    public void OnlyComments()
    {
        var data = "#asdf\n\n#asdf,asdf";
        using var csv = ReadCsv.FromString(data, hasHeaders: true);
        string[] headers = csv.GetFieldHeaders();

        Assert.That(headers, Is.Not.Null);
        Assert.That(headers.Length, Is.EqualTo(0));
    }

    [Test]
    public void GetFieldHeaders_WithEmptyHeaderNames()
    {
        var data = ",  ,,aaa,\"   \",,,";

        using var csv = ReadCsv.FromString(data, hasHeaders: true);
        Assert.That(csv.Read(), Is.False);
        Assert.That(csv.FieldCount, Is.EqualTo(8));

        string[] headers = csv.GetFieldHeaders();
        Assert.That(headers.Length, Is.EqualTo(csv.FieldCount));

        Assert.That(headers[3], Is.EqualTo("aaa"));
        foreach (var index in new int[] { 0, 1, 2, 4, 5, 6, 7 })
            Assert.That(headers[index], Is.EqualTo("Column" + index.ToString()));
    }

    #endregion

    #region SkipEmptyLines

    [Test]
    public void SkipEmptyLinesTest1()
    {
        var data = "00\n\n10";
        using var csv = ReadCsv.FromString(data, emptyLineAction: EmptyLineAction.None);
        Assert.That(csv.FieldCount, Is.EqualTo(1));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("00"));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo(string.Empty));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("10"));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void SkipEmptyLinesTest2()
    {
        var data = "00\n\n10";
        using var csv = ReadCsv.FromString(data, emptyLineAction: EmptyLineAction.Skip);
        Assert.That(csv.FieldCount, Is.EqualTo(1));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("00"));

        Assert.That(csv.Read(), Is.True);
        Assert.That(csv[0], Is.EqualTo("10"));

        Assert.That(csv.Read(), Is.False);
    }

    #endregion

    #region Trimming tests

    [TestCase("", ValueTrimmingOptions.None, new string[] { })]
    [TestCase("", ValueTrimmingOptions.QuotedOnly, new string[] { })]
    [TestCase("", ValueTrimmingOptions.UnquotedOnly, new string[] { })]
    [TestCase(" aaa , bbb , ccc ", ValueTrimmingOptions.None, new string[] { " aaa ", " bbb ", " ccc " })]
    [TestCase(" aaa , bbb , ccc ", ValueTrimmingOptions.QuotedOnly, new string[] { " aaa ", " bbb ", " ccc " })]
    [TestCase(" aaa , bbb , ccc ", ValueTrimmingOptions.UnquotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [TestCase("\" aaa \",\" bbb \",\" ccc \"", ValueTrimmingOptions.None, new string[] { " aaa ", " bbb ", " ccc " })]
    [TestCase("\" aaa \",\" bbb \",\" ccc \"", ValueTrimmingOptions.QuotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [TestCase("\" aaa \",\" bbb \",\" ccc \" ", ValueTrimmingOptions.QuotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [TestCase("\" aaa \",\" bbb \" ,\" ccc \"", ValueTrimmingOptions.QuotedOnly, new string[] { "aaa", "bbb", "ccc" })]
    [TestCase("\" aaa \",\" bbb \",\" ccc \"", ValueTrimmingOptions.UnquotedOnly, new string[] { " aaa ", " bbb ", " ccc " })]
    [TestCase(" aaa , bbb ,\" ccc \"", ValueTrimmingOptions.None, new string[] { " aaa ", " bbb ", " ccc " })]
    [TestCase(" aaa , bbb ,\" ccc \"", ValueTrimmingOptions.QuotedOnly, new string[] { " aaa ", " bbb ", "ccc" })]
    [TestCase(" aaa , bbb ,\" ccc \"", ValueTrimmingOptions.UnquotedOnly, new string[] { "aaa", "bbb", " ccc " })]
    public void TrimFieldValuesTest(string data, ValueTrimmingOptions trimmingOptions, params string[] expected)
    {
        using var csv = ReadCsv.FromString(data, trimmingOptions: trimmingOptions);
        while (csv.Read())
        {
            var actual = new string[csv.FieldCount];
            csv.GetValues(actual);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    #endregion
}

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
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1))
            {
                csv.Read();
                var s = csv[-1];
            }
        });
    }

    [Test]
    public void Indexer_BeyondRecordSize_Throws()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1))
            {
                csv.Read();
                var s = csv[CsvReaderSampleData.SampleData1RecordCount];
            }
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_IndexerByInvalidHeader_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1))
            {
                csv.Read();
                var s = csv["asdf"];
            }
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_IndexerByValidHeader_DataHasHeaders_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false))
            {
                csv.Read();
                var s = csv[CsvReaderSampleData.SampleData1Header0];
            }
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_Indexer_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false))
            {
                csv.Read();
                var s = csv[null];
            }
        });
    }

    [Test]
    public void ConstructedWithoutHeaders_IndexByEmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: false))
            {
                csv.Read();
                var s = csv[string.Empty];
            }
        });
    }

    [Test]
    public void ConstructedWithHeaders_IndexByNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true))
            {
                csv.Read();
                var s = csv[null];
            }
        });
    }

    [Test]
    public void ConstructedWithHeaders_IndexByEmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true))
            {
                csv.Read();
                var s = csv[string.Empty];
            }
        });
    }

    [Test]
    public void ConstructedWithHeaders_IndexByNonExistingHeader_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true))
            {
                csv.Read();
                var s = csv["asdf"];
            }
        });
    }

    #endregion

    #region CopyCurrentRecordTo

    [Test]
    public void GetValues_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1))
            {
                csv.Read();
                csv.GetValues(null);
            }
        });
    }

    [Test]
    public void GetValues_ArrayTooSmall_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1))
            {
                csv.Read();
                csv.GetValues(new string[CsvReaderSampleData.SampleData1RecordCount - 1]);
            }
        });
    }


    #endregion

    #endregion

    #region Parsing tests

    [Test]
    public void ParsingTest1()
    {
        const string data = "1\r\n\r\n1";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest2()
    {
        // ["Bob said, ""Hey!""",2, 3 ]
        const string data = "\"Bob said, \"\"Hey!\"\"\",2, 3 ";

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(@"Bob said, ""Hey!""", csv[0]);
            Assert.AreEqual("2", csv[1]);
            Assert.AreEqual("3", csv[2]);

            Assert.IsFalse(csv.Read());
        }

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.None))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(@"Bob said, ""Hey!""", csv[0]);
            Assert.AreEqual("2", csv[1]);
            Assert.AreEqual(" 3 ", csv[2]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest3()
    {
        const string data = "1\r2\n";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("2", csv[0]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest4()
    {
        const string data = "\"\n\r\n\n\r\r\",,\t,\n";

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());

            Assert.AreEqual(4, csv.FieldCount);

            Assert.AreEqual("\n\r\n\n\r\r", csv[0]);
            Assert.AreEqual("", csv[1]);
            Assert.AreEqual("", csv[2]);
            Assert.AreEqual("", csv[3]);

            Assert.IsFalse(csv.Read());
        }
    }


    [Test]
    public void ParsingTest6()
    {
        const string data = "1,2";
        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual("2", csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest7()
    {
        const string data = "\r\n1\r\n";
        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(1, csv.FieldCount);
            Assert.AreEqual("1", csv[0]);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest8()
    {
        const string data = "\"bob said, \"\"Hey!\"\"\",2, 3 ";

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("bob said, \"Hey!\"", csv[0]);
            Assert.AreEqual("2", csv[1]);
            Assert.AreEqual("3", csv[2]);
            Assert.AreEqual(3, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest9()
    {
        const string data = ",";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(String.Empty, csv[0]);
            Assert.AreEqual(String.Empty, csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
        }
    }

    [Test]
    public void ParsingTest10()
    {
        const string data = "1\r2";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("2", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest11()
    {
        const string data = "1\n2";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("2", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest12()
    {
        const string data = "1\r\n2";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("2", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest13()
    {
        const string data = "1\r";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest14()
    {
        const string data = "1\n";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest15()
    {
        const string data = "1\r\n";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void SupportsCarriageReturnAsDelimiter()
    {
        const string data = "1\r2\n";

        using (var csv = ReadCsv.FromString(data, delimiter: '\r', trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(2, csv.FieldCount);
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual("2", csv[1]);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest17()
    {
        const string data = "\"July 4th, 2005\"";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("July 4th, 2005", csv[0]);
        }
    }

    [Test]
    public void ParsingTest18()
    {
        const string data = " 1";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(" 1", csv[0]);
        }
    }

    [Test]
    public void ParsingTest19()
    {
        string data = String.Empty;

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest20()
    {
        const string data = "user_id,name\r\n1,Bruce";

        using (var csv = ReadCsv.FromString(data, hasHeaders: true))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual("Bruce", csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
            Assert.AreEqual("1", csv["user_id"]);
            Assert.AreEqual("Bruce", csv["name"]);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void SupportsMultilineQuotedFields()
    {
        const string data = "\"data \r\n here\"";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("data \r\n here", csv[0]);
        }
    }

    [Test]
    public void ParsingTest22()
    {
        const string data = ",,\n1,";

        using (var csv = ReadCsv.FromString(data, delimiter: ',', skipEmptyLines: false, missingFieldAction: MissingFieldAction.ReplaceByNull))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(3, csv.FieldCount);

            Assert.AreEqual(String.Empty, csv[0]);
            Assert.AreEqual(String.Empty, csv[1]);
            Assert.AreEqual(String.Empty, csv[2]);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(String.Empty, csv[1]);
            Assert.IsNull(csv[2]);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest23()
    {
        const string data = "\"double\"\"\"\"double quotes\"";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("double\"\"double quotes", csv[0]);
        }
    }

    [Test]
    public void ParsingTest24()
    {
        const string data = "1\r";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
        }
    }

    [Test]
    public void ParsingTest25()
    {
        const string data = "1\r\n";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
        }
    }

    [Test]
    public void ParsingTest26()
    {
        const string data = "1\n";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
        }
    }

    [Test]
    public void ParsingTest27()
    {
        const string data = "'bob said, ''Hey!''',2, 3 ";

        using (var csv = ReadCsv.FromString(data, quote: '\'', escape: '\'', trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("bob said, 'Hey!'", csv[0]);
            Assert.AreEqual("2", csv[1]);
            Assert.AreEqual("3", csv[2]);
        }
    }

    [Test]
    public void ParsingTest28()
    {
        const string data = "\"data \"\" here\"";

        using (var csv = ReadCsv.FromString(data, quote: '\0', escape: '\\'))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("\"data \"\" here\"", csv[0]);
        }
    }

    [Test]
    public void ParsingTest29()
    {
        string data = new String('a', 75) + "," + new String('b', 75);

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(new String('a', 75), csv[0]);
            Assert.AreEqual(new String('b', 75), csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest30()
    {
        const string data = "1\r\n\r\n1";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest31()
    {
        const string data = "1\r\n# bunch of crazy stuff here\r\n1";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
        }
    }

    [Test]
    public void ParsingTest32()
    {
        const string data = "\"1\",Bruce\r\n\"2\n\",Toni\r\n\"3\",Brian\r\n";

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("1", csv[0]);
            Assert.AreEqual("Bruce", csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("2\n", csv[0]);
            Assert.AreEqual("Toni", csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("3", csv[0]);
            Assert.AreEqual("Brian", csv[1]);
            Assert.AreEqual(2, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest33()
    {
        const string data = "\"double\\\\\\\\double backslash\"";

        using (var csv = ReadCsv.FromString(data, escape: '\\'))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("double\\\\double backslash", csv[0]);
            Assert.AreEqual(1, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest34()
    {
        const string data = "\"Chicane\", \"Love on the Run\", \"Knight Rider\", \"This field contains a comma, but it doesn't matter as the field is quoted\"\r\n" +
                  "\"Samuel Barber\", \"Adagio for Strings\", \"Classical\", \"This field contains a double quote character, \"\", but it doesn't matter as it is escaped\"";

        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("Chicane", csv[0]);
            Assert.AreEqual("Love on the Run", csv[1]);
            Assert.AreEqual("Knight Rider", csv[2]);
            Assert.AreEqual("This field contains a comma, but it doesn't matter as the field is quoted", csv[3]);
            Assert.AreEqual(4, csv.FieldCount);
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("Samuel Barber", csv[0]);
            Assert.AreEqual("Adagio for Strings", csv[1]);
            Assert.AreEqual("Classical", csv[2]);
            Assert.AreEqual("This field contains a double quote character, \", but it doesn't matter as it is escaped", csv[3]);
            Assert.AreEqual(4, csv.FieldCount);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest35()
    {
        var data = "\t";
        using (var csv = ReadCsv.FromString(data, delimiter: '\t'))
        {
            Assert.AreEqual(2, csv.FieldCount);

            Assert.IsTrue(csv.Read());

            Assert.AreEqual(string.Empty, csv[0]);
            Assert.AreEqual(string.Empty, csv[1]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest36()
    {
        using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, hasHeaders: true))
        {
        }
    }

    [Test]
    public void ParsingTest37()
    {
        using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1,
            hasHeaders: true,
            trimmingOptions: ValueTrimmingOptions.UnquotedOnly,
            schema: CsvReaderSampleData.SampleData1Schema))
        {
            CsvReaderSampleData.CheckSampleData1(csv, true, false);
        }
    }

    [Test]
    public void ParsingTest38()
    {
        using (var reader = ReadCsv.FromString("abc,def,ghi\n"))
        {
            int fieldCount = reader.FieldCount;

            Assert.IsTrue(reader.Read());
            Assert.AreEqual("abc", reader[0]);
            Assert.AreEqual("def", reader[1]);
            Assert.AreEqual("ghi", reader[2]);

            Assert.IsFalse(reader.Read());
        }
    }

    [Test]
    public void ParsingTest39()
    {
        using (var csv = ReadCsv.FromString("00,01,   \n10,11,   ", trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual("00", csv[0]);
            Assert.AreEqual("01", csv[1]);
            Assert.AreEqual("", csv[2]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("10", csv[0]);
            Assert.AreEqual("11", csv[1]);
            Assert.AreEqual("", csv[2]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest40()
    {
        const string data = "\"00\",\n\"10\",";
        using (var csv = ReadCsv.FromString(data))
        {
            Assert.AreEqual(2, csv.FieldCount);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("00", csv[0]);
            Assert.AreEqual(string.Empty, csv[1]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("10", csv[0]);
            Assert.AreEqual(string.Empty, csv[1]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void UnquotedValuesShouldBeTrimmed()
    {
        const string data = "First record          ,Second record";
        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly))
        {
            Assert.AreEqual(2, csv.FieldCount);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("First record", csv[0]);
            Assert.AreEqual("Second record", csv[1]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void SpaceBecomesEmptyField()
    {
        var data = " ";
        using (var csv = ReadCsv.FromString(data, trimmingOptions: ValueTrimmingOptions.UnquotedOnly, skipEmptyLines: false))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(1, csv.FieldCount);
            Assert.AreEqual(string.Empty, csv[0]);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void ParsingTest43()
    {
        var data = "a,b\n   ";
        using (var csv = ReadCsv.FromString(data,
            missingFieldAction: MissingFieldAction.ReplaceByNull,
            trimmingOptions: ValueTrimmingOptions.All,
            skipEmptyLines: false))

        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(2, csv.FieldCount);
            Assert.AreEqual("a", csv[0]);
            Assert.AreEqual("b", csv[1]);

            csv.Read();
            Assert.AreEqual(string.Empty, csv[0]);
            Assert.AreEqual(null, csv[1]);
        }
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

        string data = new string(raw);

        using (var csv = ReadCsv.FromString(data))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(data, csv[0]);
            Assert.IsFalse(csv.Read());
        }
    }

    byte[] ToByteArray(string s)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (TextWriter writer = new StreamWriter(stream, Encoding.Unicode))
            {
                writer.WriteLine(s);
            }

            return stream.ToArray();
        }
    }

    [Test]
    public void UnicodeParsingTest2()
    {
        var test = "München";
        var buffer = ToByteArray(test);
        using (var csv = ReadCsv.FromStream(new MemoryStream(buffer), encoding: Encoding.Unicode))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(test, csv[0]);
            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void UnicodeParsingTest3()
    {
        var test = "München";
        var buffer = ToByteArray(test);
        using (var csv = ReadCsv.FromStream(new MemoryStream(buffer), encoding: Encoding.Unicode))
        {
            Assert.IsTrue(csv.Read());
            Assert.AreEqual(test, csv[0]);
            Assert.IsFalse(csv.Read());
        }
    }

    #endregion

    #region FieldCount

    [Test]
    public void FieldCountTest1()
    {
        using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1,
            trimmingOptions: ValueTrimmingOptions.UnquotedOnly
            ))
        {
            CsvReaderSampleData.CheckSampleData1(csv, false, true);
        }
    }

    #endregion

    #region GetFieldHeaders

    [Test]
    public void GetFieldHeadersTest1()
    {
        using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1))
        {
            string[] headers = csv.GetFieldHeaders();

            Assert.IsNotNull(headers);
            Assert.AreEqual(6, headers.Length);
        }
    }

    [Test]
    public void GetFieldHeadersTest2()
    {
        using (var csv = ReadCsv.FromString(CsvReaderSampleData.SampleData1, schema: CsvReaderSampleData.SampleData1Schema))
        {
            string[] headers = csv.GetFieldHeaders();

            Assert.IsNotNull(headers);
            Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount, headers.Length);

            Assert.AreEqual(CsvReaderSampleData.SampleData1Header0, headers[0]);
            Assert.AreEqual(CsvReaderSampleData.SampleData1Header1, headers[1]);
            Assert.AreEqual(CsvReaderSampleData.SampleData1Header2, headers[2]);
            Assert.AreEqual(CsvReaderSampleData.SampleData1Header3, headers[3]);
            Assert.AreEqual(CsvReaderSampleData.SampleData1Header4, headers[4]);
            Assert.AreEqual(CsvReaderSampleData.SampleData1Header5, headers[5]);

            Assert.AreEqual(0, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header0));
            Assert.AreEqual(1, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header1));
            Assert.AreEqual(2, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header2));
            Assert.AreEqual(3, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header3));
            Assert.AreEqual(4, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header4));
            Assert.AreEqual(5, csv.GetOrdinal(CsvReaderSampleData.SampleData1Header5));
        }
    }

    [Test]
    public void OnlyComments()
    {
        var data = "#asdf\n\n#asdf,asdf";
        using (var csv = ReadCsv.FromString(data, hasHeaders: true))
        {
            string[] headers = csv.GetFieldHeaders();

            Assert.IsNotNull(headers);
            Assert.AreEqual(0, headers.Length);
        }
    }

    [Test]
    public void GetFieldHeaders_WithEmptyHeaderNames()
    {
        var data = ",  ,,aaa,\"   \",,,";

        using (var csv = ReadCsv.FromString(data, hasHeaders: true))
        {
            Assert.IsFalse(csv.Read());
            Assert.AreEqual(8, csv.FieldCount);

            string[] headers = csv.GetFieldHeaders();
            Assert.AreEqual(csv.FieldCount, headers.Length);

            Assert.AreEqual("aaa", headers[3]);
            foreach (var index in new int[] { 0, 1, 2, 4, 5, 6, 7 })
                Assert.AreEqual("Column" + index.ToString(), headers[index]);
        }
    }

    #endregion

    #region SkipEmptyLines

    [Test]
    public void SkipEmptyLinesTest1()
    {
        var data = "00\n\n10";
        using (var csv = ReadCsv.FromString(data, skipEmptyLines: false))
        {
            Assert.AreEqual(1, csv.FieldCount);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("00", csv[0]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual(string.Empty, csv[0]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("10", csv[0]);

            Assert.IsFalse(csv.Read());
        }
    }

    [Test]
    public void SkipEmptyLinesTest2()
    {
        var data = "00\n\n10";
        using (var csv = ReadCsv.FromString(data, skipEmptyLines: true))
        {
            Assert.AreEqual(1, csv.FieldCount);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("00", csv[0]);

            Assert.IsTrue(csv.Read());
            Assert.AreEqual("10", csv[0]);

            Assert.IsFalse(csv.Read());
        }
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
        using (var csv = ReadCsv.FromString(data, trimmingOptions: trimmingOptions))
        {
            while (csv.Read())
            {
                var actual = new string[csv.FieldCount];
                csv.GetValues(actual);

                CollectionAssert.AreEqual(expected, actual);
            }
        }
    }

    #endregion
}

//	LumenWorks.Framework.Tests.Unit.IO.CSV.CsvReaderMalformedTest
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
using NUnit.Framework;

namespace Net.Code.Csv.Tests.Unit.IO.Csv
{
	[TestFixture()]
	public class CsvReaderMalformedTest
	{

		[Test]
		public void MissingFieldQuotedTest1()
		{
			const string Data = "a,b,c,d\n" +
			                    "1,1,1,1\n" +
			                    "2,\"2\"\n" +
			                    "3,3,3,3";

			try
			{
				using (CsvReader csv = new CsvReader(new StringReader(Data)))
				{
					while (csv.ReadNextRecord())
						for (int i = 0; i < csv.FieldCount; i++)
						{
							string s = csv[i];
						}
				}
			}
			catch (MissingFieldCsvException ex)
			{
                Assert.AreEqual(new { LineNumber = 3L, FieldNumber = 2, ColumnNumber = 6 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        	}
		}

		[Test()]
		public void MissingFieldQuotedTest2()
		{
			const string Data = "a,b,c,d\n" +
			                    "1,1,1,1\n" +
			                    "2,\"2\",\n" +
			                    "3,3,3,3";

			try
			{
				using (CsvReader csv = new CsvReader(new StringReader(Data), false, 11))
				{
					while (csv.ReadNextRecord())
						for (int i = 0; i < csv.FieldCount; i++)
						{
							string s = csv[i];
						}
				}
			}
			catch (MissingFieldCsvException ex)
			{
                Assert.AreEqual(new { LineNumber = 3L, FieldNumber = 3, ColumnNumber = 7 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        	}
		}

		[Test()]
		public void MissingFieldQuotedTest3()
		{
			const string Data = "a,b,c,d\n" +
			                    "1,1,1,1\n" +
			                    "2,\"2\"\n" +
			                    "\"3\",3,3,3";

			try
			{
				using (CsvReader csv = new CsvReader(new StringReader(Data), false))
				{
					while (csv.ReadNextRecord())
						for (int i = 0; i < csv.FieldCount; i++)
						{
							string s = csv[i];
						}
				}
			}
			catch (MissingFieldCsvException ex)
			{
                Assert.AreEqual(new { LineNumber = 3L, FieldNumber = 2, ColumnNumber = 6 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
            }
        }

		[Test()]
		public void MissingFieldQuotedTest4()
		{
			const string Data = "a,b,c,d\n" +
			                    "1,1,1,1\n" +
			                    "2,\"2\",\n" +
			                    "\"3\",3,3,3";

			try
			{
				using (CsvReader csv = new CsvReader(new StringReader(Data), false, 16))
				{
					while (csv.ReadNextRecord())
						for (int i = 0; i < csv.FieldCount; i++)
						{
							string s = csv[i];
						}
				}
			}
			catch (MissingFieldCsvException ex)
			{
                Assert.AreEqual(new { LineNumber = 3L, FieldNumber = 3, ColumnNumber = 7 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
            }
        }

		[Test]
		public void MissingDelimiterAfterQuotedFieldTest1()
		{
			const string Data = "\"111\",\"222\"\"333\"";

			try
			{
				using (CsvReader csv = new CsvReader(
                    new StringReader(Data),
                    layout: new CsvLayout(Escape:'\\'), 
                    behaviour: new CsvBehaviour(QuotesInsideQuotedFieldAction:QuotesInsideQuotedFieldAction.ThrowException)))
				{
					while (csv.ReadNextRecord())
						for (int i = 0; i < csv.FieldCount; i++)
						{
							string s = csv[i];
						}
				}
			}
			catch (MalformedCsvException ex)
			{
                Assert.AreEqual(new {LineNumber = 1L, FieldNumber = 1, ColumnNumber = 13}, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
			}
		}

		[Test]
		public void MissingDelimiterAfterQuotedFieldTest2()
		{
			const string Data = "\"111\",\"222\",\"333\"\n" +
			                    "\"111\",\"222\"\"333\"";

			try
			{
				using (CsvReader csv = new CsvReader(new StringReader(Data), 4096, new CsvLayout(Escape:'\\'), 
                    new CsvBehaviour(QuotesInsideQuotedFieldAction:QuotesInsideQuotedFieldAction.ThrowException)))
				{
					while (csv.ReadNextRecord())
						for (int i = 0; i < csv.FieldCount; i++)
						{
							string s = csv[i];
						}
				}
			}
			catch (MalformedCsvException ex)
			{
                Assert.AreEqual(new { LineNumber = 2L, FieldNumber = 1, ColumnNumber = 13 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
            }
        }

		[Test()]
		public void MoreFieldsTest()
		{
			const string Data = "ORIGIN,DESTINATION\nPHL,FLL,kjhkj kjhkjh,eg,fhgf\nNYC,LAX";

			using (CsvReader csv = new CsvReader(new System.IO.StringReader(Data), false))
			{
				while (csv.ReadNextRecord())
				{
					for (int i = 0; i < csv.FieldCount; i++)
					{
						string s = csv[i];
					}
				}
			}
		}

		[Test()]
		public void MoreFieldsMultilineTest()
		{
			const string Data = "ORIGIN,DESTINATION\nPHL,FLL,kjhkj kjhkjh,eg,fhgf\nNYC,LAX";

			using (CsvReader csv = new CsvReader(new System.IO.StringReader(Data), false))
			{
				while (csv.ReadNextRecord())
				{
					for (int i = 0; i < csv.FieldCount; i++)
					{
						string s = csv[i];
					}
				}
			}
		}

		[Test]
		public void ParseErrorBeforeInitializeTest()
		{
			const string Data = "\"0022 - SKABELON\";\"\"Tandremstrammer\";\"\";\"0,00\";\"\"\n" +
			                    "\"15907\";\"\"BOLT TIL 2-05-405\";\"\";\"42,50\";\"4027816159070\"\n" +
			                    "\"19324\";\"FJEDER TIL 2-05-405\";\"\";\"14,50\";\"4027816193241\"";

			using (var csv = new CsvReader(new System.IO.StringReader(Data), 4096, new CsvLayout(Delimiter:';'), new CsvBehaviour(QuotesInsideQuotedFieldAction:QuotesInsideQuotedFieldAction.AdvanceToNextLine)))
			{
				Assert.IsTrue(csv.ReadNextRecord());

				Assert.AreEqual("19324", csv[0]);
				Assert.AreEqual("FJEDER TIL 2-05-405", csv[1]);
				Assert.AreEqual("", csv[2]);
				Assert.AreEqual("14,50", csv[3]);
				Assert.AreEqual("4027816193241", csv[4]);

				Assert.IsFalse(csv.ReadNextRecord());
			}
		}

		[Test]
		public void LastFieldEmptyFollowedByMissingFieldsOnNextRecord()
		{
			const string Data = "a,b,c,d,e"
				+ "\na,b,c,d,"
				+ "\na,b,";

			using (var csv = new CsvReader(new StringReader(Data), CsvReader.DefaultBufferSize, new CsvLayout(HasHeaders:false), 
                new  CsvBehaviour(MissingFieldAction:MissingFieldAction.ReplaceByNull)))
			{
				var record = new string[5];

				Assert.IsTrue(csv.ReadNextRecord());
				csv.CopyCurrentRecordTo(record);
				CollectionAssert.AreEqual(new string[] { "a", "b", "c", "d", "e" }, record);

				Assert.IsTrue(csv.ReadNextRecord());
				csv.CopyCurrentRecordTo(record);
				CollectionAssert.AreEqual(new string[] { "a", "b", "c", "d", "" }, record);

				Assert.IsTrue(csv.ReadNextRecord());
				csv.CopyCurrentRecordTo(record);
				CollectionAssert.AreEqual(new string[] { "a", "b", "", null, null }, record);

				Assert.IsFalse(csv.ReadNextRecord());
			}
		}
	}
}
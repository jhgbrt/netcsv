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

using System.Diagnostics;

namespace Net.Code.Csv.Tests.Unit.IO.Csv;

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
            using var csv = ReadCsv.FromString(Data);
            while (csv.Read())
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    string s = csv.GetString(i);
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
            using var csv = ReadCsv.FromString(Data);
            while (csv.Read())
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    string s = csv.GetString(i);
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
            using var csv = ReadCsv.FromString(Data);
            while (csv.Read())
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    string s = csv.GetString(i);
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
            using var csv = ReadCsv.FromString(Data);
            while (csv.Read())
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    string s = csv.GetString(i);
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
            using var csv = ReadCsv.FromString(
                Data,
                escape: '\\',
                quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.ThrowException);
        }
        catch (MalformedCsvException ex)
        {
            Assert.AreEqual(new { LineNumber = 1L, FieldNumber = 1, ColumnNumber = 13 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Test]
    public void MissingDelimiterAfterQuotedFieldTest2()
    {
        const string Data = "\"111\",\"222\",\"333\"\n" +
                            "\"111\",\"222\"\"333\"";

        try
        {
            using var csv = ReadCsv.FromString(Data, escape: '\\', quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.ThrowException);
            while (csv.Read())
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    string s = csv.GetString(i);
                }
        }
        catch (MalformedCsvException ex)
        {
            Assert.AreEqual(new { LineNumber = 2L, FieldNumber = 1, ColumnNumber = 13 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Test()]
    public void TrailingFields_Are_Ignored()
    {
        const string Data = "ORIGIN,DESTINATION\nPHL,FLL,kjhkj kjhkjh,eg,fhgf\nNYC,LAX";

        using var csv = ReadCsv.FromString(Data, hasHeaders: true);
        while (csv.Read())
        {
            Assert.AreEqual(2, csv.FieldCount);
            for (int i = 0; i < csv.FieldCount; i++)
            {
                _ = csv.GetString(i);
            }
        }
    }

    [Test]
    public void ParseErrorBeforeInitializeTest()
    {
        const string Data = "\"0022 - SKABELON\";\"\"Tandremstrammer\";\"\";\"0,00\";\"\"\n" +
                            "\"15907\";\"\"BOLT TIL 2-05-405\";\"\";\"42,50\";\"4027816159070\"\n" +
                            "\"19324\";\"FJEDER TIL 2-05-405\";\"\";\"14,50\";\"4027816193241\"";

        using var csv = ReadCsv.FromString(Data, delimiter: ';', quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.AdvanceToNextLine);
        Assert.IsTrue(csv.Read());

        Assert.AreEqual("19324", csv[0]);
        Assert.AreEqual("FJEDER TIL 2-05-405", csv[1]);
        Assert.AreEqual("", csv[2]);
        Assert.AreEqual("14,50", csv[3]);
        Assert.AreEqual("4027816193241", csv[4]);

        Assert.IsFalse(csv.Read());
    }

    [Test]
    public void LastFieldEmptyFollowedByMissingFieldsOnNextRecord()
    {
        const string Data = "a,b,c,d,e"
            + "\na,b,c,d,"
            + "\na,b,";

        using var csv = ReadCsv.FromString(Data, missingFieldAction: MissingFieldAction.ReplaceByNull);
        var record = new string[5];

        Assert.IsTrue(csv.Read());
        csv.GetValues(record);
        CollectionAssert.AreEqual(new string[] { "a", "b", "c", "d", "e" }, record);

        Assert.IsTrue(csv.Read());
        csv.GetValues(record);
        CollectionAssert.AreEqual(new string[] { "a", "b", "c", "d", "" }, record);

        Assert.IsTrue(csv.Read());
        csv.GetValues(record);
        CollectionAssert.AreEqual(new string[] { "a", "b", "", null, null }, record);

        Assert.IsFalse(csv.Read());
    }
}

public class MultiResultSetTests
{
    [TestFixture]
    public class CsvMultiResultSetTest
    {
        [Test]
        public void MultiResultOrdersExample()
        {
            var input = """"
            Id;FirstName;LastName;OrderDate;OrderTotal
            1;John;Doe;2023-01-01;100
            2;Jane;Doe;2023-01-02;110
            
            OrderId;ItemName;Quantity;Price
            1;Item 1;2;50
            2;Item 1;1;50
            2;Item 2;1;10
            2;Item 2;1;50
            """";

            var reader = ReadCsv.FromString(input, emptyLineAction: EmptyLineAction.NextResult, hasHeaders: true, delimiter: ';');
            while (reader.Read())
            {
                var orderId = reader.GetInt32(reader.GetOrdinal("Id"));
                var customerFirstName = reader.GetString(reader.GetOrdinal("FirstName"));
                var customerLastName = reader.GetString(reader.GetOrdinal("LastName"));
                var orderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"));
                var orderTotal = reader.GetDecimal(reader.GetOrdinal("OrderTotal"));
            }

            reader.NextResult();
            while (reader.Read())
            {
                var orderId = reader.GetInt32(reader.GetOrdinal("OrderId"));
                var itemName = reader.GetString(reader.GetOrdinal("ItemName"));
                var quantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
                var price = reader.GetDecimal(reader.GetOrdinal("Price"));
            }



        }
        [Test]
        public void MultiResultOrdersExampleWithSchemas()
        {
            var input = """"
            Id;FirstName;LastName;OrderDate;OrderTotal
            1;John;Doe;2023-01-01;100
            2;Jane;Doe;2023-01-02;110
            
            OrderId;ItemName;Quantity;Price
            1;Item 1;2;50
            2;Item 1;1;50
            2;Item 2;1;10
            2;Item 2;1;50
            """";


            var schemas = Schema.From<Order, OrderItem>();

            var reader = ReadCsv.FromString(input, emptyLineAction: EmptyLineAction.NextResult, hasHeaders: true, delimiter: ';', schema: schemas);

            var orders = reader.AsEnumerable<Order>().ToList();
            reader.NextResult();
            var items = reader.AsEnumerable<OrderItem>().ToList();

            Assert.AreEqual(2, orders.Count);
            Assert.AreEqual(4, items.Count);
        }

        record Order(int Id, string FirstName, string LastName, DateTime OrderDate, decimal OrderTotal);
        record OrderItem(int OrderId, string ItemName, int Quantity, decimal Price);

        [Test]
        public void CanReadMultiResult()
        {
            var input = """"
            a,b,c
            1,2,3

            d,e
            "x","y"
            """";


            var reader = ReadCsv.FromString(input, emptyLineAction: EmptyLineAction.NextResult, hasHeaders: true);

            while (reader.Read())
            {
                Assert.AreEqual("1", reader["a"]);
                Assert.AreEqual("2", reader["b"]);
                Assert.AreEqual("3", reader["c"]);
            }

            Assert.IsTrue(reader.NextResult());

            while (reader.Read())
            {
                Assert.AreEqual("x", reader["d"]);
                Assert.AreEqual("y", reader["e"]);
            }
        }
        [Test]
        public void CanReadMultiResultWithSchemas()
        {
            var input = """"
            a,b,c
            1,2,3

            d,e
            "x","y"
            """";


            var schemas = new[]
            {
                new CsvSchemaBuilder().From<Part1>().Schema,
                new CsvSchemaBuilder().From<Part2>().Schema
            };

            var reader = ReadCsv.FromString(input, emptyLineAction: EmptyLineAction.NextResult, hasHeaders: true, schema: schemas);

            var first = reader.AsEnumerable<Part1>().Single();
            Assert.AreEqual(new Part1(1, 2, 3), first);
            Assert.IsTrue(reader.NextResult());

            var second = reader.AsEnumerable<Part2>().Single();
            Assert.AreEqual(new Part2("x", "y"), second);
            Assert.IsFalse(reader.NextResult());
        }

        record Part1(int a, int b, int c);
        record Part2(string d, string e);
    }
}
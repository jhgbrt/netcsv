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

using NUnit.Framework.Legacy;

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
            Assert.That(
                new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber },
                Is.EqualTo(new { LineNumber = 3L, FieldNumber = 2, ColumnNumber = 6 })
                );
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
            Assert.That(new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber }, Is.EqualTo(new { LineNumber = 3L, FieldNumber = 3, ColumnNumber = 7 }));
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
            Assert.That(
                    new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber },
                    Is.EqualTo(new { LineNumber = 3L, FieldNumber = 2, ColumnNumber = 6 }) 
                    );
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
            Assert.That(
                new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber }, 
                Is.EqualTo(
                new { LineNumber = 3L, FieldNumber = 3, ColumnNumber = 7 }
                ));
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
            Assert.That(new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber }, Is.EqualTo(new { LineNumber = 1L, FieldNumber = 1, ColumnNumber = 13 }));
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
            Assert.That(new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber }, Is.EqualTo(new { LineNumber = 2L, FieldNumber = 1, ColumnNumber = 13 }));
        }
    }

    [Test()]
    public void TrailingFields_Are_Ignored()
    {
        const string Data = "ORIGIN,DESTINATION\nPHL,FLL,kjhkj kjhkjh,eg,fhgf\nNYC,LAX";

        using var csv = ReadCsv.FromString(Data, hasHeaders: true);
        while (csv.Read())
        {
            Assert.That(csv.FieldCount, Is.EqualTo(2));
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
        Assert.That(csv.Read(), Is.True);

        Assert.That(csv[0], Is.EqualTo("19324"));
        Assert.That(csv[1], Is.EqualTo("FJEDER TIL 2-05-405"));
        Assert.That(csv[2], Is.EqualTo(""));
        Assert.That(csv[3], Is.EqualTo("14,50"));
        Assert.That(csv[4], Is.EqualTo("4027816193241"));

        Assert.That(csv.Read(), Is.False);
    }

    [Test]
    public void LastFieldEmptyFollowedByMissingFieldsOnNextRecord()
    {
        const string Data = "a,b,c,d,e"
            + "\na,b,c,d,"
            + "\na,b,";

        using var csv = ReadCsv.FromString(Data, missingFieldAction: MissingFieldAction.ReplaceByNull);
        var record = new string[5];

        Assert.That(csv.Read(), Is.True);
        csv.GetValues(record);
        Assert.That(record, Is.EqualTo(new string[] { "a", "b", "c", "d", "e" }));

        Assert.That(csv.Read(), Is.True);
        csv.GetValues(record);
        Assert.That(record, Is.EqualTo(new string[] { "a", "b", "c", "d", "" }));

        Assert.That(csv.Read(), Is.True);
        csv.GetValues(record);
        Assert.That(record, Is.EqualTo(new string[] { "a", "b", "", null, null }));

        Assert.That(csv.Read(), Is.False);
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

            Assert.That(orders.Count, Is.EqualTo(2));
            Assert.That(items.Count, Is.EqualTo(4));
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
                Assert.That(reader["a"], Is.EqualTo("1"));
                Assert.That(reader["b"], Is.EqualTo("2"));
                Assert.That(reader["c"], Is.EqualTo("3"));
            }

            Assert.That(reader.NextResult(), Is.True);

            while (reader.Read())
            {
                Assert.That(reader["d"], Is.EqualTo("x"));
                Assert.That(reader["e"], Is.EqualTo("y"));
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
            Assert.That(first, Is.EqualTo(new Part1(1, 2, 3)));
            Assert.That(reader.NextResult(), Is.True);

            var second = reader.AsEnumerable<Part2>().Single();
            Assert.That(second, Is.EqualTo(new Part2("x", "y")));
            Assert.That(reader.NextResult(), Is.False);
        }

        record Part1(int a, int b, int c);
        record Part2(string d, string e);
    }
}
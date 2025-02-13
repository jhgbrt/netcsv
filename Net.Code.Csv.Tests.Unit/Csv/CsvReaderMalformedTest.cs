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

using Xunit;

using System.Diagnostics;

namespace Net.Code.Csv.Tests.Unit.IO.Csv;


public class CsvReaderMalformedTest
{

    [Fact]
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
            Assert.Equal(new { LineNumber = 3L, FieldNumber = 2, ColumnNumber = 6 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Fact]
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
            Assert.Equal(new { LineNumber = 3L, FieldNumber = 3, ColumnNumber = 7 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Fact]
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
            Assert.Equal(new { LineNumber = 3L, FieldNumber = 2, ColumnNumber = 6 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Fact]
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
            Assert.Equal(new { LineNumber = 3L, FieldNumber = 3, ColumnNumber = 7 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Fact]
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
            Assert.Equal(new { LineNumber = 1L, FieldNumber = 1, ColumnNumber = 13 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Fact]
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
            Assert.Equal(new { LineNumber = 2L, FieldNumber = 1, ColumnNumber = 13 }, new { ex.LineNumber, ex.FieldNumber, ex.ColumnNumber });
        }
    }

    [Fact]
    public void TrailingFields_Are_Ignored()
    {
        const string Data = "ORIGIN,DESTINATION\nPHL,FLL,kjhkj kjhkjh,eg,fhgf\nNYC,LAX";

        using var csv = ReadCsv.FromString(Data, hasHeaders: true);
        while (csv.Read())
        {
            Assert.Equal(2, csv.FieldCount);
            for (int i = 0; i < csv.FieldCount; i++)
            {
                _ = csv.GetString(i);
            }
        }
    }

    [Fact]
    public void ParseErrorBeforeInitializeTest()
    {
        const string Data = "\"0022 - SKABELON\";\"\"Tandremstrammer\";\"\";\"0,00\";\"\"\n" +
                            "\"15907\";\"\"BOLT TIL 2-05-405\";\"\";\"42,50\";\"4027816159070\"\n" +
                            "\"19324\";\"FJEDER TIL 2-05-405\";\"\";\"14,50\";\"4027816193241\"";

        using var csv = ReadCsv.FromString(Data, delimiter: ';', quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.AdvanceToNextLine);
        Assert.True(csv.Read());

        Assert.Equal("19324", csv[0]);
        Assert.Equal("FJEDER TIL 2-05-405", csv[1]);
        Assert.Equal("", csv[2]);
        Assert.Equal("14,50", csv[3]);
        Assert.Equal("4027816193241", csv[4]);

        Assert.False(csv.Read());
    }

    [Fact]
    public void LastFieldEmptyFollowedByMissingFieldsOnNextRecord()
    {
        const string Data = "a,b,c,d,e"
            + "\na,b,c,d,"
            + "\na,b,";

        using var csv = ReadCsv.FromString(Data, missingFieldAction: MissingFieldAction.ReplaceByNull);
        var record = new string[5];

        Assert.True(csv.Read());
        csv.GetValues(record);
        Assert.Equal(new string[] { "a", "b", "c", "d", "e" }, record);

        Assert.True(csv.Read());
        csv.GetValues(record);
        Assert.Equal(new string[] { "a", "b", "c", "d", "" }, record);

        Assert.True(csv.Read());
        csv.GetValues(record);
        Assert.Equal(new string[] { "a", "b", "", null, null }, record);

        Assert.False(csv.Read());
    }
}

public class MultiResultSetTests
{
    
    public class CsvMultiResultSetTest
    {
        [Fact]
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

            (int Id, string FirstName, string LastName, DateTime OrderDate, decimal OrderTotal)[] expected1 =
                [
                    (1, "John", "Doe", new DateTime(2023,1,1), 100m),
                    (2, "Jane", "Doe", new DateTime(2023,1,2), 110m),
                ];

            int i = 0;
            var reader = ReadCsv.FromString(input, emptyLineAction: EmptyLineAction.NextResult, hasHeaders: true, delimiter: ';');
            while (reader.Read())
            {
                var orderId = reader.GetInt32(reader.GetOrdinal("Id"));
                var customerFirstName = reader.GetString(reader.GetOrdinal("FirstName"));
                var customerLastName = reader.GetString(reader.GetOrdinal("LastName"));
                var orderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"));
                var orderTotal = reader.GetDecimal(reader.GetOrdinal("OrderTotal"));
                Assert.Equal((orderId, customerFirstName, customerLastName, orderDate, orderTotal), expected1[i]);
                i++;
            }

            (int Id, string ItemName, int Quantity, decimal Price)[] expected2 =
                [
                    (1, "Item 1", 2, 50m),
                    (2, "Item 1", 1, 50m),
                    (2, "Item 2", 1, 10m),
                    (2, "Item 2", 1, 50m),
                ];

            i = 0;
            reader.NextResult();
            while (reader.Read())
            {
                var orderId = reader.GetInt32(reader.GetOrdinal("OrderId"));
                var itemName = reader.GetString(reader.GetOrdinal("ItemName"));
                var quantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
                var price = reader.GetDecimal(reader.GetOrdinal("Price"));
                Assert.Equal((orderId, itemName, quantity, price), expected2[i]);
                i++;
            }



        }
        [Fact]
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

            Order[] expectedOrders = [
                new (1, "John", "Doe", new DateTime(2023,1,1), 100m),
                new (2, "Jane", "Doe", new DateTime(2023,1,2), 110m)
            ];
            var orders = reader.AsEnumerable<Order>().ToList();
            Assert.Equal(orders, expectedOrders);

            reader.NextResult();

            OrderItem[] expectedItems = [
                new (1, "Item 1", 2, 50m),
                new (2, "Item 1", 1, 50m),
                new (2, "Item 2", 1, 10m),
                new (2, "Item 2", 1, 50m)
                ];
            var items = reader.AsEnumerable<OrderItem>().ToList();

            Assert.Equal(2, orders.Count);
            Assert.Equal(4, items.Count);
        }

        record Order(int Id, string FirstName, string LastName, DateTime OrderDate, decimal OrderTotal);
        record OrderItem(int OrderId, string ItemName, int Quantity, decimal Price);

        [Fact]
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
                Assert.Equal("1", reader["a"]);
                Assert.Equal("2", reader["b"]);
                Assert.Equal("3", reader["c"]);
            }

            Assert.True(reader.NextResult());

            while (reader.Read())
            {
                Assert.Equal("x", reader["d"]);
                Assert.Equal("y", reader["e"]);
            }
        }
        [Fact]
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
            Assert.Equal(new Part1(1, 2, 3), first);
            Assert.True(reader.NextResult());

            var second = reader.AsEnumerable<Part2>().Single();
            Assert.Equal(new Part2("x", "y"), second);
            Assert.False(reader.NextResult());
        }

        record Part1(int a, int b, int c);
        record Part2(string d, string e);
    }
}
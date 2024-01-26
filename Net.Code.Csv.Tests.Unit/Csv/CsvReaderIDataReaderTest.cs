//	LumenWorks.Framework.Tests.Unit.IO.CSV.CsvReaderIDataReaderTest
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

using System.Data;

namespace Net.Code.Csv.Tests.Unit.IO.Csv;


public class CsvReaderIDataReaderTest
{
    #region IDataReader interface

    [Fact]
    public void IsClosed_WhenCloseWasCalled_ReturnsTrue()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema
            );

        reader.Read();
        reader.Close();
        Assert.True(reader.IsClosed);
    }

    [Fact]
    public void GetSchemaTableWithHeadersTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        
        DataTable schema = reader.GetSchemaTable();

        Assert.Equal(CsvReaderSampleData.SampleData1RecordCount, schema.Rows.Count);

        foreach (DataColumn column in schema.Columns)
        {
            Assert.True(column.ReadOnly);
        }

        for (int index = 0; index < schema.Rows.Count; index++)
        {
            DataRow column = schema.Rows[index];

            Assert.Equal(int.MaxValue, column["ColumnSize"]);
            Assert.Equal(DBNull.Value, column["NumericPrecision"]);
            Assert.Equal(DBNull.Value, column["NumericScale"]);
            Assert.Equal(false, column["IsUnique"]);
            Assert.Equal(false, column["IsKey"]);
            Assert.Equal(DBNull.Value, column["BaseServerName"]);
            Assert.Equal(DBNull.Value, column["BaseCatalogName"]);
            Assert.Equal(DBNull.Value, column["BaseSchemaName"]);
            Assert.Equal(DBNull.Value, column["BaseTableName"]);
            Assert.Equal(typeof(string), column["DataType"]);
            Assert.Equal(true, column["AllowDBNull"]);
            Assert.Equal(0, column["ProviderType"]);
            Assert.Equal(false, column["IsAliased"]);
            Assert.Equal(false, column["IsExpression"]);
            Assert.Equal(false, column["IsAutoIncrement"]);
            Assert.Equal(false, column["IsRowVersion"]);
            Assert.Equal(false, column["IsHidden"]);
            Assert.Equal(false, column["IsLong"]);
            Assert.Equal(true, column["IsReadOnly"]);

            Assert.Equal(index, column["ColumnOrdinal"]);

            Assert.Equal(CsvReaderSampleData.SampleData1Headers[index], column["ColumnName"]);
            Assert.Equal(DBNull.Value, column["BaseColumnName"]);
        }
    }
    [Fact]
    public void GetSchemaTable_WithSchema_RetrievesCorrectlyTypedValues()
    {
        using IDataReader csv = ReadCsv.FromString(CsvReaderSampleData.SampleTypedData1,
            hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        csv.Read();
        Assert.Equal(true, csv["System.Boolean"]);
        Assert.Equal(new DateTime(2001, 11, 15), csv["System.DateTime"]);
        Assert.Equivalent(1, csv["System.Single"]);
        Assert.Equivalent(1.1, csv["System.Double"]);
        Assert.Equivalent(1.10, csv["System.Decimal"]);
        Assert.Equivalent(1, csv["System.Int16"]);
        Assert.Equivalent(1, csv["System.Int32"]);
        Assert.Equivalent(1, csv["System.Int64"]);
        Assert.Equivalent(1, csv["System.UInt16"]);
        Assert.Equivalent(1, csv["System.UInt32"]);
        Assert.Equivalent(1L, csv["System.UInt64"]);
        Assert.Equivalent(1, csv["System.Byte"]);
        Assert.Equivalent(1, csv["System.SByte"]);
        Assert.Equal('a', csv["System.Char"]);
        Assert.Equal("abc", csv["System.String"]);
        Assert.Equal(Guid.Parse("{11111111-1111-1111-1111-111111111111}"), csv["System.Guid"]);
        Assert.Null(csv["System.DBNull"]);

    }
    [Fact]
    public void GetSchemaTable_WithSchema_SetsType()
    {
        using IDataReader csv = ReadCsv.FromString(CsvReaderSampleData.SampleTypedData1,
            hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        IDataReader reader = csv;
        DataTable schema = reader.GetSchemaTable();
        foreach (DataColumn column in schema.Columns)
        {
            Assert.True(column.ReadOnly);
        }

        for (int index = 0; index < schema.Rows.Count; index++)
        {
            DataRow column = schema.Rows[index];
            var schemaColumn = CsvReaderSampleData.SampleTypedData1Schema[index];

            Assert.Equal(schemaColumn.Name, column["ColumnName"]);
            Assert.Equal(int.MaxValue, column["ColumnSize"]);
            Assert.Equal(DBNull.Value, column["NumericPrecision"]);
            Assert.Equal(DBNull.Value, column["NumericScale"]);
            Assert.Equal(false, column["IsUnique"]);
            Assert.Equal(false, column["IsKey"]);
            Assert.Equal(DBNull.Value, column["BaseServerName"]);
            Assert.Equal(DBNull.Value, column["BaseCatalogName"]);
            Assert.Equal(DBNull.Value, column["BaseSchemaName"]);
            Assert.Equal(DBNull.Value, column["BaseTableName"]);
            Assert.Equal(schemaColumn.Type, column["DataType"]);
            Assert.Equal(schemaColumn.AllowNull, column["AllowDBNull"]);
            Assert.Equal(0, column["ProviderType"]);
            Assert.Equal(false, column["IsAliased"]);
            Assert.Equal(false, column["IsExpression"]);
            Assert.Equal(false, column["IsAutoIncrement"]);
            Assert.Equal(false, column["IsRowVersion"]);
            Assert.Equal(false, column["IsHidden"]);
            Assert.Equal(false, column["IsLong"]);
            Assert.Equal(true, column["IsReadOnly"]);

            Assert.Equal(index, column["ColumnOrdinal"]);

        }

    }

    [Fact]
    public void GetSchemaTableWithoutHeadersTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: false);
        DataTable schema = reader.GetSchemaTable();

        Assert.Equal(CsvReaderSampleData.SampleData1RecordCount, schema.Rows.Count);

        foreach (DataColumn column in schema.Columns)
        {
            Assert.True(column.ReadOnly);
        }

        for (int index = 0; index < schema.Rows.Count; index++)
        {
            DataRow column = schema.Rows[index];

            Assert.Equal(int.MaxValue, column["ColumnSize"]);
            Assert.Equal(DBNull.Value, column["NumericPrecision"]);
            Assert.Equal(DBNull.Value, column["NumericScale"]);
            Assert.Equal(false, column["IsUnique"]);
            Assert.Equal(false, column["IsKey"]);
            Assert.Equal(DBNull.Value, column["BaseServerName"]);
            Assert.Equal(DBNull.Value, column["BaseCatalogName"]);
            Assert.Equal(DBNull.Value, column["BaseSchemaName"]);
            Assert.Equal(DBNull.Value, column["BaseTableName"]);
            Assert.Equal(typeof(string), column["DataType"]);
            Assert.Equal(true, column["AllowDBNull"]);
            Assert.Equal(0, column["ProviderType"]);
            Assert.Equal(false, column["IsAliased"]);
            Assert.Equal(false, column["IsExpression"]);
            Assert.Equal(false, column["IsAutoIncrement"]);
            Assert.Equal(false, column["IsRowVersion"]);
            Assert.Equal(false, column["IsHidden"]);
            Assert.Equal(false, column["IsLong"]);
            Assert.Equal(true, column["IsReadOnly"]);

            Assert.Equal(index, column["ColumnOrdinal"]);

            Assert.Equal("Column" + index.ToString(CultureInfo.InvariantCulture), column["ColumnName"]);
            Assert.Equal(DBNull.Value, column["BaseColumnName"]);
        }
    }

    [Fact]
    public void NextResult_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        Assert.False(reader.NextResult());
    }

    [Fact]
    public void NextResult_WhenClosed_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        reader.Close();
        Assert.False(reader.NextResult());
    }

    [Fact]
    public void Read_RecordsAvailable_ReturnsTrue()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        for (int i = 0; i < CsvReaderSampleData.SampleData1RecordCount; i++)
            Assert.True(reader.Read());
    }
    [Fact]
    public void Read_NoMoreRecords_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        for (int i = 0; i < CsvReaderSampleData.SampleData1RecordCount; i++)
            reader.Read();

        Assert.False(reader.Read());
    }

    [Fact]
    public void Read_WhenClosed_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        reader.Close();
        Assert.False(reader.Read());
    }

    [Fact]
    public void Depth_IsAlwaysZero()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        Assert.Equal(0, reader.Depth);
        reader.Read();
        Assert.Equal(0, reader.Depth);
    }

    [Fact]
    public void Depth_WhenClosed_ReturnsZero()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        reader.Close();
        Assert.Equal(0, reader.Depth);
    }

    [Fact]
    public void Closed_WhenClosed_IsTrue()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        Assert.False(reader.IsClosed);

        reader.Read();
        Assert.False(reader.IsClosed);

        reader.Close();
        Assert.True(reader.IsClosed);
    }

    [Fact]
    public void RecordsAffected_IsAlwaysMinusOne()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        Assert.Equal(-1, reader.RecordsAffected);

        reader.Read();
        Assert.Equal(-1, reader.RecordsAffected);

        reader.Close();
        Assert.Equal(-1, reader.RecordsAffected);
    }

    #endregion

    #region IDataRecord interface

    [Fact]
    public void GetBoolean_WhenCalled_ReturnsBoolean()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Boolean value = true;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetBoolean(reader.GetOrdinal(typeof(Boolean).FullName)));
        }
    }

    [Fact]
    public void GetByte_WhenCalled_ReturnsByte()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Byte value = 1;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetByte(reader.GetOrdinal(typeof(Byte).FullName)));
        }
    }

    [Fact]
    public void GetBytesTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Console.WriteLine(Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        var expected = Convert.FromBase64String("AQIDBAUGBwgJ");
        while (reader.Read())
        {
            Byte[] buffer = new Byte[16];

            long count = reader.GetBytes(reader.GetOrdinal(typeof(System.Byte[]).FullName), 0, buffer, 0, buffer.Length);
            Assert.Equal(expected.Length, count);
            Assert.Equal(expected.Take((int)count), buffer.Take((int)count));
        }
    }

    [Fact]
    public void GetChar_WhenCalled_ReturnsChar()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Char value = 'a';
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetChar(reader.GetOrdinal(typeof(Char).FullName)));
        }
    }

    [Fact]
    public void GetCharsTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Char[] value = "abc".ToCharArray();
        while (reader.Read())
        {
            Char[] csvValue = new Char[value.Length];

            long count = reader.GetChars(reader.GetOrdinal(typeof(String).FullName), 0, csvValue, 0, value.Length);

            Assert.Equal(value.Length, count);
            Assert.Equal(value.Length, csvValue.Length);

            for (int i = 0; i < value.Length; i++)
                Assert.Equal(value[i], csvValue[i]);
        }
    }

    [Fact]
    public void GetDataTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        while (reader.Read())
        {
            Assert.Equal(reader, reader.GetData(0));

            for (int i = 1; i < reader.FieldCount; i++)
                Assert.Null(reader.GetData(i));
        }
    }

    [Fact]
    public void GetDataTypeNameTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
                Assert.Equal(CsvReaderSampleData.SampleTypedData1Schema[i].Type.FullName, reader.GetDataTypeName(i));
        }
    }

    [Fact]
    public void GetDateTimeTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        DateTime value = new(2001, 11, 15);
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetDateTime(reader.GetOrdinal(typeof(DateTime).FullName)));
        }
    }

    [Fact]
    public void GetDecimalTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Decimal value = 1.10m;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetDecimal(reader.GetOrdinal(typeof(Decimal).FullName)));
        }
    }

    [Fact]
    public void GetDoubleTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Double value = 1.1d;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetDouble(reader.GetOrdinal(typeof(Double).FullName)));
        }
    }
    [Fact]
    public void GetFloatTest1()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Single value = 1f;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetFloat(reader.GetOrdinal(typeof(Single).FullName)));
        }
    }

    [Fact]
    public void GetFieldTypeTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
                Assert.Equal(CsvReaderSampleData.SampleTypedData1Schema[i].Type, reader.GetFieldType(i));
        }
    }

    [Fact]
    public void GetFloatTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Single value = 1;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetFloat(reader.GetOrdinal(typeof(Single).FullName)));
        }
    }

    [Fact]
    public void GetGuidTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        var value = new Guid("{11111111-1111-1111-1111-111111111111}");
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetGuid(reader.GetOrdinal(typeof(Guid).FullName)));
        }
    }

    [Fact]
    public void GetInt16Test()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        Int16 value = 1;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetInt16(reader.GetOrdinal(typeof(Int16).FullName)));
        }
    }

    [Fact]
    public void GetInt32Test()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        Int32 value = 1;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetInt32(reader.GetOrdinal(typeof(Int32).FullName)));
        }
    }

    [Fact]
    public void GetInt64Test()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        Int64 value = 1;
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetInt64(reader.GetOrdinal(typeof(Int64).FullName)));
        }
    }

    [Fact]
    public void GetNameTest_WithSchema()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema);

        while (reader.Read())
        {
            Assert.Equal(CsvReaderSampleData.SampleData1Header0, reader.GetName(0));
            Assert.Equal(CsvReaderSampleData.SampleData1Header1, reader.GetName(1));
            Assert.Equal(CsvReaderSampleData.SampleData1Header2, reader.GetName(2));
            Assert.Equal(CsvReaderSampleData.SampleData1Header3, reader.GetName(3));
            Assert.Equal(CsvReaderSampleData.SampleData1Header4, reader.GetName(4));
            Assert.Equal(CsvReaderSampleData.SampleData1Header5, reader.GetName(5));
        }
    }

    [Fact]
    public void GetOrdinalTest_WithSchema()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema);

        while (reader.Read())
        {
            Assert.Equal(0, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header0));
            Assert.Equal(1, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header1));
            Assert.Equal(2, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header2));
            Assert.Equal(3, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header3));
            Assert.Equal(4, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header4));
            Assert.Equal(5, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header5));
        }
    }


    [Fact]
    public void GetNameTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);

        while (reader.Read())
        {
            Assert.Equal(CsvReaderSampleData.SampleData1Header0, reader.GetName(0));
            Assert.Equal(CsvReaderSampleData.SampleData1Header1, reader.GetName(1));
            Assert.Equal(CsvReaderSampleData.SampleData1Header2, reader.GetName(2));
            Assert.Equal(CsvReaderSampleData.SampleData1Header3, reader.GetName(3));
            Assert.Equal(CsvReaderSampleData.SampleData1Header4, reader.GetName(4));
            Assert.Equal(CsvReaderSampleData.SampleData1Header5, reader.GetName(5));
        }
    }

    [Fact]
    public void GetOrdinalTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);

        while (reader.Read())
        {
            Assert.Equal(0, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header0));
            Assert.Equal(1, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header1));
            Assert.Equal(2, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header2));
            Assert.Equal(3, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header3));
            Assert.Equal(4, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header4));
            Assert.Equal(5, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header5));
        }
    }

    [Fact]
    public void GetStringTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        String value = "abc";
        while (reader.Read())
        {
            Assert.Equal(value, reader.GetString(reader.GetOrdinal(typeof(String).FullName)));
        }
    }

    [Fact]
    public void GetValueTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        string[] values = new string[CsvReaderSampleData.SampleData1RecordCount];

        var currentRecordIndex = 0;
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object value = reader.GetValue(i);
                values[i] = value?.ToString();
            }

            CsvReaderSampleData.CheckSampleData1(true, currentRecordIndex, values);
            currentRecordIndex++;
        }
    }

    [Fact]
    public void GetValuesTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);

        object[] objValues = new object[CsvReaderSampleData.SampleData1RecordCount];
        string[] values = new string[CsvReaderSampleData.SampleData1RecordCount];

        int currentRecordIndex = 0;
        while (reader.Read())
        {
            Assert.Equal(CsvReaderSampleData.SampleData1RecordCount, reader.GetValues(objValues));

            for (int i = 0; i < reader.FieldCount; i++)
            {
                values[i] = objValues[i]?.ToString();
            }

            CsvReaderSampleData.CheckSampleData1(true, currentRecordIndex, values);
            currentRecordIndex++;
        }
    }

    [Fact]
    public void IsDBNullTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        while (reader.Read())
        {
            Assert.True(reader.IsDBNull(reader.GetOrdinal(typeof(DBNull).FullName)));
            Assert.Null(reader[reader.GetOrdinal(typeof(DBNull).FullName)]);
        }
    }

    [Fact]
    public void FieldCountTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema);
        Assert.Equal(CsvReaderSampleData.SampleData1RecordCount, reader.FieldCount);
    }

    [Fact]
    public void IndexerByFieldNameTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        string[] values = new string[CsvReaderSampleData.SampleData1RecordCount];
        int currentIndex = 0;
        while (reader.Read())
        {
            values[0] = (string)reader[CsvReaderSampleData.SampleData1Header0];
            values[1] = (string)reader[CsvReaderSampleData.SampleData1Header1];
            values[2] = (string)reader[CsvReaderSampleData.SampleData1Header2];
            values[3] = (string)reader[CsvReaderSampleData.SampleData1Header3];
            values[4] = (string)reader[CsvReaderSampleData.SampleData1Header4];
            values[5] = (string)reader[CsvReaderSampleData.SampleData1Header5];

            CsvReaderSampleData.CheckSampleData1(true, currentIndex, values, true);
            currentIndex++;
        }
    }

    [Fact]
    public void IndexerByFieldIndexTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        string[] values = new string[CsvReaderSampleData.SampleData1RecordCount];
        int currentRecordIndex = 0;
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
                values[i] = (string)reader[i];

            CsvReaderSampleData.CheckSampleData1(true, currentRecordIndex, values);
            currentRecordIndex++;
        }
    }

    #endregion
}

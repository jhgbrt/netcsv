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

[TestFixture()]
public class CsvReaderIDataReaderTest
{
    #region IDataReader interface

    [Test()]
    public void IsClosed_WhenCloseWasCalled_ReturnsTrue()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema
            );

        reader.Read();
        reader.Close();
        Assert.That(reader.IsClosed, Is.True);
    }

    [Test()]
    public void GetSchemaTableWithHeadersTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);
        
        DataTable schema = reader.GetSchemaTable();

        Assert.That(schema.Rows.Count, Is.EqualTo(CsvReaderSampleData.SampleData1RecordCount));

        foreach (DataColumn column in schema.Columns)
        {
            Assert.That(column.ReadOnly, Is.True);
        }

        for (int index = 0; index < schema.Rows.Count; index++)
        {
            DataRow column = schema.Rows[index];

            Assert.That(column["ColumnSize"], Is.EqualTo(int.MaxValue));
            Assert.That(column["NumericPrecision"], Is.EqualTo(DBNull.Value));
            Assert.That(column["NumericScale"], Is.EqualTo(DBNull.Value));
            Assert.That(column["IsUnique"], Is.EqualTo(false));
            Assert.That(column["IsKey"], Is.EqualTo(false));
            Assert.That(column["BaseServerName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseCatalogName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseSchemaName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseTableName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["DataType"], Is.EqualTo(typeof(string)));
            Assert.That(column["AllowDBNull"], Is.EqualTo(true));
            Assert.That(column["ProviderType"], Is.EqualTo(0));
            Assert.That(column["IsAliased"], Is.EqualTo(false));
            Assert.That(column["IsExpression"], Is.EqualTo(false));
            Assert.That(column["IsAutoIncrement"], Is.EqualTo(false));
            Assert.That(column["IsRowVersion"], Is.EqualTo(false));
            Assert.That(column["IsHidden"], Is.EqualTo(false));
            Assert.That(column["IsLong"], Is.EqualTo(false));
            Assert.That(column["IsReadOnly"], Is.EqualTo(true));

            Assert.That(column["ColumnOrdinal"], Is.EqualTo(index));

            Assert.That(column["ColumnName"], Is.EqualTo(CsvReaderSampleData.SampleData1Headers[index]));
            Assert.That(column["BaseColumnName"], Is.EqualTo(DBNull.Value));
        }
    }
    [Test]
    public void GetSchemaTable_WithSchema_RetrievesCorrectlyTypedValues()
    {
        using IDataReader csv = ReadCsv.FromString(CsvReaderSampleData.SampleTypedData1,
            hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        csv.Read();
        Assert.That(csv["System.Boolean"], Is.EqualTo(true));
        Assert.That(csv["System.DateTime"], Is.EqualTo(new DateTime(2001, 11, 15)));
        Assert.That(csv["System.Single"], Is.EqualTo(1));
        Assert.That(csv["System.Double"], Is.EqualTo(1.1));
        Assert.That(csv["System.Decimal"], Is.EqualTo(1.10d));
        Assert.That(csv["System.Int16"], Is.EqualTo(1));
        Assert.That(csv["System.Int32"], Is.EqualTo(1));
        Assert.That(csv["System.Int64"], Is.EqualTo(1));
        Assert.That(csv["System.UInt16"], Is.EqualTo(1));
        Assert.That(csv["System.UInt32"], Is.EqualTo(1));
        Assert.That(csv["System.UInt64"], Is.EqualTo(1));
        Assert.That(csv["System.Byte"], Is.EqualTo(1));
        Assert.That(csv["System.SByte"], Is.EqualTo(1));
        Assert.That(csv["System.Char"], Is.EqualTo('a'));
        Assert.That(csv["System.String"], Is.EqualTo("abc"));
        Assert.That(csv["System.Guid"], Is.EqualTo(Guid.Parse("{11111111-1111-1111-1111-111111111111}")));
        Assert.That(csv["System.DBNull"], Is.EqualTo(null));

    }
    [Test]
    public void GetSchemaTable_WithSchema_SetsType()
    {
        using IDataReader csv = ReadCsv.FromString(CsvReaderSampleData.SampleTypedData1,
            hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        IDataReader reader = csv;
        DataTable schema = reader.GetSchemaTable();
        foreach (DataColumn column in schema.Columns)
        {
            Assert.That(column.ReadOnly, Is.True);
        }

        for (int index = 0; index < schema.Rows.Count; index++)
        {
            DataRow column = schema.Rows[index];
            var schemaColumn = CsvReaderSampleData.SampleTypedData1Schema[index];

            Assert.That(column["ColumnName"], Is.EqualTo(schemaColumn.Name));
            Assert.That(column["ColumnSize"], Is.EqualTo(int.MaxValue));
            Assert.That(column["NumericPrecision"], Is.EqualTo(DBNull.Value));
            Assert.That(column["NumericScale"], Is.EqualTo(DBNull.Value));
            Assert.That(column["IsUnique"], Is.EqualTo(false));
            Assert.That(column["IsKey"], Is.EqualTo(false));
            Assert.That(column["BaseServerName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseCatalogName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseSchemaName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseTableName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["DataType"], Is.EqualTo(schemaColumn.Type));
            Assert.That(column["AllowDBNull"], Is.EqualTo(schemaColumn.AllowNull));
            Assert.That(column["ProviderType"], Is.EqualTo(0));
            Assert.That(column["IsAliased"], Is.EqualTo(false));
            Assert.That(column["IsExpression"], Is.EqualTo(false));
            Assert.That(column["IsAutoIncrement"], Is.EqualTo(false));
            Assert.That(column["IsRowVersion"], Is.EqualTo(false));
            Assert.That(column["IsHidden"], Is.EqualTo(false));
            Assert.That(column["IsLong"], Is.EqualTo(false));
            Assert.That(column["IsReadOnly"], Is.EqualTo(true));

            Assert.That(column["ColumnOrdinal"], Is.EqualTo(index));

        }

    }

    [Test()]
    public void GetSchemaTableWithoutHeadersTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: false);
        DataTable schema = reader.GetSchemaTable();

        Assert.That(schema.Rows.Count, Is.EqualTo(CsvReaderSampleData.SampleData1RecordCount));

        foreach (DataColumn column in schema.Columns)
        {
            Assert.That(column.ReadOnly, Is.True);
        }

        for (int index = 0; index < schema.Rows.Count; index++)
        {
            DataRow column = schema.Rows[index];

            Assert.That(column["ColumnSize"], Is.EqualTo(int.MaxValue));
            Assert.That(column["NumericPrecision"], Is.EqualTo(DBNull.Value));
            Assert.That(column["NumericScale"], Is.EqualTo(DBNull.Value));
            Assert.That(column["IsUnique"], Is.EqualTo(false));
            Assert.That(column["IsKey"], Is.EqualTo(false));
            Assert.That(column["BaseServerName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseCatalogName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseSchemaName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["BaseTableName"], Is.EqualTo(DBNull.Value));
            Assert.That(column["DataType"], Is.EqualTo(typeof(string)));
            Assert.That(column["AllowDBNull"], Is.EqualTo(true));
            Assert.That(column["ProviderType"], Is.EqualTo(0));
            Assert.That(column["IsAliased"], Is.EqualTo(false));
            Assert.That(column["IsExpression"], Is.EqualTo(false));
            Assert.That(column["IsAutoIncrement"], Is.EqualTo(false));
            Assert.That(column["IsRowVersion"], Is.EqualTo(false));
            Assert.That(column["IsHidden"], Is.EqualTo(false));
            Assert.That(column["IsLong"], Is.EqualTo(false));
            Assert.That(column["IsReadOnly"], Is.EqualTo(true));

            Assert.That(column["ColumnOrdinal"], Is.EqualTo(index));

            Assert.That(column["ColumnName"], Is.EqualTo("Column" + index.ToString(CultureInfo.InvariantCulture)));
            Assert.That(column["BaseColumnName"], Is.EqualTo(DBNull.Value));
        }
    }

    [Test()]
    public void NextResult_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        Assert.That(reader.NextResult(), Is.False);
    }

    [Test()]
    public void NextResult_WhenClosed_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        reader.Close();
        Assert.That(reader.NextResult(), Is.False);
    }

    [Test()]
    public void Read_RecordsAvailable_ReturnsTrue()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        for (int i = 0; i < CsvReaderSampleData.SampleData1RecordCount; i++)
            Assert.That(reader.Read(), Is.True);
    }
    [Test()]
    public void Read_NoMoreRecords_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        for (int i = 0; i < CsvReaderSampleData.SampleData1RecordCount; i++)
            reader.Read();

        Assert.That(reader.Read(), Is.False);
    }

    [Test()]
    public void Read_WhenClosed_ReturnsFalse()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        reader.Close();
        Assert.That(reader.Read(), Is.False);
    }

    [Test()]
    public void Depth_IsAlwaysZero()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        Assert.That(reader.Depth, Is.EqualTo(0));
        reader.Read();
        Assert.That(reader.Depth, Is.EqualTo(0));
    }

    [Test()]
    public void Depth_WhenClosed_ReturnsZero()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        reader.Read();
        reader.Close();
        Assert.That(reader.Depth, Is.EqualTo(0));
    }

    [Test()]
    public void Closed_WhenClosed_IsTrue()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        Assert.That(reader.IsClosed, Is.False);

        reader.Read();
        Assert.That(reader.IsClosed, Is.False);

        reader.Close();
        Assert.That(reader.IsClosed, Is.True);
    }

    [Test()]
    public void RecordsAffected_IsAlwaysMinusOne()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true);
        Assert.That(reader.RecordsAffected, Is.EqualTo(-1));

        reader.Read();
        Assert.That(reader.RecordsAffected, Is.EqualTo(-1));

        reader.Close();
        Assert.That(reader.RecordsAffected, Is.EqualTo(-1));
    }

    #endregion

    #region IDataRecord interface

    [Test()]
    public void GetBoolean_WhenCalled_ReturnsBoolean()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Boolean value = true;
        while (reader.Read())
        {
            Assert.That(reader.GetBoolean(reader.GetOrdinal(typeof(Boolean).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetByte_WhenCalled_ReturnsByte()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Byte value = 1;
        while (reader.Read())
        {
            Assert.That(reader.GetByte(reader.GetOrdinal(typeof(Byte).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
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
            Assert.That(count, Is.EqualTo(expected.Length));
            Assert.That(buffer.Take((int)count), Is.EqualTo(expected.Take((int)count)));
        }
    }

    [Test()]
    public void GetChar_WhenCalled_ReturnsChar()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Char value = 'a';
        while (reader.Read())
        {
            Assert.That(reader.GetChar(reader.GetOrdinal(typeof(Char).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetCharsTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Char[] value = "abc".ToCharArray();
        while (reader.Read())
        {
            Char[] csvValue = new Char[value.Length];

            long count = reader.GetChars(reader.GetOrdinal(typeof(String).FullName), 0, csvValue, 0, value.Length);

            Assert.That(count, Is.EqualTo(value.Length));
            Assert.That(csvValue.Length, Is.EqualTo(value.Length));

            for (int i = 0; i < value.Length; i++)
                Assert.That(csvValue[i], Is.EqualTo(value[i]));
        }
    }

    [Test()]
    public void GetDataTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        while (reader.Read())
        {
            Assert.That(reader.GetData(0), Is.SameAs(reader));

            for (int i = 1; i < reader.FieldCount; i++)
                Assert.That(reader.GetData(i), Is.Null);
        }
    }

    [Test()]
    public void GetDataTypeNameTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
                Assert.That(reader.GetDataTypeName(i), Is.EqualTo(CsvReaderSampleData.SampleTypedData1Schema[i].Type.FullName));
        }
    }

    [Test()]
    public void GetDateTimeTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        DateTime value = new(2001, 11, 15);
        while (reader.Read())
        {
            Assert.That(reader.GetDateTime(reader.GetOrdinal(typeof(DateTime).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetDecimalTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Decimal value = 1.10m;
        while (reader.Read())
        {
            Assert.That(reader.GetDecimal(reader.GetOrdinal(typeof(Decimal).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetDoubleTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Double value = 1.1d;
        while (reader.Read())
        {
            Assert.That(reader.GetDouble(reader.GetOrdinal(typeof(Double).FullName)), Is.EqualTo(value));
        }
    }
    [Test()]
    public void GetFloatTest1()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Single value = 1f;
        while (reader.Read())
        {
            Assert.That(reader.GetFloat(reader.GetOrdinal(typeof(Single).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetFieldTypeTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
                Assert.That(reader.GetFieldType(i), Is.EqualTo(CsvReaderSampleData.SampleTypedData1Schema[i].Type));
        }
    }

    [Test()]
    public void GetFloatTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        Single value = 1;
        while (reader.Read())
        {
            Assert.That(reader.GetFloat(reader.GetOrdinal(typeof(Single).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetGuidTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        var value = new Guid("{11111111-1111-1111-1111-111111111111}");
        while (reader.Read())
        {
            Assert.That(reader.GetGuid(reader.GetOrdinal(typeof(Guid).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetInt16Test()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        Int16 value = 1;
        while (reader.Read())
        {
            Assert.That(reader.GetInt16(reader.GetOrdinal(typeof(Int16).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetInt32Test()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        Int32 value = 1;
        while (reader.Read())
        {
            Assert.That(reader.GetInt32(reader.GetOrdinal(typeof(Int32).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetInt64Test()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        Int64 value = 1;
        while (reader.Read())
        {
            Assert.That(reader.GetInt64(reader.GetOrdinal(typeof(Int64).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
    public void GetNameTest_WithSchema()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema);

        while (reader.Read())
        {
            Assert.That(reader.GetName(0), Is.EqualTo(CsvReaderSampleData.SampleData1Header0));
            Assert.That(reader.GetName(1), Is.EqualTo(CsvReaderSampleData.SampleData1Header1));
            Assert.That(reader.GetName(2), Is.EqualTo(CsvReaderSampleData.SampleData1Header2));
            Assert.That(reader.GetName(3), Is.EqualTo(CsvReaderSampleData.SampleData1Header3));
            Assert.That(reader.GetName(4), Is.EqualTo(CsvReaderSampleData.SampleData1Header4));
            Assert.That(reader.GetName(5), Is.EqualTo(CsvReaderSampleData.SampleData1Header5));
        }
    }

    [Test()]
    public void GetOrdinalTest_WithSchema()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema);

        while (reader.Read())
        {
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header0), Is.EqualTo(0));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header1), Is.EqualTo(1));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header2), Is.EqualTo(2));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header3), Is.EqualTo(3));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header4), Is.EqualTo(4));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header5), Is.EqualTo(5));
        }
    }


    [Test()]
    public void GetNameTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);

        while (reader.Read())
        {
            Assert.That(reader.GetName(0), Is.EqualTo(CsvReaderSampleData.SampleData1Header0));
            Assert.That(reader.GetName(1), Is.EqualTo(CsvReaderSampleData.SampleData1Header1));
            Assert.That(reader.GetName(2), Is.EqualTo(CsvReaderSampleData.SampleData1Header2));
            Assert.That(reader.GetName(3), Is.EqualTo(CsvReaderSampleData.SampleData1Header3));
            Assert.That(reader.GetName(4), Is.EqualTo(CsvReaderSampleData.SampleData1Header4));
            Assert.That(reader.GetName(5), Is.EqualTo(CsvReaderSampleData.SampleData1Header5));
        }
    }

    [Test()]
    public void GetOrdinalTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);

        while (reader.Read())
        {
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header0), Is.EqualTo(0));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header1), Is.EqualTo(1));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header2), Is.EqualTo(2));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header3), Is.EqualTo(3));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header4), Is.EqualTo(4));
            Assert.That(reader.GetOrdinal(CsvReaderSampleData.SampleData1Header5), Is.EqualTo(5));
        }
    }

    [Test()]
    public void GetStringTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);

        String value = "abc";
        while (reader.Read())
        {
            Assert.That(reader.GetString(reader.GetOrdinal(typeof(String).FullName)), Is.EqualTo(value));
        }
    }

    [Test()]
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

    [Test()]
    public void GetValuesTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema, trimmingOptions: ValueTrimmingOptions.UnquotedOnly);

        object[] objValues = new object[CsvReaderSampleData.SampleData1RecordCount];
        string[] values = new string[CsvReaderSampleData.SampleData1RecordCount];

        int currentRecordIndex = 0;
        while (reader.Read())
        {
            Assert.That(reader.GetValues(objValues), Is.EqualTo(CsvReaderSampleData.SampleData1RecordCount));

            for (int i = 0; i < reader.FieldCount; i++)
            {
                values[i] = objValues[i]?.ToString();
            }

            CsvReaderSampleData.CheckSampleData1(true, currentRecordIndex, values);
            currentRecordIndex++;
        }
    }

    [Test()]
    public void IsDBNullTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema);
        while (reader.Read())
        {
            Assert.That(reader.IsDBNull(reader.GetOrdinal(typeof(DBNull).FullName)), Is.True);
            Assert.That(reader[reader.GetOrdinal(typeof(DBNull).FullName)], Is.Null);
        }
    }

    [Test()]
    public void FieldCountTest()
    {
        using var reader = ReadCsv.FromString(
            CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema);
        Assert.That(reader.FieldCount, Is.EqualTo(CsvReaderSampleData.SampleData1RecordCount));
    }

    [Test()]
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

    [Test()]
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

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

using NUnit.Framework;

using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Net.Code.Csv.Tests.Unit.IO.Csv
{
    [TestFixture()]
    public class CsvReaderIDataReaderTest
    {
        #region IDataReader interface

        [Test()]
        public void IsClosed_WhenCloseWasCalled_ReturnsTrue()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                reader.Read();

                reader.Close();

                Assert.IsTrue(reader.IsClosed);
            }
        }

        [Test()]
        public void GetSchemaTableWithHeadersTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                DataTable schema = reader.GetSchemaTable();

                Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount, schema.Rows.Count);

                foreach (DataColumn column in schema.Columns)
                {
                    Assert.IsTrue(column.ReadOnly);
                }

                for (int index = 0; index < schema.Rows.Count; index++)
                {
                    DataRow column = schema.Rows[index];

                    Assert.AreEqual(int.MaxValue, column["ColumnSize"]);
                    Assert.AreEqual(DBNull.Value, column["NumericPrecision"]);
                    Assert.AreEqual(DBNull.Value, column["NumericScale"]);
                    Assert.AreEqual(false, column["IsUnique"]);
                    Assert.AreEqual(false, column["IsKey"]);
                    Assert.AreEqual(DBNull.Value, column["BaseServerName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseCatalogName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseSchemaName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseTableName"]);
                    Assert.AreEqual(typeof(string), column["DataType"]);
                    Assert.AreEqual(true, column["AllowDBNull"]);
                    Assert.AreEqual(0, column["ProviderType"]);
                    Assert.AreEqual(false, column["IsAliased"]);
                    Assert.AreEqual(false, column["IsExpression"]);
                    Assert.AreEqual(false, column["IsAutoIncrement"]);
                    Assert.AreEqual(false, column["IsRowVersion"]);
                    Assert.AreEqual(false, column["IsHidden"]);
                    Assert.AreEqual(false, column["IsLong"]);
                    Assert.AreEqual(true, column["IsReadOnly"]);

                    Assert.AreEqual(index, column["ColumnOrdinal"]);

                    Assert.AreEqual(CsvReaderSampleData.SampleData1Headers[index], column["ColumnName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseColumnName"]);
                }
            }
        }
        [Test]
        public void GetSchemaTable_WithSchema_RetrievesCorrectlyTypedValues()
        {
            using (IDataReader csv = ReadCsv.FromString(CsvReaderSampleData.SampleTypedData1,
                hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema))
            {
                csv.Read();
                Assert.AreEqual(true, csv["System.Boolean"]);
                Assert.AreEqual(new DateTime(2001, 11, 15), csv["System.DateTime"]);
                Assert.AreEqual(1, csv["System.Single"]);
                Assert.AreEqual(1.1, csv["System.Double"]);
                Assert.AreEqual(1.10d, csv["System.Decimal"]);
                Assert.AreEqual(1, csv["System.Int16"]);
                Assert.AreEqual(1, csv["System.Int32"]);
                Assert.AreEqual(1, csv["System.Int64"]);
                Assert.AreEqual(1, csv["System.UInt16"]);
                Assert.AreEqual(1, csv["System.UInt32"]);
                Assert.AreEqual(1, csv["System.UInt64"]);
                Assert.AreEqual(1, csv["System.Byte"]);
                Assert.AreEqual(1, csv["System.SByte"]);
                Assert.AreEqual('a', csv["System.Char"]);
                Assert.AreEqual("abc", csv["System.String"]);
                Assert.AreEqual(Guid.Parse("{11111111-1111-1111-1111-111111111111}"), csv["System.Guid"]);
                Assert.AreEqual(null, csv["System.DBNull"]);
            }

        }
        [Test]
        public void GetSchemaTable_WithSchema_SetsType()
        {
            using (IDataReader csv = ReadCsv.FromString(CsvReaderSampleData.SampleTypedData1,
                hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema))
            {
                IDataReader reader = csv;
                DataTable schema = reader.GetSchemaTable();
                foreach (DataColumn column in schema.Columns)
                {
                    Assert.IsTrue(column.ReadOnly);
                }

                for (int index = 0; index < schema.Rows.Count; index++)
                {
                    DataRow column = schema.Rows[index];
                    var schemaColumn = CsvReaderSampleData.SampleTypedData1Schema[index];

                    Assert.AreEqual(schemaColumn.Name, column["ColumnName"]);
                    Assert.AreEqual(int.MaxValue, column["ColumnSize"]);
                    Assert.AreEqual(DBNull.Value, column["NumericPrecision"]);
                    Assert.AreEqual(DBNull.Value, column["NumericScale"]);
                    Assert.AreEqual(false, column["IsUnique"]);
                    Assert.AreEqual(false, column["IsKey"]);
                    Assert.AreEqual(DBNull.Value, column["BaseServerName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseCatalogName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseSchemaName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseTableName"]);
                    Assert.AreEqual(schemaColumn.Type, column["DataType"]);
                    Assert.AreEqual(schemaColumn.AllowNull, column["AllowDBNull"]);
                    Assert.AreEqual(0, column["ProviderType"]);
                    Assert.AreEqual(false, column["IsAliased"]);
                    Assert.AreEqual(false, column["IsExpression"]);
                    Assert.AreEqual(false, column["IsAutoIncrement"]);
                    Assert.AreEqual(false, column["IsRowVersion"]);
                    Assert.AreEqual(false, column["IsHidden"]);
                    Assert.AreEqual(false, column["IsLong"]);
                    Assert.AreEqual(true, column["IsReadOnly"]);

                    Assert.AreEqual(index, column["ColumnOrdinal"]);

                }
            }

        }

        [Test()]
        public void GetSchemaTableWithoutHeadersTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: false)
            )
            {
                DataTable schema = reader.GetSchemaTable();

                Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount, schema.Rows.Count);

                foreach (DataColumn column in schema.Columns)
                {
                    Assert.IsTrue(column.ReadOnly);
                }

                for (int index = 0; index < schema.Rows.Count; index++)
                {
                    DataRow column = schema.Rows[index];

                    Assert.AreEqual(int.MaxValue, column["ColumnSize"]);
                    Assert.AreEqual(DBNull.Value, column["NumericPrecision"]);
                    Assert.AreEqual(DBNull.Value, column["NumericScale"]);
                    Assert.AreEqual(false, column["IsUnique"]);
                    Assert.AreEqual(false, column["IsKey"]);
                    Assert.AreEqual(DBNull.Value, column["BaseServerName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseCatalogName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseSchemaName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseTableName"]);
                    Assert.AreEqual(typeof(string), column["DataType"]);
                    Assert.AreEqual(true, column["AllowDBNull"]);
                    Assert.AreEqual(0, column["ProviderType"]);
                    Assert.AreEqual(false, column["IsAliased"]);
                    Assert.AreEqual(false, column["IsExpression"]);
                    Assert.AreEqual(false, column["IsAutoIncrement"]);
                    Assert.AreEqual(false, column["IsRowVersion"]);
                    Assert.AreEqual(false, column["IsHidden"]);
                    Assert.AreEqual(false, column["IsLong"]);
                    Assert.AreEqual(true, column["IsReadOnly"]);

                    Assert.AreEqual(index, column["ColumnOrdinal"]);

                    Assert.AreEqual("Column" + index.ToString(CultureInfo.InvariantCulture), column["ColumnName"]);
                    Assert.AreEqual(DBNull.Value, column["BaseColumnName"]);
                }
            }
        }

        [Test()]
        public void NextResult_ReturnsFalse()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                Assert.IsFalse(reader.NextResult());

                reader.Read();
                Assert.IsFalse(reader.NextResult());
            }
        }

        [Test()]
        public void NextResult_WhenClosed_ReturnsFalse()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                reader.Read();
                reader.Close();
                Assert.IsFalse(reader.NextResult());
            }
        }

        [Test()]
        public void Read_RecordsAvailable_ReturnsTrue()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                for (int i = 0; i < CsvReaderSampleData.SampleData1RecordCount; i++)
                    Assert.IsTrue(reader.Read());
            }
        }
        [Test()]
        public void Read_NoMoreRecords_ReturnsFalse()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                for (int i = 0; i < CsvReaderSampleData.SampleData1RecordCount; i++)
                    reader.Read();

                Assert.IsFalse(reader.Read());
            }
        }

        [Test()]
        public void Read_WhenClosed_ReturnsFalse()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                reader.Read();
                reader.Close();
                Assert.IsFalse(reader.Read());
            }
        }

        [Test()]
        public void Depth_IsAlwaysZero()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                Assert.AreEqual(0, reader.Depth);
                reader.Read();
                Assert.AreEqual(0, reader.Depth);
            }
        }

        [Test()]
        public void Depth_WhenClosed_ReturnsZero()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                reader.Read();
                reader.Close();
                Assert.AreEqual(0, reader.Depth);
            }
        }

        [Test()]
        public void Closed_WhenClosed_IsTrue()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                Assert.IsFalse(reader.IsClosed);

                reader.Read();
                Assert.IsFalse(reader.IsClosed);

                reader.Close();
                Assert.IsTrue(reader.IsClosed);
            }
        }

        [Test()]
        public void RecordsAffected_IsAlwaysMinusOne()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
            )
            {
                Assert.AreEqual(-1, reader.RecordsAffected);

                reader.Read();
                Assert.AreEqual(-1, reader.RecordsAffected);

                reader.Close();
                Assert.AreEqual(-1, reader.RecordsAffected);
            }
        }

        #endregion

        #region IDataRecord interface

        [Test()]
        public void GetBoolean_WhenCalled_ReturnsBoolean()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                Boolean value = true;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetBoolean(reader.GetOrdinal(typeof(Boolean).FullName)));
                }
            }
        }

        [Test()]
        public void GetByte_WhenCalled_ReturnsByte()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                Byte value = 1;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetByte(reader.GetOrdinal(typeof(Byte).FullName)));
                }
            }
        }

        [Test()]
        public void GetBytesTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                Console.WriteLine(Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
                var expected = Convert.FromBase64String("AQIDBAUGBwgJ");
                while (reader.Read())
                {
                    Byte[] buffer = new Byte[16];

                    long count = reader.GetBytes(reader.GetOrdinal(typeof(System.Byte[]).FullName), 0, buffer, 0, buffer.Length);
                    Assert.AreEqual(expected.Length, count);
                    CollectionAssert.AreEqual(expected.Take((int)count), buffer.Take((int)count));
                }
            }
        }

        [Test()]
        public void GetChar_WhenCalled_ReturnsChar()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                Char value = 'a';
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetChar(reader.GetOrdinal(typeof(Char).FullName)));
                }
            }
        }

        [Test()]
        public void GetCharsTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                Char[] value = "abc".ToCharArray();
                while (reader.Read())
                {
                    Char[] csvValue = new Char[value.Length];

                    long count = reader.GetChars(reader.GetOrdinal(typeof(String).FullName), 0, csvValue, 0, value.Length);

                    Assert.AreEqual(value.Length, count);
                    Assert.AreEqual(value.Length, csvValue.Length);

                    for (int i = 0; i < value.Length; i++)
                        Assert.AreEqual(value[i], csvValue[i]);
                }
            }
        }

        [Test()]
        public void GetDataTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                while (reader.Read())
                {
                    Assert.AreSame(reader, reader.GetData(0));

                    for (int i = 1; i < reader.FieldCount; i++)
                        Assert.IsNull(reader.GetData(i));
                }
            }
        }

        [Test()]
        public void GetDataTypeNameTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                        Assert.AreEqual(CsvReaderSampleData.SampleTypedData1Schema[i].Type.FullName, reader.GetDataTypeName(i));
                }
            }
        }

        [Test()]
        public void GetDateTimeTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                DateTime value = new DateTime(2001, 11, 15);
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetDateTime(reader.GetOrdinal(typeof(DateTime).FullName)));
                }
            }
        }

        [Test()]
        public void GetDecimalTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {
                Decimal value = 1.10m;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetDecimal(reader.GetOrdinal(typeof(Decimal).FullName)));
                }
            }
        }

        [Test()]
        public void GetDoubleTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {
                Double value = 1.1d;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetDouble(reader.GetOrdinal(typeof(Double).FullName)));
                }
            }
        }
        [Test()]
        public void GetFloatTest1()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {
                Single value = 1f;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetFloat(reader.GetOrdinal(typeof(Single).FullName)));
                }
            }
        }

        [Test()]
        public void GetFieldTypeTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {

                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                        Assert.AreEqual(CsvReaderSampleData.SampleTypedData1Schema[i].Type, reader.GetFieldType(i));
                }
            }
        }

        [Test()]
        public void GetFloatTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
            )
            {
                Single value = 1;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetFloat(reader.GetOrdinal(typeof(Single).FullName)));
                }
            }
        }

        [Test()]
        public void GetGuidTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {

                Guid value = new Guid("{11111111-1111-1111-1111-111111111111}");
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetGuid(reader.GetOrdinal(typeof(Guid).FullName)));
                }
            }
        }

        [Test()]
        public void GetInt16Test()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {

                Int16 value = 1;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetInt16(reader.GetOrdinal(typeof(Int16).FullName)));
                }
            }
        }

        [Test()]
        public void GetInt32Test()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {

                Int32 value = 1;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetInt32(reader.GetOrdinal(typeof(Int32).FullName)));
                }
            }
        }

        [Test()]
        public void GetInt64Test()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {

                Int64 value = 1;
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetInt64(reader.GetOrdinal(typeof(Int64).FullName)));
                }
            }
        }

        [Test()]
        public void GetNameTest_WithSchema()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema)
                )
            {

                while (reader.Read())
                {
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header0, reader.GetName(0));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header1, reader.GetName(1));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header2, reader.GetName(2));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header3, reader.GetName(3));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header4, reader.GetName(4));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header5, reader.GetName(5));
                }
            }
        }

        [Test()]
        public void GetOrdinalTest_WithSchema()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema)
                )
            {

                while (reader.Read())
                {
                    Assert.AreEqual(0, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header0));
                    Assert.AreEqual(1, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header1));
                    Assert.AreEqual(2, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header2));
                    Assert.AreEqual(3, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header3));
                    Assert.AreEqual(4, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header4));
                    Assert.AreEqual(5, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header5));
                }
            }
        }


        [Test()]
        public void GetNameTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
                )
            {

                while (reader.Read())
                {
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header0, reader.GetName(0));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header1, reader.GetName(1));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header2, reader.GetName(2));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header3, reader.GetName(3));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header4, reader.GetName(4));
                    Assert.AreEqual(CsvReaderSampleData.SampleData1Header5, reader.GetName(5));
                }
            }
        }

        [Test()]
        public void GetOrdinalTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
                )
            {

                while (reader.Read())
                {
                    Assert.AreEqual(0, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header0));
                    Assert.AreEqual(1, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header1));
                    Assert.AreEqual(2, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header2));
                    Assert.AreEqual(3, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header3));
                    Assert.AreEqual(4, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header4));
                    Assert.AreEqual(5, reader.GetOrdinal(CsvReaderSampleData.SampleData1Header5));
                }
            }
        }

        [Test()]
        public void GetStringTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {

                String value = "abc";
                while (reader.Read())
                {
                    Assert.AreEqual(value, reader.GetString(reader.GetOrdinal(typeof(String).FullName)));
                }
            }
        }

        [Test()]
        public void GetValueTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema)
            )
            {
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
        }

        [Test()]
        public void GetValuesTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema)
            )
            {

                object[] objValues = new object[CsvReaderSampleData.SampleData1RecordCount];
                string[] values = new string[CsvReaderSampleData.SampleData1RecordCount];

                int currentRecordIndex = 0;
                while (reader.Read())
                {
                    Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount, reader.GetValues(objValues));

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        values[i] = objValues[i]?.ToString();
                    }

                    CsvReaderSampleData.CheckSampleData1(true, currentRecordIndex, values);
                    currentRecordIndex++;
                }
            }
        }

        [Test()]
        public void IsDBNullTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleTypedData1, hasHeaders: true, schema: CsvReaderSampleData.SampleTypedData1Schema)
                )
            {
                while (reader.Read())
                {
                    Assert.IsTrue(reader.IsDBNull(reader.GetOrdinal(typeof(DBNull).FullName)));
                    Assert.IsNull(reader[reader.GetOrdinal(typeof(DBNull).FullName)]);
                }
            }
        }

        [Test()]
        public void FieldCountTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema)
                )
            {
                Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount, reader.FieldCount);
            }
        }

        [Test()]
        public void IndexerByFieldNameTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true)
                )
            {
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
        }

        [Test()]
        public void IndexerByFieldIndexTest()
        {
            using (var reader = ReadCsv.FromString(
                CsvReaderSampleData.SampleData1, hasHeaders: true, schema: CsvReaderSampleData.SampleData1Schema)
            )
            {
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
        }

        #endregion
    }
}

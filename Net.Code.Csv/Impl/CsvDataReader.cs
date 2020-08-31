using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using static System.Data.Common.SchemaTableColumn;
using static System.Data.Common.SchemaTableOptionalColumn;

namespace Net.Code.Csv.Impl
{
    internal class CsvDataReader : IDataReader
    {
        private readonly CsvHeader _header;
        private CsvLine _line;
        private CsvParser _parser;
        private readonly IConverter _converter;
        private bool _isDisposed;
        private readonly IEnumerator<CsvLine> _enumerator;
        private bool _eof;

        public CsvDataReader(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter)
        {
            _parser = new CsvParser(reader, csvLayout, csvBehaviour);
            _header = _parser.Header;
            _line = null;
            _converter = converter;
            _enumerator = _parser.GetEnumerator();
        }

        public string GetName(int i) => _header[i];

        public string GetDataTypeName(int i) => typeof(string).FullName;

        public Type GetFieldType(int i) => typeof(string);

        public object GetValue(int i) => IsDBNull(i) ? (object)DBNull.Value : _line.Fields[i];

        public int GetValues(object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length < _line.Fields.Length)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "input array is too small. Expected at least {0}", _line.Fields.Length), nameof(values));
            }

            for (var i = 0; i < _line.Fields.Length; i++)
            {
                values[i] = GetValue(i);
            }

            return _line.Fields.Length;
        }

        public int GetOrdinal(string name)
        {
            if (!_header.TryGetIndex(name, out var index))
            {
                throw new ArgumentException($"'{name}' field header not found", nameof(name));
            }

            return index;
        }

        public bool GetBoolean(int i) => GetValue(i, _converter.ToBoolean);

        public byte GetByte(int i) => GetValue(i, _converter.ToByte);

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) => throw new NotImplementedException();

        public char GetChar(int i) => GetValue(i, _converter.ToChar);

        public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (fieldOffset < 0 || fieldOffset >= int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldOffset));
            }

            var value = GetString(i);
            for (var j = 0; j < length; j++)
            {
                buffer[j + bufferOffset] = value[(int)fieldOffset];
            }

            return length;
        }

        public Guid GetGuid(int i) => GetValue(i, _converter.ToGuid);

        public short GetInt16(int i) => GetValue(i, _converter.ToInt16);

        public int GetInt32(int i) => GetValue(i, _converter.ToInt32);

        public long GetInt64(int i) => GetValue(i, _converter.ToInt64);

        public float GetFloat(int i) => GetValue(i, _converter.ToSingle);

        public double GetDouble(int i) => GetValue(i, _converter.ToDouble);

        public string GetString(int i) => _line.Fields[i];

        public decimal GetDecimal(int i) => GetValue(i, _converter.ToDecimal);

        public DateTime GetDateTime(int i) => GetValue(i, _converter.ToDateTime);

        IDataReader IDataRecord.GetData(int i) => i == 0 ? this : null;

        public bool IsDBNull(int i) => false;

        public int FieldCount => _parser.FieldCount;

        object IDataRecord.this[int i] => _line.Fields[i];

        object IDataRecord.this[string name] => _line.Fields[GetOrdinal(name)];

        private T GetValue<T>(int fieldNumber, Func<string, T> convert) => convert(_line.Fields[fieldNumber]);

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _parser.Dispose();
            _parser = null;
            _eof = true;
            _isDisposed = true;
        }

        public void Close() => Dispose();

        public DataTable GetSchemaTable()
        {
            var schema = new DataTable("SchemaTable");
            try
            {
                schema.Locale = CultureInfo.InvariantCulture;
                schema.MinimumCapacity = FieldCount;
                schema.Columns.Add(AllowDBNull, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(BaseColumnName, typeof(string)).ReadOnly = true;
                schema.Columns.Add(BaseSchemaName, typeof(string)).ReadOnly = true;
                schema.Columns.Add(BaseTableName, typeof(string)).ReadOnly = true;
                schema.Columns.Add(ColumnName, typeof(string)).ReadOnly = true;
                schema.Columns.Add(ColumnOrdinal, typeof(int)).ReadOnly = true;
                schema.Columns.Add(ColumnSize, typeof(int)).ReadOnly = true;
                schema.Columns.Add(DataType, typeof(object)).ReadOnly = true;
                schema.Columns.Add(IsAliased, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsExpression, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsKey, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsLong, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsUnique, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(NumericPrecision, typeof(short)).ReadOnly = true;
                schema.Columns.Add(NumericScale, typeof(short)).ReadOnly = true;
                schema.Columns.Add(ProviderType, typeof(int)).ReadOnly = true;
                schema.Columns.Add(BaseCatalogName, typeof(string)).ReadOnly = true;
                schema.Columns.Add(BaseServerName, typeof(string)).ReadOnly = true;
                schema.Columns.Add(IsAutoIncrement, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsHidden, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsReadOnly, typeof(bool)).ReadOnly = true;
                schema.Columns.Add(IsRowVersion, typeof(bool)).ReadOnly = true;

                string[] columnNames;

                if (_header != null)
                {
                    columnNames = _parser.Header.Fields;
                }
                else
                {
                    columnNames = new string[FieldCount];

                    for (var i = 0; i < FieldCount; i++)
                    {
                        columnNames[i] = "Column" + i.ToString(CultureInfo.InvariantCulture);
                    }
                }

                // null marks columns that will change for each row
                var schemaRow = new object[]
                                     {
                                         true, // 00- AllowDBNull
                                         null, // 01- BaseColumnName
                                         string.Empty, // 02- BaseSchemaName
                                         string.Empty, // 03- BaseTableName
                                         null, // 04- ColumnName
                                         null, // 05- ColumnOrdinal
                                         int.MaxValue, // 06- ColumnSize
                                         typeof (string), // 07- DataType
                                         false, // 08- IsAliased
                                         false, // 09- IsExpression
                                         false, // 10- IsKey
                                         false, // 11- IsLong
                                         false, // 12- IsUnique
                                         DBNull.Value, // 13- NumericPrecision
                                         DBNull.Value, // 14- NumericScale
                                         (int) DbType.String, // 15- ProviderType

                                         string.Empty, // 16- BaseCatalogName
                                         string.Empty, // 17- BaseServerName
                                         false, // 18- IsAutoIncrement
                                         false, // 19- IsHidden
                                         true, // 20- IsReadOnly
                                         false // 21- IsRowVersion
                                     };

                for (var i = 0; i < columnNames.Length; i++)
                {
                    schemaRow[1] = columnNames[i]; // Base column name
                    schemaRow[4] = columnNames[i]; // Column name
                    schemaRow[5] = i; // Column ordinal

                    schema.Rows.Add(schemaRow);
                }
            }
            catch
            {
                schema.Dispose();
                throw;
            }
            return schema;
        }

        public bool NextResult() => false;

        public bool Read()
        {
            if (_eof || !_enumerator.MoveNext())
            {
                _eof = true;
                return false;
            }
            _line = _enumerator.Current;
            return true;
        }

        public int Depth => 0;
        public bool IsClosed => _isDisposed;
        public int RecordsAffected => -1; // For SELECT statements, -1 must be returned.
    }
}
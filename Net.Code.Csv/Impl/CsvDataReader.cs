using static System.Data.Common.SchemaTableColumn;
using static System.Data.Common.SchemaTableOptionalColumn;

namespace Net.Code.Csv.Impl;

internal class CsvDataReader : IDataReader
{
    private readonly TextReader _reader;
    private readonly Converter _converter;
    private readonly CsvLayout _layout;
    private readonly CsvBehaviour _behaviour;
    private CsvHeader _header => _parser.Header;
    private CsvLine _line;
    private CsvParser _parser;
    private bool _isDisposed;
    private IEnumerator<CsvLine> _enumerator;
    private IEnumerator<CsvSchema> _schemas;
    private CsvSchema _schema;
    private bool _eof;

    public CsvDataReader(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, CultureInfo cultureInfo)
    {
        _reader = reader;
        _layout = csvLayout;
        _behaviour = csvBehaviour;
        _converter = new Converter(cultureInfo ?? CultureInfo.InvariantCulture);
        _schemas = _layout.Schemas.GetEnumerator();
        InitializeResultSet();
    }

    private void InitializeResultSet()
    {
        _parser = new CsvParser(_reader, _layout, _behaviour);
        _line = CsvLine.Empty;
        _enumerator = _parser.GetEnumerator();
        if (_schemas?.MoveNext() ?? false)
            _schema = _schemas.Current;
    }

    private int GetIndex(int i) => _schema switch
    {
        null => i,
        not null => _header.TryGetIndex(GetName(i), out var j) ? j : i
    };
    private string GetRawValue(int i) => _line[GetIndex(i)];

    public string GetName(int i) => _schema?.GetName(i) ?? _header[i];

    public string GetDataTypeName(int i) => GetFieldType(i).FullName;

    public Type GetFieldType(int i) => _schema?[i].Type ?? typeof(string);

    public object GetValue(int i) => _schema switch
    {
        not null => GetRawValue(i) switch
        {
            "" => null,
            string s => _schema[i].FromString(s)
        },
        _ => GetRawValue(i)
    };

    public int GetValues(object[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Length < _line.Length)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "input array is too small. Expected at least {0}", _line.Length), nameof(values));
        }

        var n = _schema is not null ? _schema.Columns.Count : _line.Length;

        for (var i = 0; i < n; i++)
        {
            values[i] = GetValue(i);
        }

        return _line.Length;
    }

    public int GetOrdinal(string name)
    {
        if (_schema != null)
        {
            return _schema.GetOrdinal(name);
        }
        if (!_header.TryGetIndex(name, out var index))
        {
            throw new ArgumentException($"'{name}' field header not found", nameof(name));
        }

        return index;
    }

    public bool GetBoolean(int i) => GetValue(i, _converter.ToBoolean);
    public byte GetByte(int i) => GetValue(i, _converter.ToByte);

    public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
    {
        var value = Convert.FromBase64String(GetRawValue(i));

        int copied = 0;
        for (var j = 0; j < length; j++)
        {
            if (j + fieldOffset >= value.Length) break;
            if (j + bufferOffset >= buffer.Length) break;
            buffer[j + bufferOffset] = value[j + (int)fieldOffset];
            copied++;
        }

        return copied;
    }

    public char GetChar(int i) => GetValue(i, _converter.ToChar);

    public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
    {
        var value = GetString(i);
        int copied = 0;
        for (var j = 0; j < length; j++)
        {
            if (j + fieldOffset >= value.Length) break;
            if (j + bufferOffset >= buffer.Length) break;
            buffer[j + bufferOffset] = value[j + (int)fieldOffset];
            copied++;
        }

        return copied;
    }

    public Guid GetGuid(int i) => GetValue(i, _converter.ToGuid);

    public short GetInt16(int i) => GetValue(i, _converter.ToInt16);

    public int GetInt32(int i) => GetValue(i, _converter.ToInt32);

    public long GetInt64(int i) => GetValue(i, _converter.ToInt64);

    public float GetFloat(int i) => GetValue(i, _converter.ToSingle);

    public double GetDouble(int i) => GetValue(i, _converter.ToDouble);

    public string GetString(int i) => GetValue(i, s => s);

    public decimal GetDecimal(int i) => GetValue(i, _converter.ToDecimal);

    public DateTime GetDateTime(int i) => GetValue(i, _converter.ToDateTime);

    IDataReader IDataRecord.GetData(int i) => i == 0 ? this : null;

    public bool IsDBNull(int i) => GetValue(i) is null;

    public int FieldCount => _parser.FieldCount;

    object IDataRecord.this[int i] => GetValue(i);

    object IDataRecord.this[string name] => GetValue(GetOrdinal(name));

    private T GetValue<T>(int i, Func<string, T> convert)
    {
        var value = GetValue(i);
        if (value is T t) return t;
        return convert((string)value);
    }

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
            if (FieldCount > 0)
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

            string[] columnNames =
                _schema is null ? _header.Fields : _schema.Columns.Select(c => c.Name).ToArray();

            // null marks columns that will change for each row
            object[] schemaRow =
                                 [
                                         true, // 00- AllowDBNull
                                         null, // 01- BaseColumnName
                                         null, // 02- BaseSchemaName
                                         null, // 03- BaseTableName
                                         null, // 04- ColumnName
                                         null, // 05- ColumnOrdinal
                                         int.MaxValue, // 06- ColumnSize
                                         null, // 07- DataType
                                         false, // 08- IsAliased
                                         false, // 09- IsExpression
                                         false, // 10- IsKey
                                         false, // 11- IsLong
                                         false, // 12- IsUnique
                                         null, // 13- NumericPrecision
                                         null, // 14- NumericScale
                                         0, // 15- ProviderType
                                         null, // 16- BaseCatalogName
                                         null, // 17- BaseServerName
                                         false, // 18- IsAutoIncrement
                                         false, // 19- IsHidden
                                         true, // 20- IsReadOnly
                                         false // 21- IsRowVersion
                                 ];

            for (var i = 0; i < columnNames.Length; i++)
            {
                schemaRow[0] = _schema?[i].AllowNull ?? true;
                schemaRow[4] = columnNames[i]; // Column name
                schemaRow[5] = i; // Column ordinal
                schemaRow[7] = _schema?[i].Type ?? typeof(string);
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

    public bool NextResult()
    {
        while (!_line.IsEmpty && Read())
        {}
        return !_eof;
    }

    public bool Read()
    {
        if (_eof || !_enumerator.MoveNext())
        {
            _eof = true;
            return false;
        }

        _line = _enumerator.Current;

        if (_line.IsEmpty && _behaviour.EmptyLineAction == EmptyLineAction.NextResult)
        {
            InitializeResultSet();
            return false;
        }

        return true;
    }

    public bool HasRows => !_eof;

    public int Depth => 0;
    public bool IsClosed => _isDisposed;
    public int RecordsAffected => -1; // For SELECT statements, -1 must be returned.
}

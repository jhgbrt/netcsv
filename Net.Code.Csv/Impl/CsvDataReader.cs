using Net.Code.Csv;
using static System.Data.Common.SchemaTableColumn;
using static System.Data.Common.SchemaTableOptionalColumn;

namespace Net.Code.Csv.Impl;
interface ICsvParser
{
    CsvHeader Header { get; }
    int FieldCount { get; }
    IEnumerator<CsvLineSlice> GetEnumerator();
    void Dispose();
}

internal class CsvDataReader : IDataReader
{
    private readonly TextReader _reader;
    private readonly Converter _converter;
    private readonly CsvLayout _layout;
    private readonly CsvBehaviour _behaviour;
    private readonly BufferedCharReader _charReader;
    private readonly CsvSpanConverter<bool> _booleanConverter;
    private readonly CsvSpanConverter<byte> _byteConverter;
    private readonly CsvSpanConverter<char> _charConverter;
    private readonly CsvSpanConverter<Guid> _guidConverter;
    private readonly CsvSpanConverter<short> _int16Converter;
    private readonly CsvSpanConverter<int> _int32Converter;
    private readonly CsvSpanConverter<long> _int64Converter;
    private readonly CsvSpanConverter<float> _singleConverter;
    private readonly CsvSpanConverter<double> _doubleConverter;
    private readonly CsvSpanConverter<decimal> _decimalConverter;
    private readonly CsvSpanConverter<DateTime> _dateTimeConverter;
    private CsvHeader _header => _parser.Header;
    private CsvLineSlice _line;
    private ICsvParser _parser;
    private bool _isDisposed;
    private IEnumerator<CsvLineSlice> _enumerator;
    private IEnumerator<CsvSchema> _schemas;
    private CsvSchema _schema;
    private bool _eof;
    internal CsvSchema Schema => _schema;
    public CsvDataReader(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, CultureInfo cultureInfo)
    {
        _reader = reader;
        _layout = csvLayout;
        _behaviour = csvBehaviour;
        _charReader = new BufferedCharReader(reader);
        _converter = new Converter(cultureInfo ?? CultureInfo.InvariantCulture);
        _booleanConverter = _converter.ToBoolean;
        _byteConverter = _converter.ToByte;
        _charConverter = _converter.ToChar;
        _guidConverter = _converter.ToGuid;
        _int16Converter = _converter.ToInt16;
        _int32Converter = _converter.ToInt32;
        _int64Converter = _converter.ToInt64;
        _singleConverter = _converter.ToSingle;
        _doubleConverter = _converter.ToDouble;
        _decimalConverter = _converter.ToDecimal;
        _dateTimeConverter = _converter.ToDateTime;
        _schemas = _layout.Schemas.GetEnumerator();
        InitializeResultSet();
    }

    private void InitializeResultSet()
    {
        _parser = CsvParserFactory.Create(_reader, _charReader, _layout, _behaviour);
        _line = CsvLineSlice.Empty;
        _enumerator = _parser.GetEnumerator();
        if (_schemas?.MoveNext() ?? false)
            _schema = _schemas.Current;
    }

    private int GetIndex(int i) => _schema switch
    {
        null => i,
        not null => _header.TryGetIndex(GetName(i), out var j) ? j : i
    };
    private CsvField GetField(int i) => _line.GetField(GetIndex(i));
    private string GetRawValue(int i) => GetField(i).GetString();
    private ReadOnlySpan<char> GetRawValueSpan(int i, out bool isNull)
    {
        var field = GetField(i);
        isNull = field.IsNull;
        return field.Span;
    }

    private bool TryGetSpan(int i, out ReadOnlySpan<char> span)
    {
        span = GetRawValueSpan(i, out var isNull);
        return !isNull && span.Length > 0;
    }

    internal string GetStringRaw(int i)
    {
        return TryGetSpan(i, out var span) ? span.ToString() : null;
    }

    internal T GetSchemaRaw<T>(int i)
    {
        var field = GetField(i);
        var span = field.Span;
        if (field.IsNull || span.Length == 0)
        {
            return default;
        }

        return (T)_schema[i].FromSpan(span);
    }

    internal T? GetSchemaNullableRaw<T>(int i) where T : struct
    {
        if (!TryGetSpan(i, out var span))
        {
            return null;
        }

        return (T)_schema[i].FromSpan(span);
    }

    public string GetName(int i) => _schema?.GetName(i) ?? _header[i];

    public string GetDataTypeName(int i) => GetFieldType(i).FullName;

    public Type GetFieldType(int i) => _schema?[i].Type ?? typeof(string);

    public object GetValue(int i)
    {
        if (_schema is null)
        {
            return GetRawValue(i);
        }

        var span = GetRawValueSpan(i, out var isNull);
        if (isNull)
        {
            return null;
        }
        if (span.Length == 0)
        {
            return null;
        }

        return _schema[i].FromSpan(span);
    }

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

    public bool GetBoolean(int i) => GetValue(i, _booleanConverter);
    public byte GetByte(int i) => GetValue(i, _byteConverter);

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

    public char GetChar(int i) => GetValue(i, _charConverter);

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

    public Guid GetGuid(int i) => GetValue(i, _guidConverter);

    public short GetInt16(int i) => GetValue(i, _int16Converter);

    public int GetInt32(int i) => GetValue(i, _int32Converter);

    public long GetInt64(int i) => GetValue(i, _int64Converter);

    public float GetFloat(int i) => GetValue(i, _singleConverter);

    public double GetDouble(int i) => GetValue(i, _doubleConverter);

    public string GetString(int i) => GetValue(i, s => s.ToString());

    public decimal GetDecimal(int i) => GetValue(i, _decimalConverter);

    public DateTime GetDateTime(int i) => GetValue(i, _dateTimeConverter);

    IDataReader IDataRecord.GetData(int i) => i == 0 ? this : null;

    public bool IsDBNull(int i) => GetValue(i) is null;

    public int FieldCount => _parser.FieldCount;

    object IDataRecord.this[int i] => GetValue(i);

    object IDataRecord.this[string name] => GetValue(GetOrdinal(name));

    private T GetValue<T>(int i, CsvSpanConverter<T> convert)
    {
        var span = GetRawValueSpan(i, out var isnull);

        if (_schema is not null)
        {
            if (isnull || span.Length == 0)
            {
                return default;
            }

            var column = _schema[i];
            if (column.Type == typeof(string) && typeof(T) != typeof(string))
            {
                return convert(span);
            }

            var schemaValue = column.FromSpan(span);
            if (schemaValue is T schemaValueTyped2) return schemaValueTyped2;
            return convert(((string)schemaValue).AsSpan());
        }
        else if (isnull)
        {
            var value = GetValue(i);
            if (value is T rawValueTyped) return rawValueTyped;
            return convert(((string)value).AsSpan());
        }

        return convert(span);
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

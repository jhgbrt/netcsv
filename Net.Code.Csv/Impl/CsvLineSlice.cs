namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV line represented as field slices.
/// </summary>
internal readonly struct CsvLineSlice(CsvField[] fields, bool isEmpty)
{
    private readonly CsvField[] _fields = fields;
    private readonly bool _isEmpty = isEmpty;

    public static readonly CsvLineSlice Empty = new([], true);

    public int Length => _fields.Length;
    public string[] Fields => GetStrings();
    public CsvField[] FieldSlices => _fields;
    public bool IsEmpty => _isEmpty;

    public ReadOnlySpan<char> this[int field]
    {
        get
        {
            if (field < _fields.Length)
            {
                return _fields[field].Span;
            }

            if (_isEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }

            throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    public CsvField GetField(int field)
    {
        if (field < _fields.Length)
        {
            return _fields[field];
        }

        if (_isEmpty)
        {
            return CsvField.FromString(string.Empty);
        }

        throw new ArgumentOutOfRangeException(nameof(field));
    }

    public string GetString(int field)
    {
        return GetField(field).GetString();
    }

    public string[] GetStrings()
    {
        if (_fields.Length == 0)
        {
            return Array.Empty<string>();
        }

        var result = new string[_fields.Length];
        for (var i = 0; i < _fields.Length; i++)
        {
            result[i] = _fields[i].GetString();
        }
        return result;
    }
}

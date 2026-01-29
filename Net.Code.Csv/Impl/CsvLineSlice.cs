using System.Buffers;

namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV line represented as field slices.
/// </summary>
internal readonly struct CsvLineSlice
{
    private readonly CsvField[] _fields;
    private readonly int _count;
    private readonly bool _isEmpty;
    private readonly bool _pooled;

    public static readonly CsvLineSlice Empty = new([], 0, true, false);

    internal CsvLineSlice(CsvField[] fields, bool isEmpty)
        : this(fields, fields?.Length ?? 0, isEmpty, false)
    {
    }

    internal CsvLineSlice(CsvField[] fields, int count, bool isEmpty, bool pooled)
    {
        _fields = fields ?? [];
        _count = count < 0 ? 0 : count;
        _isEmpty = isEmpty;
        _pooled = pooled;
    }

    public int Length => _count;
    public string[] Fields => GetStrings();
    public ReadOnlySpan<CsvField> FieldSlices => _fields.AsSpan(0, _count);
    public bool IsEmpty => _isEmpty;

    public ReadOnlySpan<char> this[int field]
    {
        get
        {
            if (field < _count)
            {
                return _fields[field].Span;
            }

            if (_isEmpty)
            {
                return [];
            }

            throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    public CsvField GetField(int field)
    {
        if (field < _count)
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
        if (_count == 0)
        {
            return [];
        }

        var result = new string[_count];
        for (var i = 0; i < _count; i++)
        {
            result[i] = _fields[i].GetString();
        }
        return result;
    }

    // Used when a line must outlive the parser (e.g., header read before enumeration starts).
    internal CsvLineSlice ToOwned()
    {
        if (_count == 0)
        {
            return Empty;
        }

        var copy = new CsvField[_count];
        Array.Copy(_fields, copy, _count);
        return new CsvLineSlice(copy, _count, _isEmpty, false);
    }

    // Return pooled arrays once the consumer has moved past the current line.
    internal void ReturnToPool()
    {
        if (!_pooled || _fields.Length == 0)
        {
            return;
        }

        ArrayPool<CsvField>.Shared.Return(_fields, clearArray: true);
    }
}
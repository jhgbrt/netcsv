namespace Net.Code.Csv.Impl;

internal readonly struct CsvField
{
    private readonly string _string;
    private readonly ReadOnlyMemory<char> _buffer;
    private readonly Range _range;
    private readonly bool _hasString;

    public bool IsNull { get; }

    private CsvField(string value, bool isNull)
    {
        _string = value;
        _buffer = default;
        _range = 0..0;
        _hasString = true;
        IsNull = isNull;
    }

    private CsvField(ReadOnlyMemory<char> buffer, Range range)
    {
        _string = null;
        _buffer = buffer;
        _range = range;
        _hasString = false;
        IsNull = false;
    }

    public static CsvField FromString(string value) => new(value, value is null);

    public static CsvField FromBuffer(ReadOnlyMemory<char> buffer, Range range)
        => new(buffer, range);

    public ReadOnlySpan<char> Span
        => IsNull ? ReadOnlySpan<char>.Empty : _hasString ? _string.AsSpan() : _buffer.Span[_range];

    public string GetString() => IsNull ? null : _hasString ? _string : new string(Span);
}

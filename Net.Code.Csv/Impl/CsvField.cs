namespace Net.Code.Csv.Impl;

internal readonly struct CsvField
{
    private readonly string _string;
    private readonly ReadOnlyMemory<char> _buffer;
    private readonly int _start;
    private readonly int _length;
    private readonly bool _hasString;

    public bool IsNull { get; }

    private CsvField(string value, bool isNull)
    {
        _string = value;
        _buffer = default;
        _start = 0;
        _length = 0;
        _hasString = true;
        IsNull = isNull;
    }

    private CsvField(ReadOnlyMemory<char> buffer, int start, int length)
    {
        _string = null;
        _buffer = buffer;
        _start = start;
        _length = length;
        _hasString = false;
        IsNull = false;
    }

    public static CsvField FromString(string value) => new(value, value is null);

    public static CsvField FromBuffer(ReadOnlyMemory<char> buffer, int start, int length)
        => new(buffer, start, length);

    public ReadOnlySpan<char> Span
        => IsNull ? ReadOnlySpan<char>.Empty : _hasString ? _string.AsSpan() : _buffer.Span.Slice(_start, _length);

    public string GetString() => IsNull ? null : _hasString ? _string : new string(Span);
}

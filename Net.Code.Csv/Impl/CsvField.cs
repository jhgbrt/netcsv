namespace Net.Code.Csv.Impl;

internal readonly struct CsvField
{
    // A field is either a whole string or a slice of a char array (reader buffer,
    // line buffer or a materialized copy). A negative length marks a null field.
    private readonly string _string;
    private readonly char[] _array;
    private readonly int _start;
    private readonly int _length;

    private CsvField(string value)
    {
        _string = value;
        _array = null;
        _start = 0;
        _length = value is null ? -1 : value.Length;
    }

    private CsvField(char[] array, int start, int length)
    {
        _string = null;
        _array = array;
        _start = start;
        _length = length;
    }

    public bool IsNull => _length < 0;

    public static CsvField FromString(string value) => new(value);

    public static CsvField FromArray(char[] array, int start, int length)
        => new(array, start, length);

    public ReadOnlySpan<char> Span
        => _array is not null
            ? new ReadOnlySpan<char>(_array, _start, _length)
            : _string.AsSpan();

    public string GetString()
        => _array is not null
            ? (_length == 0 ? string.Empty : new string(_array, _start, _length))
            : _string ?? (IsNull ? null : string.Empty);
}

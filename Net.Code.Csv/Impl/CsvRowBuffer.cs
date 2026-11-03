using System.Buffers;

namespace Net.Code.Csv.Impl;

/// <summary>
/// Accumulates one CSV row in a pooled char buffer. Values are formatted directly into the buffer
/// (no intermediate strings for primitives) and quoted/escaped in place when necessary.
/// </summary>
/// <remarks>
/// The formatting of every Write* method mirrors <see cref="Converter.ToString(object, string)"/> exactly:
/// integral types and decimal use "G" and floating point types "R" (as their TypeDescriptor converters do),
/// DateTime/DateTimeOffset default to "O", TimeSpan to "c", '\0' is written as an empty field, null as empty.
/// The public methods are invoked from compiled expression trees (see <see cref="CsvRowWriter{T}"/>).
/// </remarks>
internal sealed class CsvRowBuffer : IDisposable
{
    private const int InitialSize = 256;

    private readonly CultureInfo _culture;
    private readonly Converter _converter;
    private readonly char _delimiter;
    private readonly char _quote;
    private readonly char _escape;
    private readonly char[] _special;
    private char[] _buffer;
    private int _length;

    public CsvRowBuffer(CultureInfo culture, char delimiter, char quote, char escape)
    {
        _culture = culture;
        _converter = new Converter(culture);
        _delimiter = delimiter;
        _quote = quote;
        _escape = escape;
        _special = [quote, delimiter, '\n', '\r'];
        _buffer = ArrayPool<char>.Shared.Rent(InitialSize);
    }

    public char[] Buffer => _buffer;
    public int Length => _length;

    public void Clear() => _length = 0;

    public void Delimiter()
    {
        if (_length == _buffer.Length)
        {
            Grow(1);
        }
        _buffer[_length++] = _delimiter;
    }

    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        EnsureCapacity(value.Length);
        var start = _length;
        value.CopyTo(0, _buffer, _length, value.Length);
        _length += value.Length;
        EndField(start);
    }

    public void WriteObject(object value, string format) => WriteString(_converter.ToString(value, format));

    public void WriteBoolean(bool value) => WriteString(value ? bool.TrueString : bool.FalseString);

    public void WriteChar(char value)
    {
        // CharConverter converts '\0' to an empty string.
        if (value == '\0')
        {
            return;
        }
        if (_length == _buffer.Length)
        {
            Grow(1);
        }
        var start = _length;
        _buffer[_length++] = value;
        EndField(start);
    }

    public void WriteByte(byte value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteSByte(sbyte value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteInt16(short value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteUInt16(ushort value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteInt32(int value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteUInt32(uint value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteInt64(long value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteUInt64(ulong value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteSingle(float value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "R", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteDouble(double value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "R", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteDecimal(decimal value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "G", _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteGuid(Guid value)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, "D")) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteDateTime(DateTime value, string format)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, format, _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteDateTimeOffset(DateTimeOffset value, string format)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, format, _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    public void WriteTimeSpan(TimeSpan value, string format)
    {
        var start = _length;
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, format, _culture)) Grow(written);
        _length += written;
        EndField(start);
    }

    /// <summary>
    /// Quotes the field that starts at <paramref name="start"/> if it contains the quote character,
    /// the delimiter, '\n' or '\r'; every quote character is then preceded by the escape character.
    /// Single linear pass, performed in place from the back of the buffer.
    /// </summary>
    private void EndField(int start)
    {
        var field = new ReadOnlySpan<char>(_buffer, start, _length - start);
        var first = field.IndexOfAny(_special);
        if (first < 0)
        {
            return;
        }

        var quotes = 0;
        for (var i = first; i < field.Length; i++)
        {
            if (field[i] == _quote) quotes++;
        }

        var extra = quotes + 2;
        EnsureCapacity(extra);

        var buffer = _buffer;
        var quote = _quote;
        var escape = _escape;
        var src = _length - 1;
        var dst = _length + extra - 1;
        buffer[dst--] = quote;
        while (src >= start)
        {
            var c = buffer[src--];
            buffer[dst--] = c;
            if (c == quote)
            {
                buffer[dst--] = escape;
            }
        }
        buffer[dst] = quote;
        _length += extra;
    }

    private void EnsureCapacity(int additional)
    {
        if (_buffer.Length - _length < additional)
        {
            Grow(additional);
        }
    }

    private void Grow(int additional)
    {
        var newSize = Math.Max(_buffer.Length * 2, _length + Math.Max(additional, 16));
        var newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        Array.Copy(_buffer, newBuffer, _length);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer is not null)
        {
            _buffer = null;
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}

using System.IO;

namespace Net.Code.Csv.Impl;

/// <summary>
/// Provides buffered access to a <see cref="TextReader"/> so the CSV state machine
/// can consume characters sequentially while still peeking at the next character
/// without paying the cost of <see cref="TextReader.Peek"/> on every iteration.
/// </summary>
internal sealed class BufferedCharReader
{
    private readonly TextReader _reader;
    private readonly char[] _buffer;
    private int _index;
    private int _length;

    private const int DefaultBufferSize = 4096;

    public BufferedCharReader(TextReader reader, int bufferSize = DefaultBufferSize)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _buffer = new char[bufferSize];
    }

    private bool EnsureData()
    {
        if (_index < _length)
        {
            return true;
        }

        _length = _reader.Read(_buffer.AsSpan());
        _index = 0;
        return _length > 0;
    }

    public bool TryGetSpan(out ReadOnlySpan<char> span)
    {
        if (!EnsureData())
        {
            span = ReadOnlySpan<char>.Empty;
            return false;
        }

        span = _buffer.AsSpan(_index, _length - _index);
        return true;
    }

    public void Advance(int count)
    {
        _index += count;
    }

    public char? Peek()
    {
        if (_index < _length)
        {
            return _buffer[_index];
        }

        var peek = _reader.Peek();
        return peek < 0 ? null : (char?)peek;
    }

    public bool MoveNext(out char current, out char? next)
    {
        if (!EnsureData())
        {
            current = default;
            next = null;
            return false;
        }

        current = _buffer[_index++];

        if (_index < _length)
        {
            next = _buffer[_index];
        }
        else
        {
            var peek = _reader.Peek();
            next = peek < 0 ? null : (char?)peek;
        }

        return true;
    }
}

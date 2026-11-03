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

    private const int DefaultBufferSize = 16384;

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

        if (!_rawSuspended)
        {
            CaptureRaw(_length);
        }
        _rawStart = 0;

        _length = _reader.Read(_buffer.AsSpan());
        _index = 0;
        return _length > 0;
    }

    // Raw data tracking for error messages (MalformedCsvException.RawData).
    // RawData is the last RawWindowSize characters consumed by the parser, excluding
    // comment bodies. Instead of recording every character, the consumed characters are
    // reconstructed from the buffer when an exception is thrown; only the tail of the
    // previous buffer (or the data preceding a comment) is carried over.
    private const int RawWindowSize = 32;
    private readonly char[] _rawCarry = new char[RawWindowSize];
    private int _rawCarryCount;
    private int _rawStart;
    private bool _rawSuspended;

    // Starts a new raw window at the current position (a new line enumerator/result set).
    public void ResetRaw()
    {
        _rawCarryCount = 0;
        _rawStart = _index;
        _rawSuspended = false;
    }

    // Stops recording raw data after the character at absoluteEnd - 1 (e.g. inside a comment).
    public void SuspendRaw(int absoluteEnd)
    {
        CaptureRaw(absoluteEnd);
        _rawSuspended = true;
    }

    // Resumes recording raw data starting at absoluteStart in the current buffer.
    public void ResumeRaw(int absoluteStart)
    {
        _rawStart = absoluteStart;
        _rawSuspended = false;
    }

    // Returns the raw window ending just before absoluteEnd in the current buffer.
    public string GetRawData(int absoluteEnd)
    {
        var count = _rawSuspended ? 0 : Math.Max(0, absoluteEnd - _rawStart);
        if (count >= RawWindowSize)
        {
            return new string(_buffer, absoluteEnd - RawWindowSize, RawWindowSize);
        }

        var keep = Math.Min(_rawCarryCount, RawWindowSize - count);
        if (keep + count == 0)
        {
            return string.Empty;
        }

        var result = new char[keep + count];
        Array.Copy(_rawCarry, _rawCarryCount - keep, result, 0, keep);
        Array.Copy(_buffer, absoluteEnd - count, result, keep, count);
        return new string(result);
    }

    private void CaptureRaw(int absoluteEnd)
    {
        var count = absoluteEnd - _rawStart;
        if (count <= 0)
        {
            return;
        }

        if (count >= RawWindowSize)
        {
            Array.Copy(_buffer, absoluteEnd - RawWindowSize, _rawCarry, 0, RawWindowSize);
            _rawCarryCount = RawWindowSize;
        }
        else
        {
            var keep = Math.Min(_rawCarryCount, RawWindowSize - count);
            Array.Copy(_rawCarry, _rawCarryCount - keep, _rawCarry, 0, keep);
            Array.Copy(_buffer, _rawStart, _rawCarry, keep, count);
            _rawCarryCount = keep + count;
        }

        _rawStart = absoluteEnd;
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

    // Exposes the active read buffer so callers can create zero-copy slices.
    // The array is reused on subsequent reads; callers must materialize slices
    // before advancing past the current span.
    public ReadOnlyMemory<char> CurrentBuffer => _buffer.AsMemory(0, _length);

    public int CurrentIndex => _index;

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

    public char? PeekAt(int absoluteIndex)
    {
        if (absoluteIndex < _length)
        {
            return _buffer[absoluteIndex];
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

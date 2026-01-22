namespace Net.Code.Csv.Impl;

internal sealed class CsvLineSliceBuilder(CsvLayout layout, CsvBehaviour behaviour)
{
    private char _currentChar;
    private char? _next;
    private bool _quoted;
    private bool _fieldStarted;
    private int _fieldStart;
    private Location _location = Location.Origin().NextLine();

    private const int RawWindowSize = 32;

    private char[] _lineBuffer = new char[256];
    private int _lineLength;
    private char[] _tentativeBuffer = new char[64];
    private int _tentativeLength;

    private readonly List<FieldSliceInfo> _fields = [];
    private readonly char[] _rawBuffer = new char[RawWindowSize];
    private int _rawBufferCount;
    private int _rawBufferIndex;

    public string RawData
    {
        get
        {
            if (_rawBufferCount == 0)
            {
                return string.Empty;
            }

            var result = new char[_rawBufferCount];
            var start = (_rawBufferIndex - _rawBufferCount + RawWindowSize) % RawWindowSize;

            if (start + _rawBufferCount <= RawWindowSize)
            {
                Array.Copy(_rawBuffer, start, result, 0, _rawBufferCount);
            }
            else
            {
                var firstPart = RawWindowSize - start;
                Array.Copy(_rawBuffer, start, result, 0, firstPart);
                Array.Copy(_rawBuffer, 0, result, firstPart, _rawBufferCount - firstPart);
            }

            return new string(result);
        }
    }

    public CsvChar CurrentChar => new(_currentChar, layout, _next);

    public Location Location => _location;

    public int? FieldCount { get; set; }

    public int FieldsCount => _fields.Count;

    internal CsvLineSliceBuilder PrepareNextLine()
    {
        _fields.Clear();
        _lineLength = 0;
        _tentativeLength = 0;
        _quoted = false;
        _fieldStarted = false;
        _fieldStart = 0;
        _location = _location.NextLine();
        return this;
    }

    internal CsvLineSliceBuilder AddToField()
    {
        EnsureFieldStart();
        AppendLine(_currentChar);
        return this;
    }

    internal CsvLineSliceBuilder AddToField(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return this;
        }

        EnsureFieldStart();
        EnsureLineCapacity(_lineLength + span.Length);
        span.CopyTo(_lineBuffer.AsSpan(_lineLength));
        _lineLength += span.Length;
        return this;
    }

    internal CsvLineSliceBuilder AcceptTentative()
    {
        if (_tentativeLength == 0)
        {
            return this;
        }

        EnsureFieldStart();
        EnsureLineCapacity(_lineLength + _tentativeLength);
        Array.Copy(_tentativeBuffer, 0, _lineBuffer, _lineLength, _tentativeLength);
        _lineLength += _tentativeLength;
        _tentativeLength = 0;
        return this;
    }

    internal CsvLineSliceBuilder AddToTentative()
    {
        EnsureTentativeCapacity(_tentativeLength + 1);
        _tentativeBuffer[_tentativeLength++] = _currentChar;
        return this;
    }

    internal CsvLineSliceBuilder AddToTentative(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return this;
        }

        EnsureTentativeCapacity(_tentativeLength + span.Length);
        span.CopyTo(_tentativeBuffer.AsSpan(_tentativeLength));
        _tentativeLength += span.Length;
        return this;
    }

    internal CsvLineSliceBuilder DiscardTentative()
    {
        _tentativeLength = 0;
        return this;
    }

    internal CsvLineSliceBuilder NextField()
    {
        var start = _fieldStarted ? _fieldStart : _lineLength;
        var length = _fieldStarted ? _lineLength - _fieldStart : 0;

        var trim = behaviour.TrimmingOptions switch
        {
            ValueTrimmingOptions.All => true,
            ValueTrimmingOptions.QuotedOnly when _quoted => true,
            ValueTrimmingOptions.UnquotedOnly when !_quoted => true,
            _ => false
        };

        if (trim)
        {
            (start, length) = TrimSlice(start, length);
        }

        _fields.Add(new FieldSliceInfo(start, length, false));
        _quoted = false;
        _fieldStarted = false;
        _fieldStart = _lineLength;
        return this;
    }

    internal CsvLineSliceBuilder MarkQuoted()
    {
        _quoted = true;
        return this;
    }

    internal CsvLineSlice ToLine()
    {
        var lineIsEmpty = _fields.Count == 0 || (_fields.Count == 1 && _fields[0].Length == 0 && !_fields[0].IsNull);
        var count = _fields.Count;

        if (!FieldCount.HasValue)
        {
            if (!(lineIsEmpty && behaviour.EmptyLineAction == EmptyLineAction.Skip))
            {
                FieldCount = count;
            }
        }

        var fields = _fields;
        bool fieldsAreMissing = count < FieldCount;
        if (fieldsAreMissing)
        {
            var missingCount = FieldCount.Value - count;
            if (behaviour.MissingFieldAction == MissingFieldAction.ParseError && !lineIsEmpty)
            {
                throw new MissingFieldCsvException(RawData, Location, count);
            }

            if (behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull)
            {
                for (var i = 0; i < missingCount; i++)
                {
                    fields.Add(new FieldSliceInfo(0, 0, true));
                }
            }
            else
            {
                for (var i = 0; i < missingCount; i++)
                {
                    fields.Add(new FieldSliceInfo(0, 0, false));
                }
            }
        }

        var bufferMemory = new ReadOnlyMemory<char>(_lineBuffer, 0, _lineLength);
        var fieldArray = new CsvField[fields.Count];
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            fieldArray[i] = field.IsNull
                ? CsvField.FromString(null)
                : CsvField.FromBuffer(bufferMemory, field.Start, field.Length);
        }

        var line = new CsvLineSlice(fieldArray, lineIsEmpty);

        PrepareNextLine();

        return line;
    }

    internal bool ReadNext(BufferedCharReader reader)
    {
        if (!reader.MoveNext(out var currentChar, out var next))
        {
            return false;
        }
        _next = next;
        _location = _location.NextColumn();
        _currentChar = currentChar;
        AppendRaw(currentChar);
        return true;
    }

    internal CsvLineSliceBuilder SetCurrent(char currentChar, char? next)
    {
        _next = next;
        _location = _location.NextColumn();
        _currentChar = currentChar;
        AppendRaw(currentChar);
        return this;
    }

    internal CsvLineSliceBuilder Ignore() => this;

    internal CsvLineSliceBuilder AdvanceSpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return this;
        }

        _location = _location with { Column = _location.Column + span.Length };
        AppendRaw(span);
        return this;
    }

    private void EnsureFieldStart()
    {
        if (_fieldStarted)
        {
            return;
        }

        _fieldStart = _lineLength;
        _fieldStarted = true;
    }

    private void AppendLine(char c)
    {
        EnsureLineCapacity(_lineLength + 1);
        _lineBuffer[_lineLength++] = c;
    }

    private void EnsureLineCapacity(int needed)
    {
        if (_lineBuffer.Length >= needed)
        {
            return;
        }

        var newSize = _lineBuffer.Length * 2;
        if (newSize < needed)
        {
            newSize = needed;
        }
        Array.Resize(ref _lineBuffer, newSize);
    }

    private void EnsureTentativeCapacity(int needed)
    {
        if (_tentativeBuffer.Length >= needed)
        {
            return;
        }

        var newSize = _tentativeBuffer.Length * 2;
        if (newSize < needed)
        {
            newSize = needed;
        }
        Array.Resize(ref _tentativeBuffer, newSize);
    }

    private (int start, int length) TrimSlice(int start, int length)
    {
        if (length == 0)
        {
            return (start, 0);
        }

        var end = start + length - 1;
        while (start <= end && char.IsWhiteSpace(_lineBuffer[start])) start++;
        while (end >= start && char.IsWhiteSpace(_lineBuffer[end])) end--;
        return end < start ? (start, 0) : (start, end - start + 1);
    }

    private void AppendRaw(char currentChar)
    {
        _rawBuffer[_rawBufferIndex] = currentChar;
        _rawBufferIndex = (_rawBufferIndex + 1) % RawWindowSize;
        if (_rawBufferCount < RawWindowSize)
        {
            _rawBufferCount++;
        }
    }

    private void AppendRaw(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return;
        }

        if (span.Length >= RawWindowSize)
        {
            span[^RawWindowSize..].CopyTo(_rawBuffer);
            _rawBufferIndex = 0;
            _rawBufferCount = RawWindowSize;
            return;
        }

        for (var i = 0; i < span.Length; i++)
        {
            AppendRaw(span[i]);
        }
    }

    private readonly record struct FieldSliceInfo(int Start, int Length, bool IsNull);
}

using System.Buffers;

namespace Net.Code.Csv.Impl.V2;

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

    private ReadOnlyMemory<char> _directBuffer;
    private int _directStart;
    private int _directLength;
    private bool _usingDirectSlice;

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
        EnsureLineBufferForField();
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

        EnsureLineBufferForField();
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
        if (_usingDirectSlice)
        {
            var directStart = _directStart;
            var directLength = _directLength;
            var shouldTrimDirect = behaviour.TrimmingOptions switch
            {
                ValueTrimmingOptions.All => true,
                ValueTrimmingOptions.QuotedOnly when _quoted => true,
                ValueTrimmingOptions.UnquotedOnly when !_quoted => true,
                _ => false
            };

            if (shouldTrimDirect)
            {
                (directStart, directLength) = TrimSlice(_directBuffer.Span, directStart, directLength);
            }

            _fields.Add(new FieldSliceInfo(_directBuffer, directStart, directLength, false, false));
            _quoted = false;
            _fieldStarted = false;
            _usingDirectSlice = false;
            _directBuffer = default;
            _directStart = 0;
            _directLength = 0;
            _fieldStart = _lineLength;
            return this;
        }

        var start = _fieldStarted ? _fieldStart : _lineLength;
        var length = _fieldStarted ? _lineLength - _fieldStart : 0;

        var shouldTrim = behaviour.TrimmingOptions switch
        {
            ValueTrimmingOptions.All => true,
            ValueTrimmingOptions.QuotedOnly when _quoted => true,
            ValueTrimmingOptions.UnquotedOnly when !_quoted => true,
            _ => false
        };

        if (shouldTrim)
        {
            (start, length) = TrimSlice(_lineBuffer, start, length);
        }

        _fields.Add(new FieldSliceInfo(default, start, length, false, true));
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
                    fields.Add(new FieldSliceInfo(default, 0, 0, true, true));
                }
            }
            else
            {
                for (var i = 0; i < missingCount; i++)
                {
                    fields.Add(new FieldSliceInfo(default, 0, 0, false, true));
                }
            }
        }

        var bufferMemory = new ReadOnlyMemory<char>(_lineBuffer, 0, _lineLength);
        var totalCount = fields.Count;
        if (totalCount == 0)
        {
            var emptyLine = new CsvLineSlice(Array.Empty<CsvField>(), 0, lineIsEmpty, false);
            PrepareNextLine();
            return emptyLine;
        }

        var fieldArray = ArrayPool<CsvField>.Shared.Rent(totalCount);
        for (var i = 0; i < totalCount; i++)
        {
            var field = fields[i];
            fieldArray[i] = field.IsNull
                ? CsvField.FromString(null)
                : CsvField.FromBuffer(field.UseLineBuffer ? bufferMemory : field.Buffer, field.Start, field.Length);
        }

        var line = new CsvLineSlice(fieldArray, totalCount, lineIsEmpty, true);

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

    internal bool TrySetDirectSlice(ReadOnlyMemory<char> buffer, int start, int length)
    {
        if (_fieldStarted || _tentativeLength != 0 || _usingDirectSlice)
        {
            return false;
        }

        _directBuffer = buffer;
        _directStart = start;
        _directLength = length;
        _usingDirectSlice = true;
        _fieldStarted = true;
        return true;
    }

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

    internal CsvLineSliceBuilder MaterializeDirectSlices(ReadOnlyMemory<char> buffer)
    {
        if (_usingDirectSlice && _directBuffer.Equals(buffer))
        {
            EnsureLineBufferForField();
        }

        if (_fields.Count == 0)
        {
            return this;
        }

        for (var i = 0; i < _fields.Count; i++)
        {
            var field = _fields[i];
            if (field.UseLineBuffer || field.IsNull)
            {
                continue;
            }

            if (!field.Buffer.Equals(buffer))
            {
                continue;
            }

            var copy = new char[field.Length];
            field.Buffer.Span.Slice(field.Start, field.Length).CopyTo(copy);
            _fields[i] = new FieldSliceInfo(copy, 0, field.Length, field.IsNull, false);
        }

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

    private void EnsureLineBufferForField()
    {
        if (!_usingDirectSlice)
        {
            return;
        }

        if (!_fieldStarted)
        {
            _fieldStart = _lineLength;
            _fieldStarted = true;
        }

        var directSpan = _directBuffer.Span.Slice(_directStart, _directLength);
        AppendLineSpan(directSpan);
        _usingDirectSlice = false;
        _directBuffer = default;
        _directStart = 0;
        _directLength = 0;
    }

    private void AppendLine(char c)
    {
        EnsureLineCapacity(_lineLength + 1);
        _lineBuffer[_lineLength++] = c;
    }

    private void AppendLineSpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return;
        }

        EnsureLineCapacity(_lineLength + span.Length);
        span.CopyTo(_lineBuffer.AsSpan(_lineLength));
        _lineLength += span.Length;
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

    private (int start, int length) TrimSlice(char[] buffer, int start, int length)
    {
        if (length == 0)
        {
            return (start, 0);
        }

        var end = start + length - 1;
        while (start <= end && char.IsWhiteSpace(buffer[start])) start++;
        while (end >= start && char.IsWhiteSpace(buffer[end])) end--;
        return end < start ? (start, 0) : (start, end - start + 1);
    }

    private (int start, int length) TrimSlice(ReadOnlySpan<char> buffer, int start, int length)
    {
        if (length == 0)
        {
            return (start, 0);
        }

        var end = start + length - 1;
        while (start <= end && char.IsWhiteSpace(buffer[start])) start++;
        while (end >= start && char.IsWhiteSpace(buffer[end])) end--;
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

    private readonly record struct FieldSliceInfo(ReadOnlyMemory<char> Buffer, int Start, int Length, bool IsNull, bool UseLineBuffer);
}

namespace Net.Code.Csv.Impl;

internal sealed class CsvLineSliceBuilder(CsvLayout layout, CsvBehaviour behaviour, BufferedCharReader reader)
{
    private char _currentChar;
    private char? _next;
    private bool _quoted;
    private bool _fieldStarted;
    private int _fieldStart;
    private Location _location = Location.Origin().NextLine();

    private char[] _lineBuffer = new char[256];
    private int _lineLength;
    private char[] _tentativeBuffer = new char[64];
    private int _tentativeLength;

    // Fields of the line being built. The array is reused for every line: a line is only valid
    // until the next MoveNext, and lines that must outlive it are copied via CsvLineSlice.ToOwned.
    // Fields point into the line buffer, the reader buffer (direct slices) or a private copy.
    private CsvField[] _fields = new CsvField[16];
    private int _fieldCount;
    private int _directFieldCount;
    private readonly bool _trimAll = behaviour.TrimmingOptions == ValueTrimmingOptions.All;
    private readonly bool _trimQuoted = behaviour.TrimmingOptions == ValueTrimmingOptions.QuotedOnly;
    private readonly bool _trimUnquoted = behaviour.TrimmingOptions == ValueTrimmingOptions.UnquotedOnly;

    // Holds a reference to the reader's current buffer for zero-copy fields.
    // Must be materialized before the reader advances and reuses the array.
    private char[] _directBuffer;
    private int _directStart;
    private int _directLength;
    private bool _usingDirectSlice;

    public CsvChar CurrentChar => new(_currentChar, layout, _next);

    public Location Location => _location;

    public int? FieldCount { get; set; }

    public int FieldsCount => _fieldCount;

    internal bool HasTentative => _tentativeLength > 0;

    internal CsvLineSliceBuilder PrepareNextLine()
    {
        _fieldCount = 0;
        _lineLength = 0;
        _tentativeLength = 0;
        _quoted = false;
        _fieldStarted = false;
        _fieldStart = 0;
        _directFieldCount = 0;
        // A line abandoned mid-field (parse error recovery) may leave a pending direct slice.
        _usingDirectSlice = false;
        _directBuffer = null;
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

        // A pending direct slice (e.g. quoted content before `"  x`) must precede the tentative chars.
        EnsureLineBufferForField();
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
            var start = _directStart;
            var length = _directLength;
            var shouldTrimDirect = _trimAll || (_trimQuoted && _quoted) || (_trimUnquoted && !_quoted);

            if (shouldTrimDirect)
            {
                Trim(_directBuffer, ref start, ref length);
            }

            AddField(CsvField.FromArray(_directBuffer, start, length));
            _directFieldCount++;
            _quoted = false;
            _fieldStarted = false;
            _usingDirectSlice = false;
            _directBuffer = null;
            _fieldStart = _lineLength;
            return this;
        }

        var fieldStart = _fieldStarted ? _fieldStart : _lineLength;
        var fieldLength = _lineLength - fieldStart;

        var shouldTrim = _trimAll || (_trimQuoted && _quoted) || (_trimUnquoted && !_quoted);

        if (shouldTrim)
        {
            Trim(_lineBuffer, ref fieldStart, ref fieldLength);
        }

        AddField(CsvField.FromArray(_lineBuffer, fieldStart, fieldLength));
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

    internal CsvLineSlice ToLine(int rawEnd)
    {
        var count = _fieldCount;
        var lineIsEmpty = count == 0 || (count == 1 && _fields[0].Length == 0 && !_fields[0].IsNull);

        if (!FieldCount.HasValue)
        {
            if (!(lineIsEmpty && behaviour.EmptyLineAction == EmptyLineAction.Skip))
            {
                FieldCount = count;
            }
        }

        bool fieldsAreMissing = count < FieldCount;
        if (fieldsAreMissing)
        {
            var missingCount = FieldCount.Value - count;
            if (behaviour.MissingFieldAction == MissingFieldAction.ParseError && !lineIsEmpty)
            {
                throw new MissingFieldCsvException(reader.GetRawData(rawEnd), Location, count);
            }

            var filler = CsvField.FromString(behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : string.Empty);
            for (var i = 0; i < missingCount; i++)
            {
                AddField(filler);
            }
        }

        var totalCount = _fieldCount;
        if (totalCount == 0)
        {
            var emptyLine = new CsvLineSlice([], 0, lineIsEmpty, false);
            PrepareNextLine();
            return emptyLine;
        }

        var line = new CsvLineSlice(_fields, totalCount, lineIsEmpty, false);

        PrepareNextLine();

        return line;
    }

    private void AddField(CsvField field)
    {
        if (_fieldCount == _fields.Length)
        {
            Array.Resize(ref _fields, _fields.Length * 2);
        }
        _fields[_fieldCount++] = field;
    }

    internal CsvLineSliceBuilder SetCurrent(char currentChar, char? next)
    {
        _next = next;
        _location = _location.NextColumn();
        _currentChar = currentChar;
        return this;
    }

    internal CsvLineSliceBuilder SetCurrent(char currentChar)
    {
        _next = null;
        _location = _location.NextColumn();
        _currentChar = currentChar;
        return this;
    }

    internal CsvLineSliceBuilder Ignore() => this;

    internal bool TrySetDirectSlice(char[] buffer, int start, int length)
    {
        if (_fieldStarted || _tentativeLength != 0 || _usingDirectSlice)
        {
            return false;
        }

        // Track the buffer identity so we can later materialize if it is reused.
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
        return this;
    }

    internal CsvLineSliceBuilder MaterializeDirectSlices(char[] buffer)
    {
        if (_usingDirectSlice && ReferenceEquals(_directBuffer, buffer))
        {
            EnsureLineBufferForField();
        }

        if (_directFieldCount == 0)
        {
            return this;
        }

        for (var i = 0; i < _fieldCount; i++)
        {
            var field = _fields[i];
            if (!ReferenceEquals(field.Array, buffer))
            {
                continue;
            }

            var canUseLineBuffer = !_fieldStarted;
            var fieldSpan = field.Span;
            var length = fieldSpan.Length;

            if (canUseLineBuffer)
            {
                EnsureLineCapacity(_lineLength + length);
                var copyStart = _lineLength;
                fieldSpan.CopyTo(_lineBuffer.AsSpan(copyStart));
                _lineLength += length;
                _fields[i] = field.WithArray(_lineBuffer, copyStart);
            }
            else
            {
                var copy = new char[length];
                fieldSpan.CopyTo(copy);
                _fields[i] = field.WithArray(copy, 0);
            }

            _directFieldCount--;
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

        // A direct slice always holds the whole field so far; earlier fields may have been
        // copied into the line buffer since it was set, so the field starts here.
        _fieldStart = _lineLength;
        _fieldStarted = true;

        AppendLineSpan(new ReadOnlySpan<char>(_directBuffer, _directStart, _directLength));
        _usingDirectSlice = false;
        _directBuffer = null;
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
        // Fields already recorded for this line keep pointing at the old array. That is safe:
        // Array.Resize copies into a new array and the old one is never written again.
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

    private static void Trim(char[] buffer, ref int start, ref int length)
    {
        var end = start + length - 1;
        while (start <= end && char.IsWhiteSpace(buffer[start])) start++;
        while (end >= start && char.IsWhiteSpace(buffer[end])) end--;
        length = end < start ? 0 : end - start + 1;
    }
}

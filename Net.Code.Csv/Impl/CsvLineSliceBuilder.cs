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

    private readonly List<FieldSliceInfo> _fields = [];
    private CsvField[] _fieldArray = new CsvField[16];
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

    public int FieldsCount => _fields.Count;

    internal bool HasTentative => _tentativeLength > 0;

    internal CsvLineSliceBuilder PrepareNextLine()
    {
        _fields.Clear();
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

            _fields.Add(new FieldSliceInfo(_directBuffer, start, length, false, false));
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

        _fields.Add(new FieldSliceInfo(null, fieldStart, fieldLength, false, true));
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
                throw new MissingFieldCsvException(reader.GetRawData(rawEnd), Location, count);
            }

            if (behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull)
            {
                for (var i = 0; i < missingCount; i++)
                {
                    fields.Add(new FieldSliceInfo(null, 0, 0, true, true));
                }
            }
            else
            {
                for (var i = 0; i < missingCount; i++)
                {
                    fields.Add(new FieldSliceInfo(null, 0, 0, false, true));
                }
            }
        }

        var totalCount = fields.Count;
        if (totalCount == 0)
        {
            var emptyLine = new CsvLineSlice([], 0, lineIsEmpty, false);
            PrepareNextLine();
            return emptyLine;
        }

        // The field array is reused for every line: a line is only valid until the next MoveNext,
        // and lines that must outlive it are copied via CsvLineSlice.ToOwned.
        if (_fieldArray.Length < totalCount)
        {
            _fieldArray = new CsvField[Math.Max(totalCount, _fieldArray.Length * 2)];
        }
        var fieldArray = _fieldArray;
        for (var i = 0; i < totalCount; i++)
        {
            var field = fields[i];
            fieldArray[i] = field.IsNull
                ? CsvField.FromString(null)
                : CsvField.FromArray(field.UseLineBuffer ? _lineBuffer : field.Buffer, field.Start, field.Length);
        }

        var line = new CsvLineSlice(fieldArray, totalCount, lineIsEmpty, false);

        PrepareNextLine();

        return line;
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

            if (!ReferenceEquals(field.Buffer, buffer))
            {
                continue;
            }

            var canUseLineBuffer = !_fieldStarted;
            var fieldSpan = new ReadOnlySpan<char>(field.Buffer, field.Start, field.Length);
            var length = fieldSpan.Length;

            if (canUseLineBuffer)
            {
                EnsureLineCapacity(_lineLength + length);
                var copyStart = _lineLength;
                fieldSpan.CopyTo(_lineBuffer.AsSpan(copyStart));
                _lineLength += length;
                _fields[i] = new FieldSliceInfo(null, copyStart, length, field.IsNull, true);
            }
            else
            {
                var copy = new char[length];
                fieldSpan.CopyTo(copy);
                _fields[i] = new FieldSliceInfo(copy, 0, length, field.IsNull, false);
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

    private readonly record struct FieldSliceInfo(char[] Buffer, int Start, int Length, bool IsNull, bool UseLineBuffer);
}

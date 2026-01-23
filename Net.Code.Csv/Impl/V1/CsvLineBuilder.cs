namespace Net.Code.Csv.Impl.V1;
internal class CsvLineBuilder(CsvLayout layout, CsvBehaviour behaviour)
{
    private char _currentChar;
    private char? _next;
    private bool _quoted;
    private Location _location = Location.Origin().NextLine();

    private const int RawWindowSize = 32;

    private readonly StringBuilder _field = new();
    private readonly StringBuilder _tentative = new();
    private readonly List<string> _fields = [];
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

    public CsvChar CurrentChar => new (_currentChar, layout, _next);

    public List<string> Fields => _fields;

    public Location Location => _location;

    public int? FieldCount { get; set; }

    internal CsvLineBuilder PrepareNextLine()
    {
        _fields.Clear();
        _field.Clear();
        _tentative.Clear();
        _quoted = false;
        _location = _location.NextLine();
        return this;
    }

    internal CsvLineBuilder AddToField()
    {
        _field.Append(_currentChar);
        return this;
    }

    internal CsvLineBuilder AcceptTentative()
    {
        _field.Append(_tentative);
        _tentative.Clear();
        return this;
    }

    internal CsvLineBuilder AddToTentative()
    {
        _tentative.Append(_currentChar);
        return this;
    }

    internal CsvLineBuilder DiscardTentative()
    {
        _tentative.Clear();
        return this;
    }

    internal CsvLineBuilder NextField()
    {
        var result = behaviour.TrimmingOptions switch
        {
            ValueTrimmingOptions.All => _field.Trim(),
            ValueTrimmingOptions.QuotedOnly when _quoted => _field.Trim(),
            ValueTrimmingOptions.UnquotedOnly when !_quoted => _field.Trim(),
            _ => _field.ToString()
        };

        _field.Clear();
        _fields.Add(result);
        _quoted = false;
        return this;
    }

    internal CsvLineBuilder MarkQuoted()
    {
        _quoted = true;
        return this;
    }

    /// <summary>
    /// Generate a CsvLineSlice and prepare for next
    /// </summary>
    internal CsvLineSlice ToLine()
    {
        var lineIsEmpty = _fields is [] or [""];
        var count = _fields.Count;

        if (!FieldCount.HasValue)
        {
            // this is either the first line, or previous lines where empty and should be skipped
            if (!(lineIsEmpty && behaviour.EmptyLineAction == EmptyLineAction.Skip))
            {
                FieldCount = count;
            }
        }

        bool fieldsAreMissing = count < FieldCount;
        var f = (fieldsAreMissing, lineIsEmpty, behaviour.MissingFieldAction) switch
        {
            // throw an error if the line is not empty and has less fields than the header
            // do not throw an error, but add empty fields if the line *is* empty 
            (fieldsAreMissing: true, lineIsEmpty: false, MissingFieldAction.ParseError) => throw new MissingFieldCsvException(RawData, Location, count),
            (fieldsAreMissing: true, lineIsEmpty: true, MissingFieldAction.ParseError) => _fields.Concat(Enumerable.Repeat(string.Empty, FieldCount.Value - count)),
            (fieldsAreMissing: true, lineIsEmpty: _, MissingFieldAction.ReplaceByEmpty) => _fields.Concat(Enumerable.Repeat(string.Empty, FieldCount.Value - count)),
            (fieldsAreMissing: true, lineIsEmpty: _, MissingFieldAction.ReplaceByNull) => _fields.Concat(Enumerable.Repeat(default(string), FieldCount.Value - count)),
            // no fields missing => return the fields
            _ => _fields
        };

        var line = new CsvLineSlice(f.Select(CsvField.FromString).ToArray(), lineIsEmpty);

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

    internal CsvLineBuilder Ignore() => this;

    private void AppendRaw(char currentChar)
    {
        _rawBuffer[_rawBufferIndex] = currentChar;
        _rawBufferIndex = (_rawBufferIndex + 1) % RawWindowSize;
        if (_rawBufferCount < RawWindowSize)
        {
            _rawBufferCount++;
        }
    }
}

namespace Net.Code.Csv.Impl;
internal class CsvLineBuilder(CsvLayout layout, CsvBehaviour behaviour)
{
    private char _currentChar;
    private char? _next;
    private bool _quoted;
    private Location _location = Location.Origin().NextLine();

    private readonly StringBuilder _field = new();
    private readonly StringBuilder _tentative = new();
    private readonly StringBuilder _rawData = new();
    private readonly List<string> _fields = [];

    public string RawData => _rawData.ToString();

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
    /// Generate a CsvLine and prepare for next
    /// </summary>
    /// <returns></returns>
    internal CsvLine ToLine()
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

        var line = new CsvLine([.. f], lineIsEmpty);

        PrepareNextLine();

        return line;
    }

    internal bool ReadNext(TextReader textReader)
    {
        var i = textReader.Read();
        if (i < 0) return false;
        var currentChar = (char)i;
        var peek = textReader.Peek();
        _next = peek < 0 ? null : (char?)peek;
        _location = _location.NextColumn();
        _currentChar = currentChar;
        _rawData.Append(currentChar);
        if (_rawData.Length > 32) _rawData.Remove(0, 1);
        return true;
    }

    internal CsvLineBuilder Ignore() => this;
}

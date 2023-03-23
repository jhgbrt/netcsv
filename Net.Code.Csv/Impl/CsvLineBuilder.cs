using System.Diagnostics;

namespace Net.Code.Csv.Impl;
internal class CsvLineBuilder
{
    private CsvChar _currentChar;
    private bool _quoted;
    private Location _location = Location.Origin().NextLine();

    private readonly StringBuilder _field = new();
    private readonly StringBuilder _tentative = new();
    private readonly StringBuilder _rawData = new();
    private readonly List<string> _fields = new();

    private readonly CsvLayout _layout;
    private readonly CsvBehaviour _behaviour;
    public string RawData => _rawData.ToString();

    public CsvChar CurrentChar => _currentChar;

    public List<string> Fields => _fields;

    public Location Location => _location;

    public int? FieldCount { get; set; }

    public CsvLineBuilder(CsvLayout layout, CsvBehaviour behaviour)
    {
        _layout = layout;
        _behaviour = behaviour;
    }

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
        _field.Append(_currentChar.Value);
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
        _tentative.Append(_currentChar.Value);
        return this;
    }

    internal CsvLineBuilder DiscardTentative()
    {
        _tentative.Clear();
        return this;
    }

    internal CsvLineBuilder NextField()
    {
        var result = _behaviour.TrimmingOptions switch
        {
            ValueTrimmingOptions.All => _field.Trim(),
            ValueTrimmingOptions.QuotedOnly when _quoted => _field.Trim(),
            ValueTrimmingOptions.UnquotedOnly when !_quoted=> _field.Trim(),
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
        var isEmpty = _fields.Count == 0 || (_fields.Count == 1 && string.IsNullOrEmpty(_fields[0]));

        if (!FieldCount.HasValue && (!isEmpty || _behaviour.EmptyLineAction != EmptyLineAction.Skip))
        {
            FieldCount = _fields.Count;
        }


        var count = _fields.Count;
        if (!isEmpty && count < FieldCount && _behaviour.MissingFieldAction == MissingFieldAction.ParseError)
        {
            throw new MissingFieldCsvException(RawData, Location, _fields.Count);
        }

        if (FieldCount.HasValue && isEmpty && _behaviour.EmptyLineAction == EmptyLineAction.NextResult)
        {
        }
        else if (count < FieldCount)
        {
            var s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
            _fields.AddRange(Enumerable.Repeat(s, FieldCount.Value - count));
        }

        var line = new CsvLine(_fields.ToArray(), isEmpty);

        PrepareNextLine();

        return line;
    }

    internal bool ReadNext(TextReader textReader)
    {
        var i = textReader.Read();
        if (i < 0) return false;
        var currentChar = (char)i;
        var peek = textReader.Peek();
        var next = peek < 0 ? null : (char?)peek;
        _location = _location.NextColumn();
        _currentChar = new CsvChar(currentChar, _layout, next);
        _rawData.Append(currentChar);
        if (_rawData.Length > 32) _rawData.Remove(0, 1);
        return true;
    }

    internal CsvLineBuilder Ignore() => this;
}

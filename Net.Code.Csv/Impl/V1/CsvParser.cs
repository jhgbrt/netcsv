namespace Net.Code.Csv.Impl;

// The parser takes care of processing the actual content of the file
// It does not take into account schema information, so it only works with strings
class CsvParser : ICsvParser, IEnumerable<CsvLineSlice>, IDisposable
{
    private readonly CsvStateMachine _csvStateMachine;
    private bool _disposed;
    private readonly IDisposable _textReader;

    public CsvParser(TextReader textReader, BufferedCharReader bufferedReader, CsvLayout layOut, CsvBehaviour behaviour)
    {
        _csvStateMachine = new CsvStateMachine(bufferedReader, layOut, behaviour);
        _enumerator = _csvStateMachine.Lines().GetEnumerator();
        _textReader = textReader;

        CsvLineSlice? firstLine = ReadFirstLine();

        Header = (layOut, firstLine) switch
        {
            ({ HasHeaders: true }, firstLine: not null)
                => CsvHeader.Create(firstLine.Value.GetStrings()),

            ({ HasHeaders: false }, firstLine: not null)
                => CsvHeader.Default(firstLine.Value.Length),

            (_, firstLine: null)
                => CsvHeader.None
        };

        if (!layOut.HasHeaders)
            _cachedLine = firstLine;
    }

    private CsvLineSlice? _cachedLine;

    private readonly IEnumerator<CsvLineSlice> _enumerator;

    public CsvHeader Header { get; }

    public int FieldCount => _csvStateMachine.FieldCount ?? -1;

    private IEnumerable<CsvLineSlice> Lines()
    {
        if (_cachedLine != null)
        {
            yield return _cachedLine.Value;
            _cachedLine = null;
        }

        while (_enumerator.MoveNext())
        {
            var readLine = _enumerator.Current;

            yield return readLine;
        }
    }

    public IEnumerator<CsvLineSlice> GetEnumerator() => Lines().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _textReader.Dispose();
        _disposed = true;
    }

    private CsvLineSlice? ReadFirstLine()
    {
        using var enumerator = Lines().GetEnumerator();
        if (enumerator.MoveNext())
        {
            return enumerator.Current;
        }
        return null;
    }
}

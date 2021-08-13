namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV header line
/// </summary>
record CsvHeader : CsvLine
{
    private readonly IReadOnlyDictionary<string, int> _fieldHeaderIndexes;

    public CsvHeader(string[] fields)
        : base(DefaultWhereEmpty(fields).ToArray(), false)
        => _fieldHeaderIndexes = Fields.WithIndex().ToDictionary(x => x.item, x => x.index);

    private static IEnumerable<string> DefaultWhereEmpty(IEnumerable<string> fields)
        => fields.Select((f, i) => string.IsNullOrWhiteSpace(f) ? $"Column{i}" : f);

    public int this[string headerName] => _fieldHeaderIndexes[headerName];

    public bool TryGetIndex(string name, out int index) => _fieldHeaderIndexes.TryGetValue(name, out index);
}

static class EnumerableEx
{
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> input) => input.Select((t, i) => (t, i));
}

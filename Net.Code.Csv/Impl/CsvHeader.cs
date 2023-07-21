namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV header line
/// </summary>
record struct CsvHeader(string[] Fields)
{
    public static CsvHeader None = new(Array.Empty<string>());
    public static CsvHeader Default(int length) => new(Enumerable.Range(0, length).Select(i => $"Column{i}").ToArray());
    public static CsvHeader Create(string[] names) => new(names.Select((f, i) => string.IsNullOrWhiteSpace(f) ? $"Column{i}" : f).ToArray());

    public override string ToString() => string.Join(";", Fields);
    public readonly int Length => Fields.Length;
    public readonly string this[int field] => Fields[field];

    public bool TryGetIndex(string name, out int index)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        index = -1;
        foreach (var (f, i) in Fields.WithIndex())
        {
            if (f == name)
            {
                index = i;
                return true;
            }
        }
        return false;
    }
}

static class EnumerableEx
{
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> input) => input.Select((t, i) => (t, i));
}

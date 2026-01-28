namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV header line
/// </summary>
struct CsvHeader(string[] fields)
{
    public readonly string[] Fields => fields;
    public static CsvHeader None = new([]);
    public static CsvHeader Default(int length) => new(Enumerable.Range(0, length).Select(i => $"Column{i}").ToArray());
    public static CsvHeader Create(string[] names) => new(names.Select((f, i) => string.IsNullOrWhiteSpace(f) ? $"Column{i}" : f).ToArray());
    public static CsvHeader Create(CsvLineSlice line)
    {
        var count = line.Length;
        if (count == 0)
        {
            return None;
        }

        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            var value = line.GetString(i);
            names[i] = string.IsNullOrWhiteSpace(value) ? $"Column{i}" : value;
        }
        return new CsvHeader(names);
    }

    public readonly override string ToString() => string.Join(";", fields);
    public readonly int Length => fields.Length;
    public readonly string this[int field] => fields[field];

    public readonly bool TryGetIndex(string name, out int index)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        index = -1;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == name)
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

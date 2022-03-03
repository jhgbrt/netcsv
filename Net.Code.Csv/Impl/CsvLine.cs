namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV line
/// </summary>
record CsvLine(string[] Fields, bool IsEmpty)
{

    /// <summary>
    /// An empty CSV line
    /// </summary>
    public static readonly CsvLine Empty = new(Array.Empty<string>(), true);

    public override string ToString() => string.Join(";", Fields);
    public int Length => Fields.Length;
    public string this[int field]
    {
        get
        {
            if (field < Fields.Length)
            {
                return Fields[field];
            }

            if (IsEmpty)
            {
                return string.Empty;
            }

            throw new ArgumentOutOfRangeException(nameof(field));
        }
    }
}

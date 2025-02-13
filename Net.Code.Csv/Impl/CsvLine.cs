namespace Net.Code.Csv.Impl;

/// <summary>
/// A CSV line
/// </summary>
readonly struct CsvLine(string[] fields, bool isEmpty)
{

    /// <summary>
    /// An empty CSV line
    /// </summary>
    public static readonly CsvLine Empty = new([], true);

    public override string ToString() => string.Join(";", fields);
    public readonly int Length => fields.Length;
    public readonly string[] Fields => fields;
    public readonly bool IsEmpty => isEmpty;
    public readonly string this[int field]
    {
        get
        {
            if (field < fields.Length)
            {
                return fields[field];
            }

            if (isEmpty)
            {
                return string.Empty;
            }

            throw new ArgumentOutOfRangeException(nameof(field));
        }
    }
}

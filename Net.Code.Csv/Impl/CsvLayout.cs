namespace Net.Code.Csv.Impl;

/// <summary>
/// Describes a CSV file layout (quote character, delimiter, escape character, comment marker, does the CSV have headers or not)
/// </summary>
internal record CsvLayout(
            /// <summary>
            /// The character used as a field quote
            /// </summary>
            char Quote = '"',
            /// <summary>
            /// The character that delimits the fields
            /// </summary>
            char Delimiter = ',',
            /// <summary>
            /// The character to be used for escaping quotes inside a field
            /// </summary>
            char Escape = '"',
            /// <summary>
            /// The character that marks a line as a comment
            /// </summary>
            char Comment = '#',
            /// <summary>
            /// Indicates whether or not the input file has a header
            /// </summary>
            bool HasHeaders = false,
            /// <summary>
            /// Represents the schema of the file
            /// </summary>
            OneOf<CsvSchema, CsvSchema[]> Schema = default
    )

{
    /// <summary>
    /// The default CSV layout. Uses double quote, comma as separator,
    /// backslash as escape character, hash (#) as a comment marker and assumes no header.
    /// </summary>
    public static CsvLayout Default => new();

    public IEnumerable<CsvSchema> Schemas => Schema == null ? [] : Schema.Match(s => [s], s => s);

    internal bool IsEscape(char currentChar, char? nextChar)
        => currentChar == Escape && (nextChar == Quote || nextChar == Escape);
}

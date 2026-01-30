namespace Net.Code.Csv.Impl;

/// <summary>
/// represents a character in the CSV stream
/// </summary>
/// <param name="c">The last read character</param>
/// <param name="layout">the CSV layout parameters</param>
/// <param name="next">Look ahead character (if any). Required to determine if the current character is an escape character.</param>
readonly ref struct CsvChar(char value, CsvLayout layout, char? Next)
{
    public readonly bool IsCarriageReturn => value == '\r';
    public readonly bool IsNewLine => value == '\n';
    public readonly bool IsComment => value == layout.Comment;
    public readonly bool IsQuote => layout.Quote.HasValue && value == layout.Quote.Value;
    public readonly bool IsDelimiter => value == layout.Delimiter;
    public readonly bool IsWhiteSpace => char.IsWhiteSpace(value);
    public readonly bool IsEscape => layout.IsEscape(value, Next);
}

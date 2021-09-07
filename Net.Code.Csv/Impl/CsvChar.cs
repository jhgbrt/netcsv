namespace Net.Code.Csv.Impl;

/// <summary>
/// 
/// </summary>
/// <param name="c">The last read character</param>
/// <param name="layout">the CSV layout parameters</param>
/// <param name="next">Look ahead character (if any). Required to determine if the current character is an escape character.</param>
record struct CsvChar(char Value, CsvLayout Layout, char? Next)
{
    public bool IsNewLine => Value == '\r' || Value == '\n';
    public bool IsComment => Value == Layout.Comment;
    public bool IsQuote => Value == Layout.Quote;
    public bool IsDelimiter => Value == Layout.Delimiter;
    public bool IsWhiteSpace => char.IsWhiteSpace(Value);
    public bool IsEscape => Layout.IsEscape(Value, Next);
}

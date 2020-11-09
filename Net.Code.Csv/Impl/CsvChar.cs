namespace Net.Code.Csv.Impl
{
    struct CsvChar
    {
        public readonly char Value;
        private readonly char? _next;
        private readonly CsvLayout _layout;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c">The last read character</param>
        /// <param name="layout">the CSV layout parameters</param>
        /// <param name="next">Look ahead character (if any). Required to determine if the current character is an escape character.</param>
        public CsvChar(char c, CsvLayout layout, char? next)
        {
            Value = c;
            _layout = layout;
            _next = next;
        }

        public bool IsNewLine => Value == '\r' || Value == '\n';
        public bool IsComment => Value == _layout.Comment;
        public bool IsQuote => Value == _layout.Quote;
        public bool IsDelimiter => Value == _layout.Delimiter;
        public bool IsWhiteSpace => char.IsWhiteSpace(Value);
        public bool IsEscape => _layout.IsEscape(Value, _next);
    }
}
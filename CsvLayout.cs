namespace Net.Code.Csv
{

    public class CsvBehaviour
    {
        public static CsvBehaviour Default { get { return new CsvBehaviour(); } }
        private readonly ValueTrimmingOptions _trimmingOptions;
        private readonly MissingFieldAction _missingFieldAction;
        private readonly bool _skipEmptyLines;

        public CsvBehaviour(
            ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
            MissingFieldAction missingFieldAction = MissingFieldAction.ParseError, 
            bool skipEmptyLines = true)
        {
            _trimmingOptions = trimmingOptions;
            _missingFieldAction = missingFieldAction;
            _skipEmptyLines = skipEmptyLines;
        }

        public ValueTrimmingOptions TrimmingOptions
        {
            get { return _trimmingOptions; }
        }

        public MissingFieldAction MissingFieldAction
        {
            get { return _missingFieldAction; }
        }

        public bool SkipEmptyLines
        {
            get { return _skipEmptyLines; }
        }

        public QuotesInsideQuotedFieldAction QuotesInsideQuotedFieldAction { get; set; }
    }

    public class CsvLayout
    {
        public static CsvLayout Default { get { return new CsvLayout(); } }

        private readonly char _quote;
        private readonly char _delimiter;
        private readonly char _escape;
        private readonly char _comment;
        private readonly bool _hasHeaders;


        public CsvLayout(
            char quote = '"',
            char delimiter = ',',
            char escape = '"',
            char comment = '#',
            bool hasHeaders = false)
        {
            _quote = quote;
            _delimiter = delimiter;
            _escape = escape;
            _comment = comment;
            _hasHeaders = hasHeaders;
        }

        public char Quote
        {
            get { return _quote; }
        }

        public char Delimiter
        {
            get { return _delimiter; }
        }

        public char Escape
        {
            get { return _escape; }
        }

        public char Comment
        {
            get { return _comment; }
        }

        public bool HasHeaders
        {
            get { return _hasHeaders; }
        }
    }
}
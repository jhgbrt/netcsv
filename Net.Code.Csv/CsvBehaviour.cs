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
}
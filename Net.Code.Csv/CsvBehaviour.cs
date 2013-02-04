namespace Net.Code.Csv
{
    /// <summary>
    /// Describes the way the CSV parser should behave
    /// </summary>
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

        /// <summary>
        /// How should fields be trimmed?
        /// </summary>
        public ValueTrimmingOptions TrimmingOptions
        {
            get { return _trimmingOptions; }
        }

        /// <summary>
        /// What should happen when a field is missing from a line?
        /// </summary>
        public MissingFieldAction MissingFieldAction
        {
            get { return _missingFieldAction; }
        }

        public bool SkipEmptyLines
        {
            get { return _skipEmptyLines; }
        }

        /// <summary>
        /// What should happen when a quote is found inside a quoted field? (e.g. "123","x y "z" u","345")
        /// </summary>
        public QuotesInsideQuotedFieldAction QuotesInsideQuotedFieldAction { get; set; }
    }
}
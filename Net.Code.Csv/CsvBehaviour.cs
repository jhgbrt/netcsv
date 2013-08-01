namespace Net.Code.Csv
{
    /// <summary>
    /// Describes the way the CSV parser should behave
    /// </summary>
    public class CsvBehaviour
    {
        /// <summary>
        /// The default behaviour of the Csv parser: trim unquoted fields,
        /// throw exception when a line contains too little fields, 
        /// skip empty lines and ignore quotes inside quoted fields.
        /// </summary>
        public static CsvBehaviour Default { get { return new CsvBehaviour(); } }

        private readonly ValueTrimmingOptions _trimmingOptions;
        private readonly MissingFieldAction _missingFieldAction;
        private readonly bool _skipEmptyLines;
        private readonly QuotesInsideQuotedFieldAction _quotesInsideQuotedFieldAction;

        /// <summary>
        /// Constructs a CsvBehaviour instance that can be used to drive the csv parser
        /// </summary>
        /// <param name="trimmingOptions">How should fields be trimmed?</param>
        /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
        /// <param name="skipEmptyLines">Should empty lines be skipped?</param>
        /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
        public CsvBehaviour(
            ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
            MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
            bool skipEmptyLines = true,
            QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore)
        {
            _trimmingOptions = trimmingOptions;
            _missingFieldAction = missingFieldAction;
            _skipEmptyLines = skipEmptyLines;
            _quotesInsideQuotedFieldAction = quotesInsideQuotedFieldAction;
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

        /// <summary>
        /// Should empty lines be skipped?
        /// </summary>
        public bool SkipEmptyLines
        {
            get { return _skipEmptyLines; }
        }

        /// <summary>
        /// What should happen when a quote is found inside a quoted field? (e.g. "123","x y "z" u","345")
        /// </summary>
        public QuotesInsideQuotedFieldAction QuotesInsideQuotedFieldAction
        {
            get { return _quotesInsideQuotedFieldAction; }
        }
    }
}
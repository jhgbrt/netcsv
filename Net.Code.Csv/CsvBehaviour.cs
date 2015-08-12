namespace Net.Code.Csv
{
    public struct CsvOptions
    {
    }

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
        public static CsvBehaviour Default => new CsvBehaviour();

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
            TrimmingOptions = trimmingOptions;
            MissingFieldAction = missingFieldAction;
            SkipEmptyLines = skipEmptyLines;
            QuotesInsideQuotedFieldAction = quotesInsideQuotedFieldAction;
        }

        /// <summary>
        /// How should fields be trimmed?
        /// </summary>
        public ValueTrimmingOptions TrimmingOptions { get; }

        /// <summary>
        /// What should happen when a field is missing from a line?
        /// </summary>
        public MissingFieldAction MissingFieldAction { get; }

        /// <summary>
        /// Should empty lines be skipped?
        /// </summary>
        public bool SkipEmptyLines { get; }

        /// <summary>
        /// What should happen when a quote is found inside a quoted field? (e.g. "123","x y "z" u","345")
        /// </summary>
        public QuotesInsideQuotedFieldAction QuotesInsideQuotedFieldAction { get; }
    }
}
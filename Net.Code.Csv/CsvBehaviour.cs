namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
namespace Net.Code.Csv
{
    /// <summary>
    /// Describes the way the CSV parser should behave
    /// </summary>
    public record CsvBehaviour(
            /// <summary>
            /// How should fields be trimmed?
            /// </summary>
            ValueTrimmingOptions TrimmingOptions = ValueTrimmingOptions.UnquotedOnly,
            /// <summary>
            /// What should happen when a field is missing from a line?
            /// </summary>
            MissingFieldAction MissingFieldAction = MissingFieldAction.ParseError,
            /// <summary>
            /// Should empty lines be skipped?
            /// </summary>
            bool SkipEmptyLines = true,
            /// <summary>
            /// What should happen when a quote is found inside a quoted field? (e.g. "123","x y "z" u","345")
            /// </summary>
            QuotesInsideQuotedFieldAction QuotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore)
    {
        /// <summary>
        /// The default behaviour of the Csv parser: trim unquoted fields,
        /// throw exception when a line contains too little fields, 
        /// skip empty lines and ignore quotes inside quoted fields.
        /// </summary>
        public static CsvBehaviour Default => new CsvBehaviour();

        internal bool ShouldTrim(bool quoted)
            => (TrimmingOptions == ValueTrimmingOptions.All)
            || (quoted && TrimmingOptions == ValueTrimmingOptions.QuotedOnly)
            || (!quoted && TrimmingOptions == ValueTrimmingOptions.UnquotedOnly);
    }
}
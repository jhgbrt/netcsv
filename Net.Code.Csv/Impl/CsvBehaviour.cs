namespace Net.Code.Csv.Impl;

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
            EmptyLineAction EmptyLineAction = EmptyLineAction.Skip,
            /// <summary>
            /// What should happen when a quote is found inside a quoted field? (e.g. "123","x y "z" u","345")
            /// </summary>
            QuotesInsideQuotedFieldAction QuotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
#pragma warning disable CS0618 // CsvParserKind is obsolete; kept so existing callers still compile.
            /// <summary>
            /// Has no effect: there is only one parser.
            /// </summary>
            [property: Obsolete("There is only one parser; this setting has no effect and will be removed in a future major version.")]
            CsvParserKind Parser = CsvParserKind.Default)
#pragma warning restore CS0618
{
    /// <summary>
    /// Enables strict parsing rules.
    /// </summary>
    internal bool StrictMode { get; init; }

    /// <summary>
    /// The default behaviour of the Csv parser: trim unquoted fields,
    /// throw exception when a line contains too little fields, 
    /// skip empty lines and ignore quotes inside quoted fields.
    /// </summary>
    public static CsvBehaviour Default => new();

    /// <summary>
    /// Strict, fail-fast parsing. When TrimmingOptions is All,
    /// whitespace outside quoted fields is treated as an error.
    /// </summary>
    public static CsvBehaviour Strict()
        => new CsvBehaviour(
            TrimmingOptions: ValueTrimmingOptions.None,
            MissingFieldAction: MissingFieldAction.ParseError,
            EmptyLineAction: EmptyLineAction.Skip,
            QuotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.ThrowException)
        {
            StrictMode = true
        };

    /// <summary>
    /// Forgiving parsing. Quotes are treated as normal characters when disabled via CsvLayout.
    /// </summary>
    public static CsvBehaviour Forgiving()
        => new CsvBehaviour(
            TrimmingOptions: ValueTrimmingOptions.None,
            MissingFieldAction: MissingFieldAction.ParseError,
            EmptyLineAction: EmptyLineAction.Skip,
            QuotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.Ignore);
}

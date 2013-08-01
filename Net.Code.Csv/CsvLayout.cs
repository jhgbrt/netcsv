namespace Net.Code.Csv
{
    /// <summary>
    /// Describes a CSV file layout (quote character, delimiter, escape character, comment marker, does the CSV have headers or not)
    /// </summary>
    public class CsvLayout
    {
        /// <summary>
        /// The default CSV layout. Uses double quote, comma as separator,
        /// backslash as escape character, hash (#) as a comment marker and assumes no header.
        /// </summary>
        public static CsvLayout Default { get { return new CsvLayout(); } }

        private readonly char _quote;
        private readonly char _delimiter;
        private readonly char _escape;
        private readonly char _comment;
        private readonly bool _hasHeaders;


        /// <summary>
        /// Describes a CSV file layout
        /// </summary>
        /// <param name="quote">The quote character. Default '"'</param>
        /// <param name="delimiter">Field delimiter. Default ','</param>
        /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
        /// <param name="comment">Comment marker. Default '#'</param>
        /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
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

        /// <summary>
        /// The character used as a field quote
        /// </summary>
        public char Quote
        {
            get { return _quote; }
        }

        /// <summary>
        /// The character that delimits the fields
        /// </summary>
        public char Delimiter
        {
            get { return _delimiter; }
        }

        /// <summary>
        /// The character to be used for escaping quotes inside a field
        /// </summary>
        public char Escape
        {
            get { return _escape; }
        }

        /// <summary>
        /// The character that marks a line as a comment
        /// </summary>
        public char Comment
        {
            get { return _comment; }
        }

        /// <summary>
        /// Indicates whether or not the input file has a header
        /// </summary>
        public bool HasHeaders
        {
            get { return _hasHeaders; }
        }
    }
}
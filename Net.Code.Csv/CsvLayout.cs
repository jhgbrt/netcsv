namespace Net.Code.Csv
{
    /// <summary>
    /// Describes a CSV file layout (quote character, delimiter, escape character, comment marker, does the CSV have headers or not)
    /// </summary>
    public class CsvLayout
    {
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
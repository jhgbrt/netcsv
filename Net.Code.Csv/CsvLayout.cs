using System;
using Net.Code.Csv.Impl;

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
        public static CsvLayout Default => new CsvLayout();


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
            Quote = quote;
            Delimiter = delimiter;
            Escape = escape;
            Comment = comment;
            HasHeaders = hasHeaders;
        }

        /// <summary>
        /// The character used as a field quote
        /// </summary>
        public char Quote { get; }

        /// <summary>
        /// The character that delimits the fields
        /// </summary>
        public char Delimiter { get; }

        /// <summary>
        /// The character to be used for escaping quotes inside a field
        /// </summary>
        public char Escape { get; }

        /// <summary>
        /// The character that marks a line as a comment
        /// </summary>
        public char Comment { get; }

        /// <summary>
        /// Indicates whether or not the input file has a header
        /// </summary>
        public bool HasHeaders { get; }

        internal bool IsEscape(char currentChar, char? nextChar) => currentChar == Escape && (nextChar == Quote || nextChar == Escape);
    }
}
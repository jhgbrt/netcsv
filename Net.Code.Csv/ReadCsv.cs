using Net.Code.Csv.Impl;
using System.Data;
using System.IO;
using System.Text;

namespace Net.Code.Csv
{
    public static class ReadCsv
    {
        /// <summary>
        /// Read a file as CSV, using specific behaviour, layout and conversion options. Make sure to dispose the DataReader.
        /// </summary>
        /// <param name="path">The full or relative path name</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <param name="quote">The quote character. Default '"'</param>
        /// <param name="delimiter">Field delimiter. Default ','</param>
        /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
        /// <param name="comment">Comment marker. Default '#'</param>
        /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
        /// <param name="trimmingOptions">How should fields be trimmed?</param>
        /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
        /// <param name="skipEmptyLines">Should empty lines be skipped?</param>
        /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader). When none is specified, System.Convert is used.</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a DataReader instance to read the contents of the CSV file</returns>
        public static IDataReader FromFile(
            string path,
            Encoding encoding = null,
            char quote = '"',
            char delimiter = ',',
            char escape = '"',
            char comment = '#',
            bool hasHeaders = false,
            ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
            MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
            bool skipEmptyLines = true,
            QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
            IConverter converter = null,
            int bufferSize = 4096)
        {
            // caller should dispose IDataReader, which will indirectly also close the stream
            var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders);
            var behaviour = new CsvBehaviour(trimmingOptions, missingFieldAction, skipEmptyLines, quotesInsideQuotedFieldAction);
            var stream = File.OpenRead(path);
            var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, encoding == null);
            return FromReader(reader, layout, behaviour, converter ?? Converter.Default, bufferSize);
        }

        /// <summary>
        /// Read a stream as CSV, using specific behaviour, layout and conversion options.
        /// The stream will not be disposed by disposing the data reader.
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <param name="encoding">The encoding. Default is UTF8.</param>
        /// <param name="quote">The quote character. Default '"'</param>
        /// <param name="delimiter">Field delimiter. Default ','</param>
        /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
        /// <param name="comment">Comment marker. Default '#'</param>
        /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
        /// <param name="trimmingOptions">How should fields be trimmed?</param>
        /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
        /// <param name="skipEmptyLines">Should empty lines be skipped?</param>
        /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader)</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a DataReader instance to read the contents of the CSV file</returns>
        public static IDataReader FromStream(
                Stream stream,
                Encoding encoding = null,
                char quote = '"',
                char delimiter = ',',
                char escape = '"',
                char comment = '#',
                bool hasHeaders = false,
                ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
                MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
                bool skipEmptyLines = true,
                QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
                IConverter converter = null,
                int bufferSize = 4096)
        {
            var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, encoding == null, 1024, true);
            var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders);
            var behaviour = new CsvBehaviour(trimmingOptions, missingFieldAction, skipEmptyLines, quotesInsideQuotedFieldAction);
            return FromReader(reader, layout, behaviour, converter ?? Converter.Default, bufferSize);
        }

        /// <summary>
        /// Read a string as CSV, using specific behaviour, layout and conversion options 
        /// </summary>
        /// <param name="input">The CSV input</param>
        /// <param name="quote">The quote character. Default '"'</param>
        /// <param name="delimiter">Field delimiter. Default ','</param>
        /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
        /// <param name="comment">Comment marker. Default '#'</param>
        /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
        /// <param name="trimmingOptions">How should fields be trimmed?</param>
        /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
        /// <param name="skipEmptyLines">Should empty lines be skipped?</param>
        /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a DataReader instance to read the contents of the CSV file</returns>
        public static IDataReader FromString(
            string input,
            char quote = '"',
            char delimiter = ',',
            char escape = '"',
            char comment = '#',
            bool hasHeaders = false,
            ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
            MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
            bool skipEmptyLines = true,
            QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
            IConverter converter = null, int bufferSize = 4096)
        {
            var reader = new StringReader(input);
            var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders);
            var behaviour = new CsvBehaviour(trimmingOptions, missingFieldAction, skipEmptyLines, quotesInsideQuotedFieldAction);
            return FromReader(reader, layout, behaviour, converter ?? Converter.Default, bufferSize);
        }


        internal static IDataReader FromReader(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize) => new CsvDataReader(reader, csvLayout, csvBehaviour, converter);
    }
}
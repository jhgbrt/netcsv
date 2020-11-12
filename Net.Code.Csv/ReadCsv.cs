using Net.Code.Csv.Impl;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        /// <param name="schema">The CSV schema.</param>
        /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
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
            CsvSchema schema = null,
            CultureInfo cultureInfo = null)
        {
            // caller should dispose IDataReader, which will indirectly also close the stream
            var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
            var behaviour = new CsvBehaviour(trimmingOptions, missingFieldAction, skipEmptyLines, quotesInsideQuotedFieldAction);
            var stream = File.OpenRead(path);
            var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, encoding == null);
            return FromReader(reader, layout, behaviour, cultureInfo);
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
        /// <param name="schema">The CSV schema.</param>
        /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
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
                CsvSchema schema = null,
                CultureInfo cultureInfo = null)
        {
            var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, encoding == null, 1024, true);
            var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
            var behaviour = new CsvBehaviour(trimmingOptions, missingFieldAction, skipEmptyLines, quotesInsideQuotedFieldAction);
            return FromReader(reader, layout, behaviour, cultureInfo);
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
        /// <param name="schema">The CSV schema.</param>
        /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
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
            CsvSchema schema = null,
            CultureInfo cultureInfo = null)
        {
            var reader = new StringReader(input);
            var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
            var behaviour = new CsvBehaviour(trimmingOptions, missingFieldAction, skipEmptyLines, quotesInsideQuotedFieldAction);
            return FromReader(reader, layout, behaviour, cultureInfo);
        }

        /// <summary>
        /// Read a file as CSV, using specific behaviour, layout and conversion options. 
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
        /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
        /// <returns>a DataReader instance to read the contents of the CSV file</returns>
        public static IEnumerable<T> FromFile<T>(
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
            CultureInfo cultureInfo = null)
        {
            var schema = new CsvSchemaBuilder(cultureInfo).From<T>().Schema;
            return FromFile(
                    path,
                    encoding,
                    quote,
                    delimiter,
                    escape,
                    comment,
                    hasHeaders,
                    trimmingOptions,
                    missingFieldAction,
                    skipEmptyLines,
                    quotesInsideQuotedFieldAction,
                    schema,
                    cultureInfo)
                .AsEnumerable<T>();
        }

        /// <summary>
        /// Read a file as CSV, using specific behaviour, layout and conversion options.
        /// Deserializes each record into an instance of <typeparamref name="T"/>
        /// </summary>
        /// <param name="stream">The stream to process</param>
        /// <param name="encoding">The encoding to use (default UTF-8)</param>
        /// <param name="quote">The quote character. Default '"'</param>
        /// <param name="delimiter">Field delimiter. Default ','</param>
        /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
        /// <param name="comment">Comment marker. Default '#'</param>
        /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
        /// <param name="trimmingOptions">How should fields be trimmed?</param>
        /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
        /// <param name="skipEmptyLines">Should empty lines be skipped?</param>
        /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
        /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
        /// <returns>a DataReader instance to read the contents of the CSV file</returns>
        public static IEnumerable<T> FromStream<T>(
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
            CultureInfo cultureInfo = null)
        {
            var schema = new CsvSchemaBuilder(cultureInfo).From<T>().Schema;
            return FromStream(
                    stream,
                    encoding,
                    quote,
                    delimiter,
                    escape,
                    comment,
                    hasHeaders,
                    trimmingOptions,
                    missingFieldAction,
                    skipEmptyLines,
                    quotesInsideQuotedFieldAction,
                    schema,
                    cultureInfo)
                .AsEnumerable<T>();
        }
        /// <summary>
        /// Read a file as CSV, using specific behaviour, layout and conversion options. 
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
        /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
        /// <returns>a DataReader instance to read the contents of the CSV file</returns>
        public static IEnumerable<T> FromString<T>(
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
            CultureInfo cultureInfo = null)
        {
            var schema = new CsvSchemaBuilder(cultureInfo).From<T>().Schema;
            return FromString(
                    input,
                    quote,
                    delimiter,
                    escape,
                    comment,
                    hasHeaders,
                    trimmingOptions,
                    missingFieldAction,
                    skipEmptyLines,
                    quotesInsideQuotedFieldAction,
                    schema,
                    cultureInfo)
                .AsEnumerable<T>();
        }

        internal static IDataReader FromReader(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, CultureInfo cultureInfo = null) 
            => new CsvDataReader(reader, csvLayout, csvBehaviour, cultureInfo);
    }
}
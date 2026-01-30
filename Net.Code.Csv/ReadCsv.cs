using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using Net.Code.Csv.Impl;
using Ude;

namespace Net.Code.Csv;

public static class ReadCsv
{
    private static Encoding DetectEncoding(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        if (!stream.CanSeek)
            return Encoding.UTF8;

        var detector = new CharsetDetector();
        var startPos = stream.Position;
        var buffer = new byte[16 * 1024]; // Read up to 16KB for detection
        
        try
        {
            int read;
            int totalRead = 0;
            
            // Only read up to buffer size to avoid loading too much data
            while ((read = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
            {
                totalRead += read;
                if (totalRead >= buffer.Length)
                    break;
            }
            
            if (totalRead > 0)
            {
                detector.Feed(buffer, 0, totalRead);
                detector.DataEnd();

                if (string.IsNullOrWhiteSpace(detector.Charset)) return Encoding.UTF8;
                try
                {
                    return Encoding.GetEncoding(detector.Charset);
                }
                catch (Exception e) when (e is ArgumentException or NotSupportedException)
                {
                    return Encoding.UTF8;
                }
            }
        }
        finally
        {
            stream.Position = startPos; // Reset position to beginning of file
        }

        return Encoding.UTF8; // Default to UTF8 if detection fails
    }

    /// <summary>
    /// Read a file as CSV, using specific behaviour, layout and conversion options. Make sure to dispose the DataReader.
    /// </summary>
    /// <param name="path">The full or relative path name</param>
    /// <param name="encoding">The encoding of the file.</param>
    /// <param name="quote">The quote character. Set to null to disable quoting. Default '"'</param>
    /// <param name="delimiter">Field delimiter. Default ','</param>
    /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
    /// <param name="comment">Comment marker. Set to null to disable comments. Default '#'</param>
    /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
    /// <param name="trimmingOptions">How should fields be trimmed?</param>
    /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
    /// <param name="emptyLineAction">What should happen when an empty line is encountered?</param>
    /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
    /// <param name="schema">The CSV schema (or schema's, if the file contains multiple result sets).</param>
    /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
    /// <param name="behaviour">Optional behaviour override. If provided, trimming/missing/empty/quotesInside parameters are ignored.</param>
    /// <returns>a DataReader instance to read the contents of the CSV file</returns>
    public static IDataReader FromFile(
        string path,
        Encoding encoding = null,
        char? quote = '"',
        char delimiter = ',',
        char escape = '"',
        char? comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        EmptyLineAction emptyLineAction = EmptyLineAction.Skip,
        QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        OneOf<CsvSchema,CsvSchema[]> schema = null,
        CultureInfo cultureInfo = null,
        CsvBehaviour behaviour = null)
    {
        var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
        var effectiveBehaviour = behaviour ?? new CsvBehaviour(trimmingOptions, missingFieldAction, emptyLineAction, quotesInsideQuotedFieldAction);
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);
        
        var effectiveEncoding = encoding;
        var detectBom = false;
        
        if (effectiveEncoding == null) 
        {
            effectiveEncoding = DetectEncoding(stream);
            detectBom = true;
        }
        else if (effectiveEncoding == Encoding.UTF8)
        {
            detectBom = true;
        }
        
        var reader = new StreamReader(stream, effectiveEncoding, detectBom, bufferSize: 32 * 1024);
        return FromReader(reader, layout, effectiveBehaviour, cultureInfo);
    }

    /// <summary>
    /// Read a stream as CSV, using specific behaviour, layout and conversion options.
    /// The stream will not be disposed by disposing the data reader.
    /// </summary>
    /// <param name="stream">The stream</param>
    /// <param name="encoding">The encoding. Default is auto-detected.</param>
    /// <param name="quote">The quote character. Set to null to disable quoting. Default '"'</param>
    /// <param name="delimiter">Field delimiter. Default ','</param>
    /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
    /// <param name="comment">Comment marker. Set to null to disable comments. Default '#'</param>
    /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
    /// <param name="trimmingOptions">How should fields be trimmed?</param>
    /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
    /// <param name="emptyLineAction">What should happen when an empty line is found?</param>
    /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
    /// <param name="schema">The CSV schema (or schema's, if the file contains multiple result sets).</param>
    /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
    /// <param name="behaviour">Optional behaviour override. If provided, trimming/missing/empty/quotesInside parameters are ignored.</param>
    /// <returns>a DataReader instance to read the contents of the CSV file</returns>
    public static IDataReader FromStream(
            Stream stream,
            Encoding encoding = null,
            char? quote = '"',
            char delimiter = ',',
            char escape = '"',
            char? comment = '#',
            bool hasHeaders = false,
            ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.None,
            MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
            EmptyLineAction emptyLineAction = EmptyLineAction.Skip,
            QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
            OneOf<CsvSchema,CsvSchema[]> schema = null,
            CultureInfo cultureInfo = null,
            CsvBehaviour behaviour = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        var effectiveEncoding = encoding;
        var detectBom = false;
        
        if (effectiveEncoding == null)
        {
            effectiveEncoding = DetectEncoding(stream);
            detectBom = true;
        }
        else if (effectiveEncoding == Encoding.UTF8)
        {
            detectBom = true;
        }
        
        var reader = new StreamReader(stream, effectiveEncoding, detectBom, 1024, true);
        var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
        var effectiveBehaviour = behaviour ?? new CsvBehaviour(trimmingOptions, missingFieldAction, emptyLineAction, quotesInsideQuotedFieldAction);
        return FromReader(reader, layout, effectiveBehaviour, cultureInfo);
    }

    /// <summary>
    /// Read a string as CSV, using specific behaviour, layout and conversion options 
    /// </summary>
    /// <param name="input">The CSV input</param>
    /// <param name="quote">The quote character. Set to null to disable quoting. Default '"'</param>
    /// <param name="delimiter">Field delimiter. Default ','</param>
    /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
    /// <param name="comment">Comment marker. Set to null to disable comments. Default '#'</param>
    /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
    /// <param name="trimmingOptions">How should fields be trimmed?</param>
    /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
    /// <param name="emptyLineAction">What to do when an empty line is encountered?</param>
    /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
    /// <param name="schema">The CSV schema (or schema's, if the file contains multiple result sets).</param>
    /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
    /// <param name="behaviour">Optional behaviour override. If provided, trimming/missing/empty/quotesInside parameters are ignored.</param>
    /// <returns>a DataReader instance to read the contents of the CSV file</returns>
    public static IDataReader FromString(
        string input,
        char? quote = '"',
        char delimiter = ',',
        char escape = '"',
        char? comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.None,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        EmptyLineAction emptyLineAction = EmptyLineAction.Skip,
        QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        OneOf<CsvSchema,CsvSchema[]> schema = default,
        CultureInfo cultureInfo = null,
        CsvBehaviour behaviour = null)
    {
        var reader = new StringReader(input);
        var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
        var effectiveBehaviour = behaviour ?? new CsvBehaviour(trimmingOptions, missingFieldAction, emptyLineAction, quotesInsideQuotedFieldAction);
        return FromReader(reader, layout, effectiveBehaviour, cultureInfo);
    }


    /// <summary>
    /// Read a file as CSV, using specific behaviour, layout and conversion options. 
    /// </summary>
    /// <param name="path">The full or relative path name</param>
    /// <param name="encoding">The encoding of the file.</param>
    /// <param name="quote">The quote character. Set to null to disable quoting. Default '"'</param>
    /// <param name="delimiter">Field delimiter. Default ','</param>
    /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
    /// <param name="comment">Comment marker. Set to null to disable comments. Default '#'</param>
    /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
    /// <param name="trimmingOptions">How should fields be trimmed?</param>
    /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
    /// <param name="emptyLineAction">What should happen when an empty line is found?</param>
    /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
    /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
    /// <param name="behaviour">Optional behaviour override. If provided, trimming/missing/empty/quotesInside parameters are ignored.</param>
    /// <returns>a DataReader instance to read the contents of the CSV file</returns>
    public static IEnumerable<T> FromFile<T>(
        string path,
        Encoding encoding = null,
        char? quote = '"',
        char delimiter = ',',
        char escape = '"',
        char? comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        EmptyLineAction emptyLineAction = EmptyLineAction.Skip,
        QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        CultureInfo cultureInfo = null,
        CsvBehaviour behaviour = null)
    {
        var schema = new CsvSchemaBuilder(cultureInfo).From<T>().Schema;
        var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
        var effectiveBehaviour = behaviour ?? new CsvBehaviour(trimmingOptions, missingFieldAction, emptyLineAction, quotesInsideQuotedFieldAction);
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);

        var effectiveEncoding = encoding;
        var detectBom = false;

        if (effectiveEncoding == null)
        {
            effectiveEncoding = DetectEncoding(stream);
            detectBom = true;
        }
        else if (effectiveEncoding == Encoding.UTF8)
        {
            detectBom = true;
        }

        var reader = new StreamReader(stream, effectiveEncoding, detectBom, bufferSize: 32 * 1024);
        return FromReader<T>(reader, layout, effectiveBehaviour, schema, cultureInfo);
    }

    /// <summary>
    /// Read a file as CSV, using specific behaviour, layout and conversion options.
    /// Deserializes each record into an instance of <typeparamref name="T"/>
    /// </summary>
    /// <param name="stream">The stream to process</param>
    /// <param name="encoding">The encoding to use (default auto-detected)</param>
    /// <param name="quote">The quote character. Set to null to disable quoting. Default '"'</param>
    /// <param name="delimiter">Field delimiter. Default ','</param>
    /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
    /// <param name="comment">Comment marker. Set to null to disable comments. Default '#'</param>
    /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
    /// <param name="trimmingOptions">How should fields be trimmed?</param>
    /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
    /// <param name="emptyLineAction">What should happen when an empty line is found?</param>
    /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
    /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
    /// <param name="behaviour">Optional behaviour override. If provided, trimming/missing/empty/quotesInside parameters are ignored.</param>
    /// <returns>a DataReader instance to read the contents of the CSV file</returns>
    public static IEnumerable<T> FromStream<T>(
        Stream stream,
        Encoding encoding = null,
        char? quote = '"',
        char delimiter = ',',
        char escape = '"',
        char? comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        EmptyLineAction emptyLineAction = EmptyLineAction.Skip,
        QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        CultureInfo cultureInfo = null,
        CsvBehaviour behaviour = null)
    {
        var schema = new CsvSchemaBuilder(cultureInfo).From<T>().Schema;
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var effectiveEncoding = encoding;
        var detectBom = false;

        if (effectiveEncoding == null)
        {
            effectiveEncoding = DetectEncoding(stream);
            detectBom = true;
        }
        else if (effectiveEncoding == Encoding.UTF8)
        {
            detectBom = true;
        }

        var reader = new StreamReader(stream, effectiveEncoding, detectBom, 1024, true);
        var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
        var effectiveBehaviour = behaviour ?? new CsvBehaviour(trimmingOptions, missingFieldAction, emptyLineAction, quotesInsideQuotedFieldAction);
        return FromReader<T>(reader, layout, effectiveBehaviour, schema, cultureInfo);
    }

   
    /// <summary>
    /// Read a file as CSV, using specific behaviour, layout and conversion options. 
    /// </summary>
    /// <param name="path">The full or relative path name</param>
    /// <param name="encoding">The encoding of the file.</param>
    /// <param name="quote">The quote character. Set to null to disable quoting. Default '"'</param>
    /// <param name="delimiter">Field delimiter. Default ','</param>
    /// <param name="escape">Quote escape character (for quotes inside fields). Default '\'</param>
    /// <param name="comment">Comment marker. Set to null to disable comments. Default '#'</param>
    /// <param name="hasHeaders">Is the first line a header line (default false)?</param>
    /// <param name="trimmingOptions">How should fields be trimmed?</param>
    /// <param name="missingFieldAction">What should happen when a field is missing from a line?</param>
    /// <param name="emptyLineAction">What should happen when an empty line is found?</param>
    /// <param name="quotesInsideQuotedFieldAction">What should happen when a quote is found inside a quoted field?</param>
    /// <param name="cultureInfo">Culture info to be used for parsing culture-sensitive data (such as date/time and decimal numbers)</param>
    /// <param name="behaviour">Optional behaviour override. If provided, trimming/missing/empty/quotesInside parameters are ignored.</param>
    /// <returns>a DataReader instance to read the contents of the CSV file</returns>
    public static IEnumerable<T> FromString<T>(
        string input,
        char? quote = '"',
        char delimiter = ',',
        char escape = '"',
        char? comment = '#',
        bool hasHeaders = false,
        ValueTrimmingOptions trimmingOptions = ValueTrimmingOptions.UnquotedOnly,
        MissingFieldAction missingFieldAction = MissingFieldAction.ParseError,
        EmptyLineAction emptyLineAction = EmptyLineAction.Skip,
        QuotesInsideQuotedFieldAction quotesInsideQuotedFieldAction = QuotesInsideQuotedFieldAction.Ignore,
        CultureInfo cultureInfo = null,
        CsvBehaviour behaviour = null)
    {
        var schema = new CsvSchemaBuilder(cultureInfo).From<T>().Schema;
        var reader = new StringReader(input);
        var layout = new CsvLayout(quote, delimiter, escape, comment, hasHeaders, schema);
        var effectiveBehaviour = behaviour ?? new CsvBehaviour(trimmingOptions, missingFieldAction, emptyLineAction, quotesInsideQuotedFieldAction);
        return FromReader<T>(reader, layout, effectiveBehaviour, schema, cultureInfo);
    }

    internal static IEnumerable<T> FromReader<T>(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, CsvSchema schema, CultureInfo cultureInfo = null)
        => TypedRowEnumerable.FromReader<T>(reader, csvLayout, csvBehaviour, schema, cultureInfo);

    internal static IDataReader FromReader(TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, CultureInfo cultureInfo = null)
        => new CsvDataReader(reader, csvLayout, csvBehaviour, cultureInfo);
}

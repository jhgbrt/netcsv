using System.Data;
using System.IO;
using System.Text;
using System;

namespace Net.Code.Csv
{
    /// <summary>
    /// These extension methods were a bad idea and will be removed in the future.
    /// </summary>
    public static class CsvExtensions
    {
        [Obsolete("Use ReadCsv.FromStream method instead")]
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding)
        {
            return ReadCsv.FromFile(path, encoding);
        }

        [Obsolete("Use ReadCsv.FromFile method instead")]
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize = 4096)
        {
            return ReadCsv.FromFile(path, encoding, 
                csvLayout.Quote, csvLayout.Delimiter, csvLayout.Escape, csvLayout.Comment, csvLayout.HasHeaders, 
                csvBehaviour.TrimmingOptions, csvBehaviour.MissingFieldAction, csvBehaviour.SkipEmptyLines, csvBehaviour.QuotesInsideQuotedFieldAction, 
                converter, bufferSize);
        }

        [Obsolete("Use ReadCsv.FromStream method instead")]
        public static IDataReader ReadStreamAsCsv(this TextReader reader)
        {
            return ReadCsv.FromReader(reader, CsvLayout.Default, CsvBehaviour.Default, Converter.Default, 4096);
        }
        [Obsolete("Use ReadCsv.FromStream method instead")]
        public static IDataReader ReadStreamAsCsv(this TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize = 4096)
        {
            return ReadCsv.FromReader(reader, csvLayout, csvBehaviour, converter, bufferSize);
        }
        [Obsolete("Use ReadCsv.FromString method instead")]
        public static IDataReader ReadStringAsCsv(this string input)
        {
            return ReadCsv.FromString(input);
        }
        [Obsolete("Use ReadCsv.FromString method instead")]
        public static IDataReader ReadStringAsCsv(this string input, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize = 4096)
        {
            return ReadCsv.FromString(input,
                csvLayout.Quote, csvLayout.Delimiter, csvLayout.Escape, csvLayout.Comment, csvLayout.HasHeaders,
                csvBehaviour.TrimmingOptions, csvBehaviour.MissingFieldAction, csvBehaviour.SkipEmptyLines, csvBehaviour.QuotesInsideQuotedFieldAction,
                converter, bufferSize);
        }
    }
}
using System;
using System.Data;
using System.IO;
using System.Text;

namespace Net.Code.Csv
{
    /// <summary>
    /// These extension methods were a bad idea and will be removed in the future.
    /// </summary>
    public static class CsvExtensions
    {
        [Obsolete("Use ReadCsv.FromStream method instead", true)]
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding) => ReadCsv.FromFile(path, encoding);

        [Obsolete("Use ReadCsv.FromFile method instead", true)]
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize = 4096) 
            => ReadCsv.FromFile(path, encoding,
                csvLayout.Quote, csvLayout.Delimiter, csvLayout.Escape, csvLayout.Comment, csvLayout.HasHeaders,
                csvBehaviour.TrimmingOptions, csvBehaviour.MissingFieldAction, csvBehaviour.SkipEmptyLines, csvBehaviour.QuotesInsideQuotedFieldAction,
                converter, bufferSize);

        [Obsolete("Use ReadCsv.FromStream method instead", true)]
        public static IDataReader ReadStreamAsCsv(this TextReader reader) 
            => ReadCsv.FromReader(reader, CsvLayout.Default, CsvBehaviour.Default, Converter.Default, 4096);

        [Obsolete("Use ReadCsv.FromStream method instead", true)]
        public static IDataReader ReadStreamAsCsv(this TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize = 4096) 
            => ReadCsv.FromReader(reader, csvLayout, csvBehaviour, converter, bufferSize);

        [Obsolete("Use ReadCsv.FromString method instead", true)]
        public static IDataReader ReadStringAsCsv(this string input) 
            => ReadCsv.FromString(input);

        [Obsolete("Use ReadCsv.FromString method instead", true)]
        public static IDataReader ReadStringAsCsv(this string input, CsvLayout csvLayout, CsvBehaviour csvBehaviour, IConverter converter, int bufferSize = 4096) 
            => ReadCsv.FromString(input,
                csvLayout.Quote, csvLayout.Delimiter, csvLayout.Escape, csvLayout.Comment, csvLayout.HasHeaders,
                csvBehaviour.TrimmingOptions, csvBehaviour.MissingFieldAction, csvBehaviour.SkipEmptyLines, csvBehaviour.QuotesInsideQuotedFieldAction,
                converter, bufferSize);
    }
}
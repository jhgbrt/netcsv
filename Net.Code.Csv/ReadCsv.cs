using System.Data;
using System.IO;
using System.Text;
using Net.Code.Csv.Impl;

namespace Net.Code.Csv
{
    public static class ReadCsv
    {
        /// <summary>
        /// Read a file as CSV, using specific behaviour, layout and conversion options. Make sure to dispose the datareader.
        /// </summary>
        /// <param name="path">The full or relative path name</param>
        /// <param name="encoding">The encoding</param>
        /// <param name="csvLayout">Csv layout info (quote, delimiter, ...)</param>
        /// <param name="csvBehaviour">Csv behaviour info (e.g. what to do when fields are missing)</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader FromFile(
                string path,
                Encoding encoding,
                CsvLayout csvLayout = null,
                CsvBehaviour csvBehaviour = null,
                Converter converter = null,
                int bufferSize = 4096)
        {
            // caller should dispose IDataReader, which will indirectly also close the stream
            var stream = File.OpenRead(path);
            var reader = new StreamReader(stream, encoding);
            return FromReader(reader, csvLayout ?? CsvLayout.Default, csvBehaviour ?? CsvBehaviour.Default, converter ?? Converter.Default, bufferSize);
        }

        /// <summary>
        /// Read a stream as CSV, using specific behaviour, layout and conversion options.
        /// The stream will not be disposed by disposing the data reader.
        /// </summary>
        /// <param name="path">The full or relative path name</param>
        /// <param name="encoding">The encoding</param>
        /// <param name="csvLayout">Csv layout info (quote, delimiter, ...)</param>
        /// <param name="csvBehaviour">Csv behaviour info (e.g. what to do when fields are missing)</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader FromStream(
                Stream stream,
                Encoding encoding,
                CsvLayout csvLayout = null,
                CsvBehaviour csvBehaviour = null,
                Converter converter = null,
                int bufferSize = 4096)
        {
            var reader = new StreamReader(stream, encoding, false, 1024, true);
            return FromReader(reader, csvLayout ?? CsvLayout.Default, csvBehaviour ?? CsvBehaviour.Default, converter ?? Converter.Default, bufferSize);
        }

        /// <summary>
        /// Read a string as CSV, using specific behaviour, layout and conversion options 
        /// </summary>
        /// <param name="input">The CSV input</param>
        /// <param name="csvLayout">Csv layout info (quote, delimiter, ...)</param>
        /// <param name="csvBehaviour">Csv behaviour info (e.g. what to do when fields are missing)</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader FromString(string input, CsvLayout csvLayout = null, CsvBehaviour csvBehaviour = null, Converter converter = null, int bufferSize = 4096)
        {
            var reader = new StringReader(input);
            return FromReader(reader, csvLayout ?? CsvLayout.Default, csvBehaviour ?? CsvBehaviour.Default, converter ?? Converter.Default, bufferSize);
        }


        internal static IDataReader FromReader(TextReader reader, CsvLayout csvLayout = null, CsvBehaviour csvBehaviour = null, Converter converter = null, int bufferSize = 4096)
        {
            var parser = new CsvParser(reader, csvLayout, csvBehaviour);
            return new CsvDataReader(parser, converter);
        }
    }
}
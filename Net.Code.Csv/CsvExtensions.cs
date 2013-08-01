using System.Data;
using System.IO;
using System.Text;
using Net.Code.Csv.Impl;

namespace Net.Code.Csv
{
    /// <summary>
    /// Contains entry points (extension methods) for reading a string, file or stream as CSV
    /// </summary>
    public static class CsvExtensions
    {
        /// <summary>
        /// Read a file as CSV, using default behaviour, layout and conversion options 
        /// (<see cref="CsvLayout.Default"/>, <see cref="CsvBehaviour.Default"/> and 
        /// <see cref="Converter.Default"/>)
        /// </summary>
        /// <param name="path">The full or relative path name</param>
        /// <param name="encoding">The encoding</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding)
        {
            StreamReader reader = null;
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(path);
                reader = new StreamReader(stream, encoding);
                return ReadStreamAsCsv(reader, CsvLayout.Default, CsvBehaviour.Default, Converter.Default);
            }
            catch
            {
                if (reader != null) reader.Dispose();
                else if (stream != null) stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Read a file as CSV, using specific behaviour, layout and conversion options 
        /// </summary>
        /// <param name="path">The full or relative path name</param>
        /// <param name="encoding">The encoding</param>
        /// <param name="csvLayout">Csv layout info (quote, delimiter, ...)</param>
        /// <param name="csvBehaviour">Csv behaviour info (e.g. what to do when fields are missing)</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            StreamReader reader = null;
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(path);
                reader = new StreamReader(stream, encoding);
                return ReadStreamAsCsv(reader, csvLayout, csvBehaviour, converter, bufferSize);
            }
            catch
            {
                if (reader != null) reader.Dispose();
                else if (stream != null) stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Read a string as CSV, using default behaviour, layout and conversion options 
        /// (<see cref="CsvLayout.Default"/>, <see cref="CsvBehaviour.Default"/> and 
        /// <see cref="Converter.Default"/>)
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader ReadStringAsCsv(this string input)
        {
            return ReadStringAsCsv(input, CsvLayout.Default, CsvBehaviour.Default, Converter.Default);
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
        public static IDataReader ReadStringAsCsv(this string input, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            StringReader reader = null;
            try
            {
                reader = new StringReader(input);
                return ReadStreamAsCsv(reader, csvLayout, csvBehaviour, converter, bufferSize);
            }
            catch
            {
                if (reader != null) reader.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Read a stream as CSV, using specific behaviour, layout and conversion options 
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> instance</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader ReadStreamAsCsv(this TextReader reader)
        {
            return ReadStreamAsCsv(reader, CsvLayout.Default, CsvBehaviour.Default, Converter.Default);
        }

        /// <summary>
        /// Read a stream as CSV, using specific behaviour, layout and conversion options 
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> instance</param>
        /// <param name="csvLayout">Csv layout info (quote, delimiter, ...)</param>
        /// <param name="csvBehaviour">Csv behaviour info (e.g. what to do when fields are missing)</param>
        /// <param name="converter">Converter class for converting strings to primitive types (used by the data reader</param>
        /// <param name="bufferSize">The number of characters to buffer while parsing the CSV.</param>
        /// <returns>a datareader instance to read the contents of the CSV file</returns>
        public static IDataReader ReadStreamAsCsv(this TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            var parser = new CsvParser(reader, bufferSize, csvLayout, csvBehaviour);
            return new CsvDataReader(parser, converter);
        }
    }
}
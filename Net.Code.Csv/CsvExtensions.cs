using System.Data;
using System.IO;
using System.Text;
using System;

namespace Net.Code.Csv
{
    /// <summary>
    /// Contains entry points (extension methods) for reading a string, file or stream as CSV
    /// </summary>
    public static class CsvExtensions
    {
        [Obsolete("Use ReadCsv.FromStream method instead")]
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding)
        {
            return ReadCsv.FromFile(path, encoding);
        }

        [Obsolete("Use ReadCsv.FromFile method instead")]
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            return ReadCsv.FromFile(path, encoding, csvLayout, csvBehaviour, converter, bufferSize);
        }

        [Obsolete("Use ReadCsv.FromStream method instead")]
        public static IDataReader ReadStreamAsCsv(this TextReader reader)
        {
            return ReadCsv.FromReader(reader);
        }
        [Obsolete("Use ReadCsv.FromStream method instead")]
        public static IDataReader ReadStreamAsCsv(this TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            return ReadCsv.FromReader(reader, csvLayout, csvBehaviour, converter, bufferSize);
        }
        [Obsolete("Use ReadCsv.FromString method instead")]
        public static IDataReader ReadStringAsCsv(this string input)
        {
            return ReadCsv.FromString(input);
        }
        [Obsolete("Use ReadCsv.FromString method instead")]
        public static IDataReader ReadStringAsCsv(this string input, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            return ReadCsv.FromString(input, csvLayout, csvBehaviour, converter, bufferSize);
        }
    }
}
using System.Data;
using System.IO;
using System.Text;

namespace Net.Code.Csv
{
    public static class CsvExtensions
    {
        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding)
        {
            var reader = new StreamReader(File.OpenRead(path), encoding);
            return ReadStreamAsCsv(reader, CsvLayout.Default, CsvBehaviour.Default, Converter.Default);
        }

        public static IDataReader ReadFileAsCsv(this string path, Encoding encoding, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            var reader = new StreamReader(File.OpenRead(path), encoding);
            return ReadStreamAsCsv(reader, csvLayout, csvBehaviour, converter, bufferSize);
        }

        public static IDataReader ReadStringAsCsv(this string input)
        {
            return ReadStringAsCsv(input, CsvLayout.Default, CsvBehaviour.Default, Converter.Default);
        }

        public static IDataReader ReadStringAsCsv(this string input, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            var reader = new StringReader(input);
            return ReadStreamAsCsv(reader, csvLayout, csvBehaviour, converter, bufferSize);
        }

        public static IDataReader ReadStreamAsCsv(this TextReader reader)
        {
            return ReadStreamAsCsv(reader, CsvLayout.Default, CsvBehaviour.Default, Converter.Default);
        }
        
        public static IDataReader ReadStreamAsCsv(this TextReader reader, CsvLayout csvLayout, CsvBehaviour csvBehaviour, Converter converter, int bufferSize = 4096)
        {
            var parser = new CsvParser(reader, bufferSize, csvLayout, csvBehaviour);
            return new CsvDataReader(parser, converter);
        }
    }
}
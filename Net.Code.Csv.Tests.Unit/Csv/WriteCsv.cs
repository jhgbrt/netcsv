
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Net.Code.Csv.Tests.Unit.Csv
{
    class WriteCsv
    {
        public static void ToFile<T>(
            IEnumerable<T> items, 
            string path,
            Encoding encoding = null,
            char delimiter = ',', 
            char quote= '"',
            char escape = '\\', 
            bool hasHeaders = false,
            bool append = false
            )
        {
            using var stream = File.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write);
            ToStream(items, stream, encoding, delimiter, quote, escape, !append && hasHeaders);
        }
        public static void ToStream<T>(
            IEnumerable<T> items,
            Stream stream,
            Encoding encoding = null,
            char delimiter = ',',
            char quote = '"',
            char escape = '\\',
            bool hasHeaders = false
            )
        {
            using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8);
            ToWriter(items, writer, delimiter, quote, escape, hasHeaders);
        }

        public static string ToString<T>(
            IEnumerable<T> items,
            char delimiter = ',',
            char quote = '"',
            char escape = '\\',
            bool hasHeaders = false
            )
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);
            ToWriter(items, writer, delimiter, quote, escape, hasHeaders);
            return sb.ToString();
        }

        static void ToWriter<T>(
            IEnumerable<T> items,
            TextWriter writer,
            char delimiter = ',',
            char quote = '"',
            char escape = '\\',
            bool hasHeaders = false
            )
        {
            var properties = typeof(T).GetProperties();
            if (hasHeaders)
            {
                writer.WriteLine(string.Join(delimiter, properties.Select(p => p.Name)));
            }

            foreach (var item in items)
            {
                writer.WriteLine(string.Join(delimiter, properties.Select(
                    p => GetString(p, item, quote, escape)))
                    );
            }
        }

        private static string GetString<T>(PropertyInfo p, T item, char quote, char escape)
        {
            var value = p.GetValue(item);
            var s = CsvConvert.ToString(value, p).Trim().Replace(quote.ToString(), $"{escape}{quote}");
            if (p.PropertyType.IsValueType) return s;
            return $"{quote}{s}{quote}";
        }

        static class CsvConvert
        {
            public static string ToString(object input, PropertyInfo p)
            {

                return input switch
                {
                    DateTime d => d.ToString((p.GetCustomAttribute<CsvFormatAttribute>()?.Format) ?? "zzz"),
                    object o => Convert.ToString(o, CultureInfo.InvariantCulture)
                };
            }
        }
    }

}

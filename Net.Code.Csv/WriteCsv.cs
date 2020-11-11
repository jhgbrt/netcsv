
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Code.Csv.Tests.Unit.Csv
{
    public class WriteCsv
    {
        /// <summary>
        /// Write a list of items to a file in CSV format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items to serialize</param>
        /// <param name="path">Path of the target file</param>
        /// <param name="encoding">Encoding to use. Default UTF-8</param>
        /// <param name="delimiter">The delimiter (default ',')</param>
        /// <param name="quote">The quote character to use. Fields are always quoted.</param>
        /// <param name="escape">The escape character to use. Required to escape quotes in string fields.</param>
        /// <param name="hasHeaders">Should a header be written? Ignored when append is true and file is not empty.</param>
        /// <param name="append">Append to the file. If true, no headers are written if the file is not empty (regardless of hasHeaders).</param>
        /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
        public static void ToFile<T>(
            IEnumerable<T> items, 
            string path,
            Encoding encoding = null,
            char delimiter = ',', 
            char quote= '"',
            char escape = '\\', 
            bool hasHeaders = false,
            bool append = false,
            CultureInfo cultureInfo = null
            )
        {
            using var stream = File.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write);
            var writeHeader = hasHeaders && (!append || stream.Position == 0);
            ToStream(items, stream, encoding, delimiter, quote, escape, writeHeader, cultureInfo);
        }

        /// <summary>
        /// Write a list of items to a stream in CSV format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items to serialize</param>
        /// <param name="stream">Stream to write to</param>
        /// <param name="encoding">Encoding to use. Default UTF-8</param>
        /// <param name="delimiter">The delimiter (default ',')</param>
        /// <param name="quote">The quote character to use. Fields are always quoted.</param>
        /// <param name="escape">The escape character to use. Required to escape quotes in string fields.</param>
        /// <param name="hasHeaders">Should a header be written?</param>
        /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
        public static void ToStream<T>(
            IEnumerable<T> items,
            Stream stream,
            Encoding encoding = null,
            char delimiter = ',',
            char quote = '"',
            char escape = '\\',
            bool hasHeaders = false,
            CultureInfo cultureInfo = null
            )
        {
            using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8);
            ToWriter(items, writer, delimiter, quote, escape, hasHeaders, cultureInfo);
        }

        /// <summary>
        /// Serialize a list of items in CSV format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items to serialize</param>
        /// <param name="delimiter">The delimiter (default ',')</param>
        /// <param name="quote">The quote character to use. Fields are always quoted.</param>
        /// <param name="escape">The escape character to use. Required to escape quotes in string fields.</param>
        /// <param name="hasHeaders">Should a header be written?</param>
        /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
        public static string ToString<T>(
            IEnumerable<T> items,
            char delimiter = ',',
            char quote = '"',
            char escape = '\\',
            bool hasHeaders = false,
            CultureInfo cultureInfo = null
            )
        {
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            ToWriter(items, writer, delimiter, quote, escape, hasHeaders, cultureInfo);
            return sb.ToString();
        }

        static void ToWriter<T>(
            IEnumerable<T> items,
            TextWriter writer,
            char delimiter = ',',
            char quote = '"',
            char escape = '\\',
            bool hasHeaders = false,
            CultureInfo cultureInfo = null
            )
        {
            cultureInfo ??= CultureInfo.InvariantCulture;
            var escapedQuote = $"{escape}{quote}";
            var unescapedQuote = $"{quote}";
            var properties = typeof(T).GetPropertiesWithCsvFormat();
            var sb = new StringBuilder();
            if (hasHeaders)
            {
                writer.WriteLine(string.Join(delimiter.ToString(), properties.Select(p => p.property.Name)));
            }

            foreach (var item in items)
            {
                var values = from tuple in properties
                             let prop = tuple.property
                             let format = tuple.format
                             let value = prop.GetValue(item)
                             let s = value switch
                             {
                                 DateTime d => d.ToString(format ?? "O", cultureInfo),
                                 DateTimeOffset d => d.ToString(format ?? "O", cultureInfo),
                                 object o => Convert.ToString(o, cultureInfo),
                                 null => string.Empty
                             }
                             select prop.PropertyType.IsValueType
                                ? sb.Clear().Append(s).Replace(unescapedQuote, escapedQuote)
                                : sb.Clear().Append(s).Replace(unescapedQuote, escapedQuote).Insert(0, quote).Append(quote);

                bool writeDelimiter = false;
                foreach (var v in values)
                {
                    if (writeDelimiter) writer.Write(delimiter);
                    else writeDelimiter = true;
                    writer.Write(sb.ToString());
                }

                writer.WriteLine();
            }
        }
    }

}

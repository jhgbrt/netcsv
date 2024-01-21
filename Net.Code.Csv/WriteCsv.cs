using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Net.Code.Csv;

public static class WriteCsv
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
    /// <param name="hasHeaders">Should a header be written? If true, the property names of the [T] are written to the file as headers. Ignored when append is true and file is not empty.</param>
    /// <param name="append">Append to the file. If true, no headers are written if the file is not empty (regardless of hasHeaders).</param>
    /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
    public static void ToFile<T>(
        IEnumerable<T> items,
        string path,
        Encoding encoding = null,
        char delimiter = ',',
        char quote = '"',
        char escape = '"',
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
    /// <param name="stream">Stream to write to. The stream is left open.</param>
    /// <param name="encoding">Encoding to use. Default UTF-8</param>
    /// <param name="delimiter">The delimiter (default ',')</param>
    /// <param name="quote">The quote character to use. Fields are always quoted.</param>
    /// <param name="escape">The escape character to use. Required to escape quotes in string fields.</param>
    /// <param name="hasHeaders">Should a header be written? If true, the property names of the [T] are written to the file as headers.</param>
    /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
    public static void ToStream<T>(
        IEnumerable<T> items,
        Stream stream,
        Encoding encoding = null,
        char delimiter = ',',
        char quote = '"',
        char escape = '"',
        bool hasHeaders = false,
        CultureInfo cultureInfo = null
        )
    {
        using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, 4096, true);
        ToWriter(items, writer, delimiter, quote, escape, hasHeaders, cultureInfo);
    }

    /// <summary>
    /// Write a list of items to a stream in CSV format
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items to serialize</param>
    /// <param name="stream">Stream to write to. The stream is left open.</param>
    /// <param name="encoding">Encoding to use. Default UTF-8</param>
    /// <param name="delimiter">The delimiter (default ',')</param>
    /// <param name="quote">The quote character to use. Fields are always quoted.</param>
    /// <param name="escape">The escape character to use. Required to escape quotes in string fields.</param>
    /// <param name="hasHeaders">Should a header be written? If true, the property names of the [T] are written to the file as headers.</param>
    /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
    public static async Task ToStream<T>(
        IAsyncEnumerable<T> items,
        Stream stream,
        Encoding encoding = null,
        char delimiter = ',',
        char quote = '"',
        char escape = '"',
        bool hasHeaders = false,
        CultureInfo cultureInfo = null
        )
    {
        using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, 4096, true);
        await ToWriter(items, writer, delimiter, quote, escape, hasHeaders, cultureInfo);
    }
    /// <summary>
    /// Serialize a list of items in CSV format
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items to serialize</param>
    /// <param name="delimiter">The delimiter (default ',')</param>
    /// <param name="quote">The quote character to use. Fields are always quoted.</param>
    /// <param name="escape">The escape character to use. Required to escape quotes in string fields.</param>
    /// <param name="hasHeaders">Should a header be written? If true, the property names of the [T] are written to the file as headers.</param>
    /// <param name="cultureInfo">Culture info to be used when serializing values.</param>
    public static string ToString<T>(
        IEnumerable<T> items,
        char delimiter = ',',
        char quote = '"',
        char escape = '"',
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
        char escape = '"',
        bool hasHeaders = false,
        CultureInfo cultureInfo = null
        )
    {
        var properties = typeof(T).GetPropertiesWithCsvFormat();
        if (hasHeaders)
        {
            writer.WriteLine(string.Join(delimiter.ToString(), properties.Select(p => p.property.Name)));
        }

        cultureInfo ??= CultureInfo.InvariantCulture;
        var converter = new Converter(cultureInfo);
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            var values = GetPropertyValuesAsString(item, properties, delimiter, quote, escape, converter, sb);

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

    static async Task ToWriter<T>(
        IAsyncEnumerable<T> items,
        TextWriter writer,
        char delimiter = ',',
        char quote = '"',
        char escape = '"',
        bool hasHeaders = false,
        CultureInfo cultureInfo = null
        )
    {
        var properties = typeof(T).GetPropertiesWithCsvFormat();
        if (hasHeaders)
        {
            writer.WriteLine(string.Join(delimiter.ToString(), properties.Select(p => p.property.Name)));
        }

        cultureInfo ??= CultureInfo.InvariantCulture;
        var converter = new Converter(cultureInfo);
        var sb = new StringBuilder();
        await foreach (var item in items)
        {
            var values = GetPropertyValuesAsString(item, properties, delimiter, quote, escape, converter, sb);

            bool writeDelimiter = false;
            foreach (var v in values)
            {
                if (writeDelimiter) await writer.WriteAsync(delimiter);
                else writeDelimiter = true;
                await writer.WriteAsync(v);
            }

            await writer.WriteLineAsync();
        }
    }

    /// <summary>
    /// This method returns an unmaterialized LINQ query for all properties of an item of type T
    /// the StringBuilder parameter should be allocated once and is passed in to avoid the 
    /// allocation of a new StringBuilder for each item in the list.
    /// In the query, the StringBuilder is cleared for each resulting value, in order to avoid unnecessary allocations.
    /// </summary>
    private static IEnumerable<string> GetPropertyValuesAsString(object item, IEnumerable<(PropertyInfo property, string format)> properties, char delimiter, char quote, char escape, Converter converter, StringBuilder sb)
    => from pf in properties
       let value = pf.property.GetValue(item)
       let s = converter.ToString(value, pf.format)
       select sb.Clear().Append(s).QuoteIfNecessary(quote, delimiter, escape).ToString();
}

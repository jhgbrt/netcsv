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
        var schema = Schema.From<T>();
        var properties = GetPropertiesForSchema<T>(schema);
        if (hasHeaders)
        {
            writer.WriteLine(string.Join(delimiter.ToString(), schema.Columns.Select(c => c.Name)));
        }

        cultureInfo ??= CultureInfo.InvariantCulture;
        var converter = new Converter(cultureInfo);
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            WriteValues(item, properties, writer, delimiter, quote, escape, converter, sb);
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
        var properties = typeof(T).GetPropertiesWithCsvFormat().ToArray();
        if (hasHeaders)
        {
            writer.WriteLine(string.Join(delimiter.ToString(), properties.Select(p => p.property.Name)));
        }

        cultureInfo ??= CultureInfo.InvariantCulture;
        var converter = new Converter(cultureInfo);
        var sb = new StringBuilder();
        await foreach (var item in items)
        {
            await WriteValuesAsync(item, properties, writer, delimiter, quote, escape, converter, sb);
            await writer.WriteLineAsync();
        }
    }

    private static (PropertyInfo property, string format)[] GetPropertiesForSchema<T>(CsvSchema schema)
    {
        var properties = typeof(T).GetPropertiesWithCsvFormat().ToArray();
        var lookup = new Dictionary<string, (PropertyInfo property, string format)>(properties.Length, StringComparer.Ordinal);
        foreach (var property in properties)
        {
            lookup[property.property.Name] = property;
        }

        var result = new (PropertyInfo property, string format)[schema.Columns.Count];
        var count = 0;
        foreach (var column in schema.Columns)
        {
            if (lookup.TryGetValue(column.PropertyName, out var property))
            {
                result[count++] = property;
            }
        }

        if (count == result.Length)
        {
            return result;
        }

        Array.Resize(ref result, count);
        return result;
    }

    private static void WriteValues(
        object item,
        (PropertyInfo property, string format)[] properties,
        TextWriter writer,
        char delimiter,
        char quote,
        char escape,
        Converter converter,
        StringBuilder sb)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(delimiter);
            }

            var (property, format) = properties[i];
            var value = property.GetValue(item);
            var s = converter.ToString(value, format);
            sb.Clear().Append(s).QuoteIfNecessary(quote, delimiter, escape);
            writer.Write(sb.ToString());
        }
    }

    private static async Task WriteValuesAsync(
        object item,
        (PropertyInfo property, string format)[] properties,
        TextWriter writer,
        char delimiter,
        char quote,
        char escape,
        Converter converter,
        StringBuilder sb)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0)
            {
                await writer.WriteAsync(delimiter);
            }

            var (property, format) = properties[i];
            var value = property.GetValue(item);
            var s = converter.ToString(value, format);
            sb.Clear().Append(s).QuoteIfNecessary(quote, delimiter, escape);
            await writer.WriteAsync(sb.ToString());
        }
    }

}

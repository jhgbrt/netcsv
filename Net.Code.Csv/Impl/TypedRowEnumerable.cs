using Net.Code.Csv.Impl.V2;

namespace Net.Code.Csv.Impl;

internal static class TypedRowEnumerable
{
    internal static IEnumerable<T> FromReader<T>(TextReader reader, CsvLayout layout, CsvBehaviour behaviour, CsvSchema schema, CultureInfo cultureInfo)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        schema ??= Schema.From<T>();
        var converter = new Converter(cultureInfo ?? CultureInfo.InvariantCulture);
        return EnumerateParser<T>(reader, layout, behaviour, schema, converter);
    }

    internal static IEnumerable<T> FromDataReader<T>(CsvDataReader reader)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        var schema = reader.Schema ?? Schema.From<T>();
        var useSchemaConverters = reader.Schema is not null;
        var converter = useSchemaConverters ? null : new Converter(CultureInfo.InvariantCulture);
        var activator = TypedLineActivator<T>.Get(schema, reader.Header, converter, useSchemaConverters);

        while (reader.Read())
        {
            yield return activator(reader.CurrentLine);
        }
    }

    private static IEnumerable<T> EnumerateParser<T>(
        TextReader reader,
        CsvLayout layout,
        CsvBehaviour behaviour,
        CsvSchema schema,
        Converter converter)
    {
        using var parser = new CsvParserV2(reader, new BufferedCharReader(reader), layout, behaviour);
        var activator = TypedLineActivator<T>.Get(schema, parser.Header, converter, useSchemaConverters: false);

        foreach (var line in parser)
        {
            if (behaviour.EmptyLineAction == EmptyLineAction.NextResult && line.IsEmpty)
            {
                line.ReturnToPool();
                yield break;
            }

            yield return activator(line);
        }
    }
}

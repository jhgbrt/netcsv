using System.Reflection;

namespace Net.Code.Csv.Impl;

internal readonly record struct ActivatorPropertyMapping(
    PropertyInfo Property,
    string ColumnName,
    int SchemaOrdinal,
    string Format,
    CsvColumn Column);

internal static class ActivatorMapping
{
    internal static ActivatorPropertyMapping[] GetMappings(Type type, CsvSchema schema)
    {
        var columns = schema.Columns;
        var byPropertyName = new Dictionary<string, (CsvColumn column, int ordinal)>(columns.Count, StringComparer.Ordinal);
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            byPropertyName[column.PropertyName] = (column, i);
        }

        var formatByPropertyName = type.GetPropertiesWithCsvFormat()
            .ToDictionary(x => x.property.Name, x => x.format, StringComparer.Ordinal);

        var properties = type.GetProperties();
        var mappings = new List<ActivatorPropertyMapping>(properties.Length);
        foreach (var property in properties)
        {
            if (!byPropertyName.TryGetValue(property.Name, out var match))
            {
                continue;
            }

            formatByPropertyName.TryGetValue(property.Name, out var format);
            var resolvedFormat = match.column.Format ?? format;
            mappings.Add(new ActivatorPropertyMapping(property, match.column.Name, match.ordinal, resolvedFormat, match.column));
        }

        return [.. mappings];
    }

    internal static string BuildMappingSignature(ActivatorPropertyMapping[] mappings)
    {
        if (mappings.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(mappings.Length * 16);
        for (var i = 0; i < mappings.Length; i++)
        {
            builder.Append(mappings[i].Property.Name)
                .Append('|')
                .Append(mappings[i].Format)
                .Append(';');
        }

        return builder.ToString();
    }

    internal static string BuildSignature(CsvSchema schema, ActivatorPropertyMapping[] mappings)
    {
        var schemaSignature = schema.Signature;
        var mappingSignature = BuildMappingSignature(mappings);
        return string.Concat(schemaSignature, "#", mappingSignature);
    }
}

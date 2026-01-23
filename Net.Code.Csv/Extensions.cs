using System.Collections.Generic;
using System.Reflection;
namespace Net.Code.Csv;

public static class Extensions
{
    private readonly record struct PropertyMapping(PropertyInfo Property, string ColumnName, int SchemaOrdinal);

    static Func<IDataRecord, T> GetActivator<T>(CsvSchema schema)
    {
        var type = typeof(T);
        var activator = GetActivator(type, schema);
        return record => (T)activator(record);
    }

    private static Func<IDataRecord, object> GetActivator(Type type, CsvSchema schema)
    {
        schema ??= Schema.From(type);
        var mappings = GetPropertyMappings(type, schema);
        var properties = mappings.Select(m => m.Property).ToArray();

        int[] ordinals = null;
        int[] EnsureOrdinals(IDataRecord record)
        {
            if (ordinals != null)
            {
                return ordinals;
            }

            ordinals = new int[mappings.Length];
            if (record is CsvDataReader)
            {
                for (var i = 0; i < mappings.Length; i++)
                {
                    ordinals[i] = mappings[i].SchemaOrdinal;
                }
                return ordinals;
            }

            for (var i = 0; i < mappings.Length; i++)
            {
                ordinals[i] = record.GetOrdinal(mappings[i].ColumnName);
            }

            return ordinals;
        }

        // if we find a record constructor with parameters matching the properties of the type, use that
        var constructor = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType))));

        if (constructor is null)
        {
            // no such constructor; use the default constructor and set all properties with setter
            constructor = type.GetConstructor([]);
            return record =>
            {
                var item = constructor.Invoke([]);
                var resolvedOrdinals = EnsureOrdinals(record);
                for (var i = 0; i < mappings.Length; i++)
                {
                    mappings[i].Property.SetValue(item, record.GetValue(resolvedOrdinals[i]));
                }
                return item;
            };
        }
        else
        {
            var values = new object[mappings.Length];
            return record =>
            {
                var resolvedOrdinals = EnsureOrdinals(record);
                for (var i = 0; i < mappings.Length; i++)
                {
                    values[i] = record.GetValue(resolvedOrdinals[i]);
                }
                return constructor.Invoke(values);
            };
        }
    }

    private static PropertyMapping[] GetPropertyMappings(Type type, CsvSchema schema)
    {
        var columns = schema.Columns;
        var byPropertyName = new Dictionary<string, (string columnName, int ordinal)>(columns.Count, StringComparer.Ordinal);
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            byPropertyName[column.PropertyName] = (column.Name, i);
        }

        var properties = type.GetProperties();
        var mappings = new List<PropertyMapping>(properties.Length);
        foreach (var property in properties)
        {
            if (byPropertyName.TryGetValue(property.Name, out var match))
            {
                mappings.Add(new PropertyMapping(property, match.columnName, match.ordinal));
            }
        }

        return [.. mappings];
    }

    public static IEnumerable<T> AsEnumerable<T>(this IDataReader reader)
    {

        CsvSchema schema = reader is CsvDataReader r ? r.Schema : Schema.From<T>();
        var activator = GetActivator<T>(schema);
        while (reader.Read())
        {
            T item = activator(reader);
            yield return item;
        }
    }

    public static IEnumerable<dynamic> AsEnumerable(this IDataReader reader)
    {
        while (reader.Read())
        {
            yield return reader.ToExpando();
        }
    }
    public static IEnumerable<dynamic> AsEnumerable(this IDataReader reader, Type type)
    {
        CsvSchema schema = reader is CsvDataReader r ? r.Schema : Schema.From(type);
        var activator = GetActivator(type, schema);
        while (reader.Read())
        {
            var item = activator(reader);
            yield return item;
        }
    }

    internal static dynamic ToExpando(this IDataRecord rdr) => Dynamic.From(rdr.NameValues().ToDictionary(p => p.name, p => p.value));
    internal static IEnumerable<(string name, object value)> NameValues(this IDataRecord record)
    {
        for (var i = 0; i < record.FieldCount; i++) yield return (record.GetName(i), record[i]);
    }
    public static string[] GetFieldHeaders(this IDataReader reader)
        => reader.GetSchemaTable().Rows.OfType<DataRow>().Select(r => r["ColumnName"]).OfType<string>().ToArray();
}
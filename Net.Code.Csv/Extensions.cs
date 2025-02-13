using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
namespace Net.Code.Csv;

public static class Extensions
{
    static Func<IDataRecord, T> GetActivator<T>(CsvSchema schema)
    {
        var type = typeof(T);
        var activator = GetActivator(type, schema);
        return record => (T)activator(record);
    }

    private static Func<IDataRecord, object> GetActivator(Type type, CsvSchema schema)
    {
        schema ??= Schema.From(type);
        var properties =
             from p in type.GetProperties()
             join c in schema.Columns on p.Name equals c.PropertyName
             select p;

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
                foreach (var p in properties)
                {
                    p.SetValue(item, record.GetValue(record.GetOrdinal(GetName(p))));
                }
                return item;
            };
        }
        else
        {
            return record =>
            {
                var values = properties.Select(p => record.GetValue(record.GetOrdinal(p.Name)));
                return constructor.Invoke(values.ToArray());
            };
        }
    }

    private static string GetName(PropertyInfo property)
    {
        return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;
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

namespace Net.Code.Csv;

public static class Extensions
{
    static Func<IDataRecord, T> GetActivator<T>()
    {
        var type = typeof(T);
        var activator = GetActivator(type);
        return record => (T)activator(record);
    }

    private static Func<IDataRecord, object> GetActivator(Type type)
    {
        var properties = type.GetProperties();

        // if we find a record constructor with parameters matching the properties of the type, use that
        var constructor = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType))));

        if (constructor is null)
        {
            // no such constructor; use the default constructor and set all properties with setter
            constructor = type.GetConstructor(Array.Empty<Type>());
            return record =>
            {
                var values = properties.Select(p => record.GetValue(record.GetOrdinal(p.Name)));
                var item = constructor.Invoke(Array.Empty<Type>());
                foreach (var p in properties)
                {
                    p.SetValue(item, record.GetValue(record.GetOrdinal(p.Name)));
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

    public static IEnumerable<T> AsEnumerable<T>(this IDataReader reader)
    {
        var activator = GetActivator<T>();
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
        var activator = GetActivator(type);
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

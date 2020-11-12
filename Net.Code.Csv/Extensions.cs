
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;

namespace Net.Code.Csv
{
    public static class Extensions
    {
        static Func<IDataRecord, T> GetActivator<T>()
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            var constructor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                    .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType))));

            if (constructor is null) 
            {
                constructor = type.GetConstructor(Array.Empty<Type>());
                return record =>
                {
                    var values = properties.Select(p => record.GetValue(record.GetOrdinal(p.Name)));
                    var item = (T)constructor.Invoke(Array.Empty<Type>());
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
                    return (T)constructor.Invoke(values.ToArray());
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
        public static string[] GetFieldHeaders(this IDataReader reader)
            => reader.GetSchemaTable().Rows.OfType<DataRow>().Select(r => r["ColumnName"]).OfType<string>().ToArray();
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

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
    }

    internal static class TypeExtensions
    {
        public static Type GetUnderlyingType(this Type type) => type.IsNullableType() ? Nullable.GetUnderlyingType(type) : type;
        public static bool IsNullableType(this Type type)
            => type.IsGenericType && !type.IsGenericTypeDefinition && typeof(Nullable<>) == type.GetGenericTypeDefinition();

        /// <summary>
        /// Finds a constructor with a signature that accepts all properties in default order.
        /// Strict positional records are guaranteed have such a constructor; regular classes may also have it.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ConstructorInfo GetRecordConstructor(this Type type) 
            => type.GetConstructor(type.GetProperties().Select(p => p.PropertyType).ToArray());

        public static IEnumerable<(PropertyInfo property, string format)> GetPropertiesWithCsvFormat(this Type type)
        {
            var properties = type.GetProperties();
            var parameters = type.GetRecordConstructor()?.GetParameters() ?? Enumerable.Repeat(default(ParameterInfo), properties.Length);
            return properties.Zip(parameters, (property, parameter) => (property, parameter?.GetCsvFormat() ?? property.GetCsvFormat())).ToArray();
        }

        static string GetCsvFormat(this ICustomAttributeProvider attributeProvider)
            => attributeProvider?.GetCustomAttributes(false).OfType<CsvFormatAttribute>().FirstOrDefault()?.Format;
    }
}
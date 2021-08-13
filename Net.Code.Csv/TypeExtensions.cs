using System.Reflection;

namespace Net.Code.Csv;

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

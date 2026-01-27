namespace Net.Code.Csv.Impl;

internal static class CsvParserSelector
{
    private const string ParserEnvVar = "NETCSV_PARSER";

    internal static CsvParserKind GetKind(CsvBehaviour behaviour)
    {
        var kind = behaviour?.Parser ?? CsvParserKind.Default;
        if (kind != CsvParserKind.Default)
        {
            return kind;
        }

        var env = Environment.GetEnvironmentVariable(ParserEnvVar);
        if (string.IsNullOrWhiteSpace(env))
        {
            return CsvParserKind.V1;
        }

        return ParseKind(env);
    }

    private static CsvParserKind ParseKind(string value)
    {
        var normalized = value.Trim();
        if (normalized.Equals("V1", StringComparison.OrdinalIgnoreCase))
        {
            return CsvParserKind.V1;
        }
        if (normalized.Equals("V2", StringComparison.OrdinalIgnoreCase))
        {
            return CsvParserKind.V2;
        }

        throw new FormatException($"Invalid parser kind '{value}'. Expected 'V1' or 'V2'.");
    }
}

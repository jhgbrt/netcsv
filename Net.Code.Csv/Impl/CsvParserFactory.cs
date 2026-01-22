namespace Net.Code.Csv.Impl;

internal static class CsvParserFactory
{
    internal static ICsvParser Create(TextReader reader, BufferedCharReader charReader, CsvLayout layout, CsvBehaviour behaviour)
    {
        var kind = CsvParserSelector.GetKind(behaviour);
        return kind switch
        {
            CsvParserKind.V1 => new CsvParser(reader, charReader, layout, behaviour),
            CsvParserKind.V2 => throw new NotSupportedException("Span-based parser is not implemented yet."),
            _ => new CsvParser(reader, charReader, layout, behaviour)
        };
    }
}

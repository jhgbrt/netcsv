namespace Net.Code.Csv.Impl;

internal static class CsvParserFactory
{
    internal static ICsvParser Create(TextReader reader, BufferedCharReader charReader, CsvLayout layout, CsvBehaviour behaviour)
    {
        var kind = CsvParserSelector.GetKind(behaviour);
        return kind switch
        {
            CsvParserKind.V1 => new V1.CsvParser(reader, charReader, layout, behaviour),
            CsvParserKind.V2 => new V2.CsvParserV2(reader, charReader, layout, behaviour),
            _ => new V2.CsvParserV2(reader, charReader, layout, behaviour)
        };
    }
}

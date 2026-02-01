namespace Net.Code.Csv.Impl;

internal static class CsvParserFactory
{
    internal static ICsvParser Create(TextReader reader, BufferedCharReader charReader, CsvLayout layout, CsvBehaviour behaviour)
    {
        return new CsvParser(reader, charReader, layout, behaviour);
    }
}

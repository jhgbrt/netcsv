namespace Net.Code.Csv.Impl
{
    internal static class CharEx
    {
        public static bool IsNewLine(this char c) => c == '\r' || c == '\n';
    }
}
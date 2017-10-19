namespace Net.Code.Csv.Impl
{
    static class CharEx
    {
        public static bool IsNewLine(this char c) => c == '\r' || c == '\n';
    }
}
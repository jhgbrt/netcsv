namespace Net.Code.Csv.Impl
{
    internal struct Location
    {
        public readonly int Line;
        public readonly int Column;

        private Location(int lineNumber, int columnNumber)
        {
            Line = lineNumber;
            Column = columnNumber;
        }

        public static Location Origin() => new Location(0, 0);
        public Location NextColumn() => new Location(Line, Column + 1);
        public Location NextLine() => new Location(Line + 1, 0);
        public override string ToString() => $"{Line},{Column}";
    }
}
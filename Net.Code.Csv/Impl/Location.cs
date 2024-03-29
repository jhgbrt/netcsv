﻿namespace Net.Code.Csv.Impl;

internal record struct Location(int Line, int Column)
{
    public static Location Origin() => new (0, 0);
    public Location NextColumn() => this with { Column = Column + 1 };
    public Location NextLine() => this with { Line = Line + 1, Column = 0 };
    public override string ToString() => $"{Line},{Column}";
}

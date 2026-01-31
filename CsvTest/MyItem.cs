using System;

namespace CsvTest
{
    public record MyItem(
        string First,
        string Last,
        DateTime BirthDate,
        int Quantity,
        decimal Price,
        string Description,
        int IntValue,
        double DoubleValue,
        TimeSpan Duration);
}

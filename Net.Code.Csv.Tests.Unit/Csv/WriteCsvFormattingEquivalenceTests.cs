namespace Net.Code.Csv.Tests.Unit.Csv;

/// <summary>
/// Verifies that the writer formats each supported type exactly as
/// <see cref="Net.Code.Csv.Impl.Converter.ToString(object, string)"/> (the TypeDescriptor based reference) does,
/// for many values and cultures (including cultures with non-ASCII digits, minus signs and separators).
/// </summary>
public class WriteCsvFormattingEquivalenceTests
{
    public record Holder<T>(T V);

    static readonly string[] CultureNames = ["", "nl-BE", "en-US", "fr-FR", "de-CH", "sv-SE", "nb-NO", "ar-SA", "fa-IR", "hi-IN", "ja-JP"];

    static void AssertSame<T>(IEnumerable<T> values)
    {
        foreach (var name in CultureNames)
        {
            var culture = name == "" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);
            var converter = new Net.Code.Csv.Impl.Converter(culture);
            foreach (var value in values)
            {
                var expected = new StringBuilder(converter.ToString(value, null)).QuoteIfNecessary('"', ',', '"').ToString() + Environment.NewLine;
                var actual = WriteCsv.ToString([new Holder<T>(value)], cultureInfo: culture);
                Assert.True(expected == actual, $"{typeof(T).Name} {value} [{name}]: expected '{expected}' but got '{actual}'");
                var boxed = WriteCsv.ToString([new Holder<object>(value)], cultureInfo: culture);
                Assert.Equal(expected, boxed);
            }
        }
    }

    static IEnumerable<long> Longs()
    {
        var rnd = new Random(42);
        long[] fixedValues = [0, 1, -1, 9, 10, -10, 999, 1000, -1000, 1234567, int.MinValue, int.MaxValue, long.MinValue, long.MaxValue];
        foreach (var v in fixedValues) yield return v;
        for (var i = 0; i < 200; i++) yield return rnd.NextInt64(long.MinValue, long.MaxValue) >> rnd.Next(0, 63);
    }

    static IEnumerable<double> Doubles()
    {
        var rnd = new Random(7);
        double[] fixedValues = [0.0, -0.0, 1, -1, 0.1, 0.2 + 0.1, 1e15, 1e16, 1e17, 1e-5, 1e-7, 123456789.123456789, double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity, Math.PI, -Math.E];
        foreach (var v in fixedValues) yield return v;
        for (var i = 0; i < 300; i++) yield return (rnd.NextDouble() - 0.5) * Math.Pow(10, rnd.Next(-30, 30));
    }

    static IEnumerable<decimal> Decimals()
    {
        var rnd = new Random(3);
        decimal[] fixedValues = [0m, -0m, 0.0m, 1.10m, -1.10m, 0.000001m, 123456789.123456789m, decimal.MaxValue, decimal.MinValue, decimal.One / 3];
        foreach (var v in fixedValues) yield return v;
        for (var i = 0; i < 200; i++) yield return new decimal(rnd.Next(), rnd.Next(), rnd.Next(), rnd.Next(2) == 0, (byte)rnd.Next(0, 29));
    }

    [Fact] public void Int32() => AssertSame(Longs().Select(v => (int)v));
    [Fact] public void NullableInt32() => AssertSame(Longs().Select(v => (int?)v).Append(null));
    [Fact] public void Int64() => AssertSame(Longs());
    [Fact] public void NullableInt64() => AssertSame(Longs().Select(v => (long?)v).Append(null));
    [Fact] public void Int16() => AssertSame(Longs().Select(v => (short)v));
    [Fact] public void SByte() => AssertSame(Longs().Select(v => (sbyte)v));
    [Fact] public void Byte() => AssertSame(Longs().Select(v => (byte)v));
    [Fact] public void UInt16() => AssertSame(Longs().Select(v => (ushort)v));
    [Fact] public void UInt32() => AssertSame(Longs().Select(v => (uint)v));
    [Fact] public void UInt64() => AssertSame(Longs().Select(v => (ulong)v));
    [Fact] public void Double() => AssertSame(Doubles());
    [Fact] public void NullableDouble() => AssertSame(Doubles().Select(v => (double?)v).Append(null));
    [Fact] public void Single() => AssertSame(Doubles().Select(v => (float)v));
    [Fact] public void NullableSingle() => AssertSame(Doubles().Select(v => (float?)v).Append(null));
    [Fact] public void Decimal() => AssertSame(Decimals());
    [Fact] public void NullableDecimal() => AssertSame(Decimals().Select(v => (decimal?)v).Append(null));
    [Fact] public void Char() => AssertSame(new[] { 'a', '\0', ',', '"', '\n', '\r', ' ', 'é', '￿' });
    [Fact] public void NullableChar() => AssertSame(new char?[] { 'a', '\0', ',', null });
    [Fact] public void Boolean() => AssertSame(new[] { true, false });
    [Fact] public void NullableBoolean() => AssertSame(new bool?[] { true, false, null });
    [Fact] public void Guid() => AssertSame(new System.Guid?[] { System.Guid.Empty, new System.Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), null });
    [Fact] public void String() => AssertSame(new[] { null, "", "a", "a,b", "a\"b", "\"", "\r\n", "x\ny", "∞" });

    [Fact]
    public void DateTime() => AssertSame(new System.DateTime?[]
    {
        System.DateTime.MinValue, System.DateTime.MaxValue, new System.DateTime(2021, 3, 4, 5, 6, 7, 890, DateTimeKind.Utc),
        new System.DateTime(2021, 3, 4, 5, 6, 7, DateTimeKind.Local), new System.DateTime(637_500_000_000_123_456), null
    });

    [Fact]
    public void DateTimeOffset() => AssertSame(new System.DateTimeOffset?[]
    {
        System.DateTimeOffset.MinValue, System.DateTimeOffset.MaxValue,
        new System.DateTimeOffset(2020, 11, 13, 10, 20, 30, System.TimeSpan.FromMinutes(-330)), null
    });

    [Fact]
    public void TimeSpan() => AssertSame(new System.TimeSpan?[]
    {
        System.TimeSpan.Zero, System.TimeSpan.MinValue, System.TimeSpan.MaxValue, new System.TimeSpan(-1, -2, -3, -4, -5),
        System.TimeSpan.FromTicks(1), null
    });
}

using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;

namespace Net.Code.Csv.Tests.Unit.Csv;

/// <summary>
/// Characterization tests that pin the exact output of the CSV writer, captured from the
/// reflection/TypeDescriptor based implementation. Any change in output is a regression.
/// </summary>
public class WriteCsvCharacterizationTests
{
    public enum Color { Red, Green = 5 }

    [Flags]
    public enum Perms { None = 0, Read = 1, Write = 2 }

    public class AllTypes
    {
        public string S { get; set; }
        public string NullS { get; set; }
        public bool B { get; set; }
        [CsvFormat("Y|N")] public bool BF { get; set; }
        public bool? NB { get; set; }
        [CsvFormat("ja|nee")] public bool? NBF { get; set; }
        public byte U8 { get; set; }
        public sbyte I8 { get; set; }
        public short I16 { get; set; }
        public ushort U16 { get; set; }
        public int I32 { get; set; }
        public uint U32 { get; set; }
        public long I64 { get; set; }
        public ulong U64 { get; set; }
        public float F32 { get; set; }
        public double F64 { get; set; }
        public decimal Dec { get; set; }
        [CsvFormat("N2")] public decimal DecIgnoredFormat { get; set; }
        [CsvFormat("X")] public int IntIgnoredFormat { get; set; }
        public char C { get; set; }
        public char NulChar { get; set; }
        public Guid G { get; set; }
        public DateTime Dt { get; set; }
        [CsvFormat("yyyy-MM-dd HH:mm:ss.fff")] public DateTime DtF { get; set; }
        [CsvFormat("D")] public DateTime DtLong { get; set; }
        public DateTimeOffset Dto { get; set; }
        [CsvFormat("dd/MM/yyyy HH:mm zzz")] public DateTimeOffset DtoF { get; set; }
        public TimeSpan Ts { get; set; }
        [CsvFormat("g")] public TimeSpan TsF { get; set; }
        [CsvFormat("G")] public TimeSpan TsG { get; set; }
        public Color E { get; set; }
        public Perms Fl { get; set; }
        public byte? NU8 { get; set; }
        public sbyte? NI8 { get; set; }
        public short? NI16 { get; set; }
        public ushort? NU16 { get; set; }
        public int? NI32 { get; set; }
        public uint? NU32 { get; set; }
        public long? NI64 { get; set; }
        public ulong? NU64 { get; set; }
        public float? NF32 { get; set; }
        public double? NF64 { get; set; }
        public decimal? NDec { get; set; }
        public char? NC { get; set; }
        public Guid? NG { get; set; }
        public DateTime? NDt { get; set; }
        [CsvFormat("yyyyMMdd")] public DateTime? NDtF { get; set; }
        public DateTimeOffset? NDto { get; set; }
        public TimeSpan? NTs { get; set; }
        [CsvFormat("hh\\:mm")] public TimeSpan? NTsF { get; set; }
        public Color? NE { get; set; }
        public Custom Cust { get; set; }
        public Amount Amt { get; set; }
        public Amount? NAmt { get; set; }
        public object Obj { get; set; }
        [Column("Renamed Column")] public int Renamed { get; set; }
        public int[] Ints { get; set; }
        public static int StaticProp => 42;
    }

    public record SmallRecord(
        string Name,
        [CsvFormat("1|0")] bool Flag,
        [CsvFormat("yyyy/MM/dd")] DateTime When,
        double? Value);

    static AllTypes Full() => new()
    {
        S = "plain",
        NullS = null,
        B = true,
        BF = false,
        NB = false,
        NBF = true,
        U8 = 255,
        I8 = -128,
        I16 = -32768,
        U16 = 65535,
        I32 = -123456789,
        U32 = uint.MaxValue,
        I64 = long.MinValue,
        U64 = ulong.MaxValue,
        F32 = -0.1f,
        F64 = -1234567.891,
        Dec = -1234567.8900m,
        DecIgnoredFormat = 1234.5m,
        IntIgnoredFormat = 255,
        C = 'x',
        NulChar = '\0',
        G = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"),
        Dt = new DateTime(2021, 3, 4, 5, 6, 7, 890, DateTimeKind.Unspecified),
        DtF = new DateTime(2021, 3, 4, 5, 6, 7, 890, DateTimeKind.Utc),
        DtLong = new DateTime(2021, 3, 4),
        Dto = new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(-5)),
        DtoF = new DateTimeOffset(2020, 11, 13, 10, 20, 30, TimeSpan.FromHours(2)),
        Ts = new TimeSpan(-1, -2, -3, -4, -5),
        TsF = new TimeSpan(1, 2, 3, 4, 500),
        TsG = new TimeSpan(1, 2, 3, 4, 500),
        E = Color.Green,
        Fl = Perms.Read | Perms.Write,
        NU8 = 1,
        NI8 = -1,
        NI16 = -2,
        NU16 = 2,
        NI32 = -3,
        NU32 = 3,
        NI64 = -4,
        NU64 = 4,
        NF32 = 1.5f,
        NF64 = 0.1 + 0.2,
        NDec = 0.000001m,
        NC = ';',
        NG = Guid.Empty,
        NDt = new DateTime(1999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        NDtF = new DateTime(1999, 12, 31),
        NDto = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
        NTs = TimeSpan.FromMilliseconds(1),
        NTsF = new TimeSpan(13, 14, 0),
        NE = Color.Red,
        Cust = new Custom("cu\"st,om;"),
        Amt = new Amount("EUR", 12.5m),
        NAmt = new Amount("USD", -0.25m),
        Obj = 1234.5,
        Renamed = 7,
        Ints = [1, 2],
    };

    static AllTypes Defaults() => new();

    static AllTypes Specials() => new()
    {
        S = "a\"b,c;d\ne\rf\"\"",
        NullS = "\"",
        F32 = float.NaN,
        F64 = double.PositiveInfinity,
        NF32 = float.NegativeInfinity,
        NF64 = -0.0,
        Dec = decimal.MaxValue,
        NDec = decimal.MinValue,
        I32 = int.MinValue,
        C = '"',
        NC = '\n',
        E = Color.Red,
        NE = Color.Green,
        Fl = (Perms)8,
        Obj = "text,with;delims",
        Ints = [],
    };

    static AllTypes[] Items() => [Full(), Defaults(), Specials()];

    static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    static string WriteSync<T>(IEnumerable<T> items, char delimiter, char quote, char escape, bool hasHeaders, CultureInfo culture)
        => WriteCsv.ToString(items, delimiter, quote, escape, hasHeaders, culture);

    static async Task<string> WriteAsync<T>(IEnumerable<T> items, char delimiter, char quote, char escape, bool hasHeaders, CultureInfo culture)
    {
        using var stream = new MemoryStream();
        await WriteCsv.ToStream(ToAsync(items), stream, new UTF8Encoding(false), delimiter, quote, escape, hasHeaders, culture);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static string WriteStream<T>(IEnumerable<T> items, char delimiter, char quote, char escape, bool hasHeaders, CultureInfo culture)
    {
        using var stream = new MemoryStream();
        WriteCsv.ToStream(items, stream, new UTF8Encoding(false), delimiter, quote, escape, hasHeaders, culture);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static CultureInfo Culture(string name) => name == "" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);

    public static IEnumerable<object[]> Cases()
    {
        foreach (var culture in new[] { "", "nl-BE" })
        {
            yield return [culture, ',', '"', '"', true];
            yield return [culture, ';', '\'', '\\', false];
        }
    }

    static SmallRecord[] Records() =>
    [
        new SmallRecord("x,y", true, new DateTime(2001, 2, 3), -1.5),
        new SmallRecord(null, false, default, null)
    ];

    static string Key(string kind, string culture, char d, char q, char e, bool h) => $"{kind}|{culture}|{d}|{q}|{e}|{h}";

    static string Expected(string key) => ExpectedOutput[key].Replace("{NL}", Environment.NewLine);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Sync_AllTypes_MatchesCapturedOutput(string culture, char d, char q, char e, bool h)
    {
        var expected = Expected(Key("S", culture, d, q, e, h));
        Assert.Equal(expected, WriteSync(Items(), d, q, e, h, Culture(culture)));
        Assert.Equal(expected, WriteStream(Items(), d, q, e, h, Culture(culture)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Async_AllTypes_MatchesCapturedOutput(string culture, char d, char q, char e, bool h)
    {
        Assert.Equal(Expected(Key("A", culture, d, q, e, h)), await WriteAsync(Items(), d, q, e, h, Culture(culture)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Sync_Record_MatchesCapturedOutput(string culture, char d, char q, char e, bool h)
    {
        Assert.Equal(Expected(Key("RS", culture, d, q, e, h)), WriteSync(Records(), d, q, e, h, Culture(culture)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Async_Record_MatchesCapturedOutput(string culture, char d, char q, char e, bool h)
    {
        Assert.Equal(Expected(Key("RA", culture, d, q, e, h)), await WriteAsync(Records(), d, q, e, h, Culture(culture)));
    }

    [Fact]
    public void WritingTwice_ProducesSameOutput()
    {
        var first = WriteSync(Items(), ',', '"', '"', true, CultureInfo.InvariantCulture);
        var second = WriteSync(Items(), ',', '"', '"', true, CultureInfo.InvariantCulture);
        Assert.Equal(first, second);
    }

    [Fact]
    public void EmptySequence_WithHeaders_WritesOnlyHeader()
    {
        var result = WriteSync(Array.Empty<SmallRecord>(), ',', '"', '"', true, CultureInfo.InvariantCulture);
        Assert.Equal($"Name,Flag,When,Value{Environment.NewLine}", result);
    }

    [Fact]
    public void UndefinedEnumValue_Throws()
    {
        var item = new AllTypes { E = (Color)42 };
        Assert.Throws<ArgumentException>(() => WriteSync([item], ',', '"', '"', false, CultureInfo.InvariantCulture));
    }

    public class InvalidBoolFormat
    {
        [CsvFormat("yes")] public bool? Flag { get; set; }
    }

    [Fact]
    public void InvalidBooleanFormat_OnlyThrowsWhenValueIsWritten()
    {
        Assert.Equal($"{Environment.NewLine}", WriteSync([new InvalidBoolFormat()], ',', '"', '"', false, CultureInfo.InvariantCulture));
        Assert.Throws<FormatException>(() => WriteSync([new InvalidBoolFormat { Flag = true }], ',', '"', '"', false, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void LongString_WithManyQuotes_IsEscaped()
    {
        var value = string.Concat(Enumerable.Repeat("ab\"", 10_000));
        var result = WriteSync([new { Value = value }], ',', '"', '\\', false, CultureInfo.InvariantCulture);
        Assert.Equal("\"" + value.Replace("\"", "\\\"") + "\"" + Environment.NewLine, result);
    }

    [Fact]
    public void NullItem_Throws()
    {
        Assert.ThrowsAny<Exception>(() => WriteSync(new SmallRecord[] { null }, ',', '"', '"', false, CultureInfo.InvariantCulture));
    }

    public class ThrowingGetter
    {
        public int Value => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void ThrowingGetter_ThrowsTargetInvocationException()
    {
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => WriteSync([new ThrowingGetter()], ',', '"', '"', false, CultureInfo.InvariantCulture));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    public class SetOnly
    {
        private int _value;
        public int Value { set => _value = value; }
        public int Other => _value;
    }

    [Fact]
    public void SetOnlyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() => WriteSync([new SetOnly()], ',', '"', '"', false, CultureInfo.InvariantCulture));
    }

    public struct StructItem
    {
        public int A { get; set; }
        public string B { get; set; }
        public double? C { get; set; }
    }

    [Fact]
    public async Task StructItems_AreWritten()
    {
        StructItem[] items = [new StructItem { A = -1, B = "x;y", C = 2.5 }, default];
        var expected = $"A;B;C{Environment.NewLine}-1;'x;y';2,5{Environment.NewLine}0;;{Environment.NewLine}";
        Assert.Equal(expected, WriteSync(items, ';', '\'', '\'', true, CultureInfo.GetCultureInfo("nl-BE")));
        Assert.Equal(expected, await WriteAsync(items, ';', '\'', '\'', true, CultureInfo.GetCultureInfo("nl-BE")));
    }

    [Fact]
    public void ManyRowsAndWideValues_MatchConcatenation()
    {
        var items = Enumerable.Range(0, 2000).Select(i => new SmallRecord(new string('q', i % 700) + (i % 3 == 0 ? "\"" : ""), i % 2 == 0, new DateTime(2000, 1, 1).AddDays(i), i * -0.25)).ToArray();
        var expected = string.Concat(items.Select(r => WriteSync([r], ',', '"', '"', false, CultureInfo.InvariantCulture)));
        Assert.Equal(expected, WriteSync(items, ',', '"', '"', false, CultureInfo.InvariantCulture));
    }

    static readonly Dictionary<string, string> ExpectedOutput = new()
    {
        ["S||,|\"|\"|True"] = "S,NullS,B,BF,NB,NBF,U8,I8,I16,U16,I32,U32,I64,U64,F32,F64,Dec,DecIgnoredFormat,IntIgnoredFormat,C,NulChar,G,Dt,DtF,DtLong,Dto,DtoF,Ts,TsF,TsG,E,Fl,NU8,NI8,NI16,NU16,NI32,NU32,NI64,NU64,NF32,NF64,NDec,NC,NG,NDt,NDtF,NDto,NTs,NTsF,NE,Cust,Amt,NAmt,Obj,Renamed Column,StaticProp{NL}plain,,True,N,False,ja,255,-128,-32768,65535,-123456789,4294967295,-9223372036854775808,18446744073709551615,-0.1,-1234567.891,-1234567.8900,1234.5,255,x,,0f8fad5b-d9cb-469f-a165-70867728950e,2021-03-04T05:06:07.8900000,2021-03-04 05:06:07.890,\"Thursday, 04 March 2021\",2020-11-13T10:20:30.0000000-05:00,13/11/2020 10:20 +02:00,-1.02:03:04.0050000,1:2:03:04.5,1:02:03:04.5000000,Green,\"Read, Write\",1,-1,-2,2,-3,3,-4,4,1.5,0.30000000000000004,0.000001,;,00000000-0000-0000-0000-000000000000,1999-12-31T23:59:59.0000000Z,19991231,2000-01-01T00:00:00.0000000+00:00,00:00:00.0010000,13:14,Red,\"cu\"\"st,om;\",EUR 12.5,USD -0.25,1234.5,7,42{NL},,False,N,,,0,0,0,0,0,0,0,0,0,0,0,0,0,,,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,\"Monday, 01 January 0001\",0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,0:00:00:00.0000000,Red,None,,,,,,,,,,,,,,,,,,,,, 0,,,0,42{NL}\"a\"\"b,c;d\ne\rf\"\"\"\"\",\"\"\"\",False,N,,,0,0,0,0,-2147483648,0,0,0,NaN,Infinity,79228162514264337593543950335,0,0,\"\"\"\",,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,\"Monday, 01 January 0001\",0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,0:00:00:00.0000000,Red,8,,,,,,,,,-Infinity,-0,-79228162514264337593543950335,\"\n\",,,,,,,Green,, 0,,\"text,with;delims\",0,42{NL}",
        ["A||,|\"|\"|True"] = "S,NullS,B,BF,NB,NBF,U8,I8,I16,U16,I32,U32,I64,U64,F32,F64,Dec,DecIgnoredFormat,IntIgnoredFormat,C,NulChar,G,Dt,DtF,DtLong,Dto,DtoF,Ts,TsF,TsG,E,Fl,NU8,NI8,NI16,NU16,NI32,NU32,NI64,NU64,NF32,NF64,NDec,NC,NG,NDt,NDtF,NDto,NTs,NTsF,NE,Cust,Amt,NAmt,Obj,Renamed,Ints,StaticProp{NL}plain,,True,N,False,ja,255,-128,-32768,65535,-123456789,4294967295,-9223372036854775808,18446744073709551615,-0.1,-1234567.891,-1234567.8900,1234.5,255,x,,0f8fad5b-d9cb-469f-a165-70867728950e,2021-03-04T05:06:07.8900000,2021-03-04 05:06:07.890,\"Thursday, 04 March 2021\",2020-11-13T10:20:30.0000000-05:00,13/11/2020 10:20 +02:00,-1.02:03:04.0050000,1:2:03:04.5,1:02:03:04.5000000,Green,\"Read, Write\",1,-1,-2,2,-3,3,-4,4,1.5,0.30000000000000004,0.000001,;,00000000-0000-0000-0000-000000000000,1999-12-31T23:59:59.0000000Z,19991231,2000-01-01T00:00:00.0000000+00:00,00:00:00.0010000,13:14,Red,\"cu\"\"st,om;\",EUR 12.5,USD -0.25,1234.5,7,Int32[] Array,42{NL},,False,N,,,0,0,0,0,0,0,0,0,0,0,0,0,0,,,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,\"Monday, 01 January 0001\",0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,0:00:00:00.0000000,Red,None,,,,,,,,,,,,,,,,,,,,, 0,,,0,,42{NL}\"a\"\"b,c;d\ne\rf\"\"\"\"\",\"\"\"\",False,N,,,0,0,0,0,-2147483648,0,0,0,NaN,Infinity,79228162514264337593543950335,0,0,\"\"\"\",,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,\"Monday, 01 January 0001\",0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,0:00:00:00.0000000,Red,8,,,,,,,,,-Infinity,-0,-79228162514264337593543950335,\"\n\",,,,,,,Green,, 0,,\"text,with;delims\",0,Int32[] Array,42{NL}",
        ["RS||,|\"|\"|True"] = "Name,Flag,When,Value{NL}\"x,y\",1,2001/02/03,-1.5{NL},0,0001/01/01,{NL}",
        ["RA||,|\"|\"|True"] = "Name,Flag,When,Value{NL}\"x,y\",1,2001/02/03,-1.5{NL},0,0001/01/01,{NL}",
        ["S||;|'|\\|False"] = "plain;;True;N;False;ja;255;-128;-32768;65535;-123456789;4294967295;-9223372036854775808;18446744073709551615;-0.1;-1234567.891;-1234567.8900;1234.5;255;x;;0f8fad5b-d9cb-469f-a165-70867728950e;2021-03-04T05:06:07.8900000;2021-03-04 05:06:07.890;Thursday, 04 March 2021;2020-11-13T10:20:30.0000000-05:00;13/11/2020 10:20 +02:00;-1.02:03:04.0050000;1:2:03:04.5;1:02:03:04.5000000;Green;Read, Write;1;-1;-2;2;-3;3;-4;4;1.5;0.30000000000000004;0.000001;';';00000000-0000-0000-0000-000000000000;1999-12-31T23:59:59.0000000Z;19991231;2000-01-01T00:00:00.0000000+00:00;00:00:00.0010000;13:14;Red;'cu\"st,om;';EUR 12.5;USD -0.25;1234.5;7;42{NL};;False;N;;;0;0;0;0;0;0;0;0;0;0;0;0;0;;;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;Monday, 01 January 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00.0000000;Red;None;;;;;;;;;;;;;;;;;;;;; 0;;;0;42{NL}'a\"b,c;d\ne\rf\"\"';\";False;N;;;0;0;0;0;-2147483648;0;0;0;NaN;Infinity;79228162514264337593543950335;0;0;\";;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;Monday, 01 January 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00.0000000;Red;8;;;;;;;;;-Infinity;-0;-79228162514264337593543950335;'\n';;;;;;;Green;; 0;;'text,with;delims';0;42{NL}",
        ["A||;|'|\\|False"] = "plain;;True;N;False;ja;255;-128;-32768;65535;-123456789;4294967295;-9223372036854775808;18446744073709551615;-0.1;-1234567.891;-1234567.8900;1234.5;255;x;;0f8fad5b-d9cb-469f-a165-70867728950e;2021-03-04T05:06:07.8900000;2021-03-04 05:06:07.890;Thursday, 04 March 2021;2020-11-13T10:20:30.0000000-05:00;13/11/2020 10:20 +02:00;-1.02:03:04.0050000;1:2:03:04.5;1:02:03:04.5000000;Green;Read, Write;1;-1;-2;2;-3;3;-4;4;1.5;0.30000000000000004;0.000001;';';00000000-0000-0000-0000-000000000000;1999-12-31T23:59:59.0000000Z;19991231;2000-01-01T00:00:00.0000000+00:00;00:00:00.0010000;13:14;Red;'cu\"st,om;';EUR 12.5;USD -0.25;1234.5;7;Int32[] Array;42{NL};;False;N;;;0;0;0;0;0;0;0;0;0;0;0;0;0;;;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;Monday, 01 January 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00.0000000;Red;None;;;;;;;;;;;;;;;;;;;;; 0;;;0;;42{NL}'a\"b,c;d\ne\rf\"\"';\";False;N;;;0;0;0;0;-2147483648;0;0;0;NaN;Infinity;79228162514264337593543950335;0;0;\";;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;Monday, 01 January 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00.0000000;Red;8;;;;;;;;;-Infinity;-0;-79228162514264337593543950335;'\n';;;;;;;Green;; 0;;'text,with;delims';0;Int32[] Array;42{NL}",
        ["RS||;|'|\\|False"] = "x,y;1;2001/02/03;-1.5{NL};0;0001/01/01;{NL}",
        ["RA||;|'|\\|False"] = "x,y;1;2001/02/03;-1.5{NL};0;0001/01/01;{NL}",
        ["S|nl-BE|,|\"|\"|True"] = "S,NullS,B,BF,NB,NBF,U8,I8,I16,U16,I32,U32,I64,U64,F32,F64,Dec,DecIgnoredFormat,IntIgnoredFormat,C,NulChar,G,Dt,DtF,DtLong,Dto,DtoF,Ts,TsF,TsG,E,Fl,NU8,NI8,NI16,NU16,NI32,NU32,NI64,NU64,NF32,NF64,NDec,NC,NG,NDt,NDtF,NDto,NTs,NTsF,NE,Cust,Amt,NAmt,Obj,Renamed Column,StaticProp{NL}plain,,True,N,False,ja,255,-128,-32768,65535,-123456789,4294967295,-9223372036854775808,18446744073709551615,\"-0,1\",\"-1234567,891\",\"-1234567,8900\",\"1234,5\",255,x,,0f8fad5b-d9cb-469f-a165-70867728950e,2021-03-04T05:06:07.8900000,2021-03-04 05:06:07.890,donderdag 4 maart 2021,2020-11-13T10:20:30.0000000-05:00,13/11/2020 10:20 +02:00,-1.02:03:04.0050000,\"1:2:03:04,5\",\"1:02:03:04,5000000\",Green,\"Read, Write\",1,-1,-2,2,-3,3,-4,4,\"1,5\",\"0,30000000000000004\",\"0,000001\",;,00000000-0000-0000-0000-000000000000,1999-12-31T23:59:59.0000000Z,19991231,2000-01-01T00:00:00.0000000+00:00,00:00:00.0010000,13:14,Red,\"cu\"\"st,om;\",\"EUR 12,5\",\"USD -0,25\",\"1234,5\",7,42{NL},,False,N,,,0,0,0,0,0,0,0,0,0,0,0,0,0,,,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,maandag 1 januari 0001,0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,\"0:00:00:00,0000000\",Red,None,,,,,,,,,,,,,,,,,,,,, 0,,,0,42{NL}\"a\"\"b,c;d\ne\rf\"\"\"\"\",\"\"\"\",False,N,,,0,0,0,0,-2147483648,0,0,0,NaN,\u221E,79228162514264337593543950335,0,0,\"\"\"\",,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,maandag 1 januari 0001,0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,\"0:00:00:00,0000000\",Red,8,,,,,,,,,-\u221E,-0,-79228162514264337593543950335,\"\n\",,,,,,,Green,, 0,,\"text,with;delims\",0,42{NL}",
        ["A|nl-BE|,|\"|\"|True"] = "S,NullS,B,BF,NB,NBF,U8,I8,I16,U16,I32,U32,I64,U64,F32,F64,Dec,DecIgnoredFormat,IntIgnoredFormat,C,NulChar,G,Dt,DtF,DtLong,Dto,DtoF,Ts,TsF,TsG,E,Fl,NU8,NI8,NI16,NU16,NI32,NU32,NI64,NU64,NF32,NF64,NDec,NC,NG,NDt,NDtF,NDto,NTs,NTsF,NE,Cust,Amt,NAmt,Obj,Renamed,Ints,StaticProp{NL}plain,,True,N,False,ja,255,-128,-32768,65535,-123456789,4294967295,-9223372036854775808,18446744073709551615,\"-0,1\",\"-1234567,891\",\"-1234567,8900\",\"1234,5\",255,x,,0f8fad5b-d9cb-469f-a165-70867728950e,2021-03-04T05:06:07.8900000,2021-03-04 05:06:07.890,donderdag 4 maart 2021,2020-11-13T10:20:30.0000000-05:00,13/11/2020 10:20 +02:00,-1.02:03:04.0050000,\"1:2:03:04,5\",\"1:02:03:04,5000000\",Green,\"Read, Write\",1,-1,-2,2,-3,3,-4,4,\"1,5\",\"0,30000000000000004\",\"0,000001\",;,00000000-0000-0000-0000-000000000000,1999-12-31T23:59:59.0000000Z,19991231,2000-01-01T00:00:00.0000000+00:00,00:00:00.0010000,13:14,Red,\"cu\"\"st,om;\",\"EUR 12,5\",\"USD -0,25\",\"1234,5\",7,Int32[] Array,42{NL},,False,N,,,0,0,0,0,0,0,0,0,0,0,0,0,0,,,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,maandag 1 januari 0001,0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,\"0:00:00:00,0000000\",Red,None,,,,,,,,,,,,,,,,,,,,, 0,,,0,,42{NL}\"a\"\"b,c;d\ne\rf\"\"\"\"\",\"\"\"\",False,N,,,0,0,0,0,-2147483648,0,0,0,NaN,\u221E,79228162514264337593543950335,0,0,\"\"\"\",,00000000-0000-0000-0000-000000000000,0001-01-01T00:00:00.0000000,0001-01-01 00:00:00.000,maandag 1 januari 0001,0001-01-01T00:00:00.0000000+00:00,01/01/0001 00:00 +00:00,00:00:00,0:00:00,\"0:00:00:00,0000000\",Red,8,,,,,,,,,-\u221E,-0,-79228162514264337593543950335,\"\n\",,,,,,,Green,, 0,,\"text,with;delims\",0,Int32[] Array,42{NL}",
        ["RS|nl-BE|,|\"|\"|True"] = "Name,Flag,When,Value{NL}\"x,y\",1,2001/02/03,\"-1,5\"{NL},0,0001/01/01,{NL}",
        ["RA|nl-BE|,|\"|\"|True"] = "Name,Flag,When,Value{NL}\"x,y\",1,2001/02/03,\"-1,5\"{NL},0,0001/01/01,{NL}",
        ["S|nl-BE|;|'|\\|False"] = "plain;;True;N;False;ja;255;-128;-32768;65535;-123456789;4294967295;-9223372036854775808;18446744073709551615;-0,1;-1234567,891;-1234567,8900;1234,5;255;x;;0f8fad5b-d9cb-469f-a165-70867728950e;2021-03-04T05:06:07.8900000;2021-03-04 05:06:07.890;donderdag 4 maart 2021;2020-11-13T10:20:30.0000000-05:00;13/11/2020 10:20 +02:00;-1.02:03:04.0050000;1:2:03:04,5;1:02:03:04,5000000;Green;Read, Write;1;-1;-2;2;-3;3;-4;4;1,5;0,30000000000000004;0,000001;';';00000000-0000-0000-0000-000000000000;1999-12-31T23:59:59.0000000Z;19991231;2000-01-01T00:00:00.0000000+00:00;00:00:00.0010000;13:14;Red;'cu\"st,om;';EUR 12,5;USD -0,25;1234,5;7;42{NL};;False;N;;;0;0;0;0;0;0;0;0;0;0;0;0;0;;;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;maandag 1 januari 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00,0000000;Red;None;;;;;;;;;;;;;;;;;;;;; 0;;;0;42{NL}'a\"b,c;d\ne\rf\"\"';\";False;N;;;0;0;0;0;-2147483648;0;0;0;NaN;\u221E;79228162514264337593543950335;0;0;\";;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;maandag 1 januari 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00,0000000;Red;8;;;;;;;;;-\u221E;-0;-79228162514264337593543950335;'\n';;;;;;;Green;; 0;;'text,with;delims';0;42{NL}",
        ["A|nl-BE|;|'|\\|False"] = "plain;;True;N;False;ja;255;-128;-32768;65535;-123456789;4294967295;-9223372036854775808;18446744073709551615;-0,1;-1234567,891;-1234567,8900;1234,5;255;x;;0f8fad5b-d9cb-469f-a165-70867728950e;2021-03-04T05:06:07.8900000;2021-03-04 05:06:07.890;donderdag 4 maart 2021;2020-11-13T10:20:30.0000000-05:00;13/11/2020 10:20 +02:00;-1.02:03:04.0050000;1:2:03:04,5;1:02:03:04,5000000;Green;Read, Write;1;-1;-2;2;-3;3;-4;4;1,5;0,30000000000000004;0,000001;';';00000000-0000-0000-0000-000000000000;1999-12-31T23:59:59.0000000Z;19991231;2000-01-01T00:00:00.0000000+00:00;00:00:00.0010000;13:14;Red;'cu\"st,om;';EUR 12,5;USD -0,25;1234,5;7;Int32[] Array;42{NL};;False;N;;;0;0;0;0;0;0;0;0;0;0;0;0;0;;;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;maandag 1 januari 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00,0000000;Red;None;;;;;;;;;;;;;;;;;;;;; 0;;;0;;42{NL}'a\"b,c;d\ne\rf\"\"';\";False;N;;;0;0;0;0;-2147483648;0;0;0;NaN;\u221E;79228162514264337593543950335;0;0;\";;00000000-0000-0000-0000-000000000000;0001-01-01T00:00:00.0000000;0001-01-01 00:00:00.000;maandag 1 januari 0001;0001-01-01T00:00:00.0000000+00:00;01/01/0001 00:00 +00:00;00:00:00;0:00:00;0:00:00:00,0000000;Red;8;;;;;;;;;-\u221E;-0;-79228162514264337593543950335;'\n';;;;;;;Green;; 0;;'text,with;delims';0;Int32[] Array;42{NL}",
        ["RS|nl-BE|;|'|\\|False"] = "x,y;1;2001/02/03;-1,5{NL};0;0001/01/01;{NL}",
        ["RA|nl-BE|;|'|\\|False"] = "x,y;1;2001/02/03;-1,5{NL};0;0001/01/01;{NL}",
    };
}

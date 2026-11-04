namespace Net.Code.Csv.Impl;

/// <summary>
/// Formerly selected the parser implementation. Only one parser exists now, so this has no effect.
/// </summary>
[Obsolete("There is only one parser; this setting has no effect and will be removed in a future major version.")]
public enum CsvParserKind
{
    Default = 0,
    V1 = 1,
    V2 = 2
}

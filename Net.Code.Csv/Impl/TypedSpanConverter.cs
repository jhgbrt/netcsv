namespace Net.Code.Csv.Impl;

/// <summary>
/// Holds a strongly typed span converter for a schema column. The column's public
/// <see cref="CsvColumn.FromSpan"/> delegate is bound to this instance, so the typed
/// converter can be recovered from that delegate without adding members to
/// <see cref="CsvColumn"/>, and the typed read paths can avoid boxing.
/// </summary>
internal sealed class TypedSpanConverter<T>
{
    private readonly CsvSpanConverter<T> _convert;
    private readonly CsvSpanConverter<object> _fromSpan;

    private TypedSpanConverter(CsvSpanConverter<T> convert)
    {
        _convert = convert;
        _fromSpan = ToObject;
    }

    private object ToObject(ReadOnlySpan<char> value) => _convert(value);

    /// <summary>
    /// Creates the boxing <see cref="CsvSpanConverter{T}"/> of object to store in <see cref="CsvColumn.FromSpan"/>.
    /// </summary>
    public static CsvSpanConverter<object> Create(CsvSpanConverter<T> convert)
        => new TypedSpanConverter<T>(convert)._fromSpan;

    /// <summary>
    /// Returns the typed converter behind <paramref name="fromSpan"/> when that delegate is exactly
    /// the one created by <see cref="Create"/> for <typeparamref name="T"/>; otherwise null
    /// (e.g. a user-supplied or combined delegate), in which case callers use the object path.
    /// </summary>
    public static CsvSpanConverter<T> TryGet(CsvSpanConverter<object> fromSpan)
        => fromSpan?.Target is TypedSpanConverter<T> typed && ReferenceEquals(typed._fromSpan, fromSpan)
            ? typed._convert
            : null;
}

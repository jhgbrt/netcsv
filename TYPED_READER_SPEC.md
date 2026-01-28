# Typed Path Bypasses IDataReader

## Summary
Typed enumeration (`ReadCsv<T>()`, `AsEnumerable<T>()`, etc.) should no longer route through `IDataReader`. Instead, it should consume the **V2 parser’s** `CsvLineSlice` stream directly and map fields to a `T` instance using span-based converters. This removes `IDataRecord` overhead, boxing, and schema conversion on the object path in the typed flow.

## Goals
- Bypass `IDataReader` for typed `IEnumerable<T>` options.
- Reduce boxing and string materialization where possible.
- Preserve behavior for layout, trimming, missing fields, empty lines, headers, and schema mapping.
- Focus performance work on the V2 parser hot path only (V1 is legacy).

## Non-goals
- No public API changes (unless explicitly approved).
- No changes to `IDataReader`-based flows.
- No converter behavior changes for custom types unless explicitly requested.

---

## Current Typed Flow (Problem)
`AsEnumerable<T>` uses `IDataReader`:

```
IDataReader -> CsvDataReader.GetValue(...) -> object/boxing
           -> GetActivator() -> dynamic lambda -> T
```

This imposes:
- `IDataRecord` overhead for each field.
- Boxing/casts for value types.
- Schema conversion invoked via `IDataRecord` object path.

---

## Proposed Architecture

### New Internal Pipeline (V2 Only)
```
CsvParserV2 -> CsvLineSlice -> TypedRowReader<T> -> T
```

Key change: `AsEnumerable<T>` detects `CsvDataReader` and bypasses it:
- Use its internal parser state and schema.
- Or replace `CsvDataReader` usage with a new typed enumerator that uses `ICsvParser` directly.

---

## Core Types (Internal)

### 1) `ITypedRowSource`
```csharp
internal interface ITypedRowSource
{
    CsvHeader Header { get; }
    CsvLayout Layout { get; }
    CsvBehaviour Behaviour { get; }
    CsvSchema Schema { get; }
    bool Read(out CsvLineSlice line);
}
```

Implementation wraps:
- `ICsvParser` (V2 only for perf work).
- Handles header / first line caching if needed.
- Applies `EmptyLineAction`, `MissingFieldAction` as today (already in parser/builder).

### 2) `TypedRowReader<T>`
Consumes `ITypedRowSource`, produces `T`:
```csharp
internal sealed class TypedRowReader<T> : IEnumerable<T>
{
    private readonly ITypedRowSource _source;
    private readonly Func<CsvLineSlice, T> _activator;

    public IEnumerator<T> GetEnumerator()
    {
        while (_source.Read(out var line))
        {
            yield return _activator(line);
        }
    }
}
```

---

## Activator Design

### Mapping
Build column mappings once:
- If headers: map property name -> header ordinal.
- If no headers: ordinal = schema ordinal.
- If missing header: preserve current behavior (error/fallback).

### Converter Pipeline
We need typed converters without boxing:

**Option A (preferred):** Build activator using typed `CsvSpanConverter<TProp>`.
- For primitives: use `Converter` methods directly.
- For `string`: use `span.ToString()`.
- For nullable: return `default` when empty/NULL.

**Option B (fallback):** Use `CsvColumn.FromSpan` and cast.

### Expression Tree (fast path)
Generate once per `(Type T, CsvSchema)`:
```
Func<CsvLineSlice, T>
{
  var span = GetSpan(line, ordinal, out isNull);
  if (isNull or empty) -> default
  else -> converter(span)
}
```

Helper:
```csharp
internal static ReadOnlySpan<char> GetSpan(CsvLineSlice line, int ordinal, out bool isNull)
{
    var field = line.GetField(ordinal);
    isNull = field.IsNull;
    return field.Span;
}
```

---

## Behavior Preservation

### Null Handling
- If field is NULL or length 0, follow existing `GetSchemaRaw<T>` semantics.
- Use `AllowNull` from schema to decide default/null.

### Trimming & Quoted Handling
- Already enforced by parser + `CsvLineSliceBuilder`.
- Typed path should not re-trim.

### Missing Fields
- Keep current logic (handled in `CsvLineSliceBuilder.ToLine()`).

---

## Caching
- Cache activator per `(Type T, CsvSchema signature)` (similar to current).
- Cache converter delegates for primitives (reuse `Converter` instance).

---

## Integration Points

### `Extensions.AsEnumerable<T>(IDataReader)`
Current:
```
CsvSchema schema = reader is CsvDataReader r ? r.Schema : Schema.From<T>();
var activator = GetActivator<T>(schema);
while (reader.Read()) { yield return activator(reader); }
```

Replace with:
```
if (reader is CsvDataReader csvReader)
{
    var typedSource = new CsvParserRowSource(csvReader); // internal
    return new TypedRowReader<T>(typedSource);
}
else
{
    // keep IDataReader path for non-CsvDataReader
}
```

### `ReadCsv<T>()`
Instantiate a parser-based typed enumerable directly and skip `IDataReader`.

---

## Performance Expectations
- Remove per-field `IDataRecord` access + boxing.
- Avoid `GetValue(object)` allocation path.
- Lower per-row overhead in typed path, closer to parser core.

---

## Testing
- Typed output matches current behavior:
  - With headers vs without.
  - Missing fields and `EmptyLineAction`.
  - Null handling for nullable types.
  - Custom converter types (`Custom`), same results.

---

## Rollout Plan
1) Introduce `TypedRowReader<T>` and internal row source.
2) Wire `AsEnumerable<T>` for `CsvDataReader` to use typed path.
3) Add tests.
4) Profile vs old path (verify speedup).

---

## Open Questions
- Should typed path reuse `CsvSchemaBuilder` or bypass it for `T`?
- Should non-primitive types (like `Custom`) use `TypeConverter` or allow custom span converters?

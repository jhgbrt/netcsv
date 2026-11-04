# Span-Based Converter Spec

## Goal
Move all string-to-type conversion to `ReadOnlySpan<char>` to support the span-based CSV state machine and avoid per-field string allocations. This is a breaking API change.

## Non-Goals
- Do not change CSV parsing semantics (quotes, escapes, missing fields).
- Do not change public `ReadCsv`/`WriteCsv` entry points.
- Do not add a second converter implementation.

## Public API Changes (Breaking)
- `CsvColumn` now stores a span-based converter:
  - `CsvSpanConverter<object> FromSpan`
- New public delegates:
  - `CsvSpanConverter<T>(ReadOnlySpan<char> value)`
  - `CsvSpanConverterWithFormat<T>(ReadOnlySpan<char> value, string format)`
- `CsvSchemaBuilder.Add` overloads now accept `CsvSpanConverter<T>`.

## Converter API
`Converter` remains the single conversion implementation, but all parsing methods accept spans:
- `bool ToBoolean(ReadOnlySpan<char> value)`
- `bool ToBoolean(ReadOnlySpan<char> value, string format)`
- `byte ToByte(ReadOnlySpan<char> value)` and other numeric overloads
- `char ToChar(ReadOnlySpan<char> value)`
- `Guid ToGuid(ReadOnlySpan<char> value)`
- `DateTime ToDateTime(ReadOnlySpan<char> value, string format = null)`
- `DateTimeOffset ToDateTimeOffset(ReadOnlySpan<char> value, string format = null)`
- `object FromSpan(Type destinationType, ReadOnlySpan<char> value)`

`FromSpan` may materialize a string to use `TypeDescriptor` converters when no span-native conversion exists.

## Parsing Semantics
- Use `Parse(ReadOnlySpan<char>, IFormatProvider)` overloads for numeric types to preserve culture behavior.
- For `DateTime`/`DateTimeOffset`, use `ParseExact` when a format is provided, else `Parse`.
- For `bool` with custom format `"true|false"`, compare spans using ordinal equality.
- For `char`, require a single-character span; otherwise throw `FormatException`.

## CsvDataReader Behavior
- Empty fields in schema mode still map to `null` (same as current behavior).
- `GetString` materializes a string from the span.
- Type-specific getters (e.g., `GetInt32`) operate on spans via the new converter.

## Compatibility Notes
- Any custom converter passed to `CsvSchemaBuilder.Add` must now accept `ReadOnlySpan<char>`.
- Existing string-based converters can adapt by calling `span.ToString()` internally.

## Tests
- Update schema-builder usages in tests to accept `ReadOnlySpan<char>` and, where needed, materialize to string explicitly.

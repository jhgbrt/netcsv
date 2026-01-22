# Span-Based CSV Reader Spec

## Goal
Eliminate per-field string allocations during parsing by scanning the input once, identifying fields as spans, and converting values directly from spans. Strings are only allocated when the target type is string (or when the caller explicitly requests string output).

## Compatibility Seams
- ReadCsv entrypoints (`ReadCsv.FromFile`, `ReadCsv.FromStream`, `ReadCsv.FromString`) remain the public API.
- IDataReader behavior remains compatible: same column naming, schema behavior, and exceptions.
- Converters are now span-based (breaking change): schema conversion delegates take `ReadOnlySpan<char>`.

## Non-Goals
- Do not change CSV format semantics (quotes, escapes, missing fields, empty line behavior).
- Do not change the public shape of CsvSchema or CsvLayout.
- Do not introduce a new mandatory dependency.

## High-Level Design
Replace the current per-field StringBuilder construction with a span-based scanner:
1. Buffer input into a reusable `char[]` or `ReadOnlyMemory<char>` window.
2. Parse characters into field slices (start index, length, and quoting metadata) without materializing strings.
3. Convert fields directly from spans into typed values using span-aware parsers.
4. Materialize strings only when requested (string columns or string data access).

The parser yields `CsvLineSlice` records where each field is represented as a slice into the buffer.

## Data Model
### Field Slice
A field is represented by:
- `int Start`
- `int Length`
- `bool WasQuoted`
- `bool ContainsEscapes`
- `bool ContainsDelimiterOrNewline` (optional; can be inferred)

This describes a span in the current buffer. The parser avoids copying unless a field spans buffers.

### Line Slice
A line is represented by:
- `FieldSlice[] Fields`
- `bool IsEmpty`
- `Location Location` (line/column tracking retained)

### Buffer Strategy
Two options, pick one:
- Sliding window buffer with compaction.
- Double-buffering and stitching for fields that cross buffer boundaries.

For cross-buffer fields, create a small temporary `ValueStringBuilder` or pooled `char[]` to assemble the field span once. This is still a single allocation per such field and only when required.

## State Machine
### States
- `BeginningOfLine`
- `InsideField`
- `OutsideField`
- `InsideQuotedField`
- `AfterSecondQuote`
- `Escaped`
- `InComment`
- `ParseError`

### Transitions (high level)
- `BeginningOfLine` -> `InsideField` on non-control characters.
- `BeginningOfLine` -> `InsideQuotedField` on quote.
- `InsideField` -> `OutsideField` on delimiter.
- `InsideField` -> `BeginningOfLine` on newline.
- `InsideQuotedField` -> `AfterSecondQuote` on quote.
- `AfterSecondQuote` -> `OutsideField` on delimiter or newline.
- `AfterSecondQuote` -> `InsideQuotedField` on non-delimiter non-newline, depending on behavior.

This mirrors current logic but builds field slices rather than strings.

### Quotes Inside Quoted Fields
Quoted-field handling has two distinct paths: escaped quotes and ambiguous quotes.

#### Escaped Quotes
When the configured escape character is encountered inside a quoted field:
- Treat the next character as literal, regardless of whether it is a quote, delimiter, or newline.
- Mark `ContainsEscapes = true` for the field.
- Continue in `InsideQuotedField` after consuming the escaped character.

This covers both backslash-style escapes and the CSV convention where `escape == quote` (double-quote escaping). In the double-quote case, the first quote is the escape marker and the second quote is the literal quote added to the field.

#### Ambiguous Quotes (Potential End of Field)
When a quote is encountered inside a quoted field and it is not an escape:
- Transition to `AfterSecondQuote` and tentatively treat the quote as the end of the field.
- While in `AfterSecondQuote`, skip whitespace as allowed by trimming rules.

Then handle the next non-whitespace character:
- **Delimiter or newline**: end the field. The quote is not part of the field.
- **Quote**: treat the prior quote as a literal quote, append it to the field, and return to `InsideQuotedField`.
- **Other character**: apply `QuotesInsideQuotedFieldAction`:
  - `Ignore`: treat the prior quote as literal, append the current character, and continue in `InsideQuotedField`.
  - `AdvanceToNextLine`: skip the remainder of the line (ParseError behavior).
  - `Throw`: raise `MalformedCsvException`.

#### Span Conversion Note
If a field has `ContainsEscapes = true`, unescape into a temporary buffer before conversion. This keeps parsing allocation-free for the common case where no escapes are present.

## Parsing Algorithm (Per Line)
- Track field start index and length in the buffer.
- Append a FieldSlice when encountering delimiter or newline.
- Mark `WasQuoted` and `ContainsEscapes` during scanning.
- Apply trimming in a slice-aware manner: adjust `Start`/`Length` without copying.
- Missing fields: pad with empty or null slices without using LINQ.

## Conversion Strategy
### Span Converters
Use the single span-based converter implementation:
- `Converter` methods accept `ReadOnlySpan<char>` for primitives, `DateTime`, `DateTimeOffset`, `Guid`, etc.
- `CsvSchemaBuilder` now builds `CsvColumn` entries with span-based delegates.
- Custom schema converters must accept `ReadOnlySpan<char>` and may materialize via `span.ToString()` if needed.

### String Columns
If the target type is string, allocate only at conversion time:
- `new string(span)` for unescaped content.
- For escaped or quoted content, unescape into a pooled buffer, then materialize.

### Existing Converter
There is only one converter implementation, and it is span-based. Fallback for custom types uses `TypeDescriptor` by materializing the span to string.

## IDataReader Implementation
Replace current `CsvLine` with `CsvLineSlice` in the reader.
- `GetValue(int i)` will:
  - Resolve the slice for column `i`.
  - Convert from span using schema converters.
  - Allocate only if the target type is string or if conversion requires a string fallback.
- `GetString(int i)` uses span-to-string directly.
- `GetOrdinal` and headers remain unchanged.

## Header Handling
- Header line is parsed as slices.
- Header strings are materialized once when building `CsvHeader` or `CsvSchema`.

## Missing Field Handling
Maintain behavior:
- ParseError => throw if not empty.
- ReplaceByEmpty => slice to empty span.
- ReplaceByNull => special null marker.

## Error Reporting
Keep `RawData` sampling for exceptions. Raw data capture can be retained as a fixed ring buffer of chars (already implemented). `Location` remains line/column based.

## Allocation Strategy
- One reusable buffer for reading.
- Minimal per-line allocations: field slice array (can be pooled and resized).
- String allocation only when converting to string or when span-based parsing is not available.

## Performance Expectations
- Reduce Gen0 allocations by avoiding per-field strings.
- Lower CPU by removing StringBuilder work and redundant string conversions.
- Improvement is largest for typed reads where strings are not needed.

## Migration Plan
1. Introduce span-based parser behind an internal interface; keep current parser as fallback.
2. Implement span-based converter set; use fallback to existing converters.
3. Switch IDataReader to use slices while keeping public API unchanged.
4. Benchmark with existing BenchmarkDotNet suite.

## Implementation Strategy (V1/V2 Switch)
### Goals
- Easy, programmatic selection between V1 and V2 parser implementations.
- Test suite remains unchanged; can be executed against either parser (or both) without duplicating tests.

### Internal Parser Selector
Introduce an internal selector that chooses the parser implementation:
- `CsvParserKind` enum: `V1`, `V2`.
- `CsvParserFactory.Create(TextReader, BufferedCharReader, CsvLayout, CsvBehaviour, CsvParserKind kind)` returns `ICsvParser`.
- Default `kind` is `V1` for stability until V2 is complete.

### Public/Config Hook (Non-breaking)
Expose selection via behavior/config only (no public API change required):
- Add `CsvBehaviour.ParserKind` (internal or public) with default `V1`.
- Alternatively, add an internal static switch (e.g., `CsvParserSwitch.CurrentKind`) for testing and benchmarks.
- `CsvDataReader.InitializeResultSet()` uses the factory + selector.

### Test Strategy (Unchanged Tests)
Keep all tests intact and run them against each parser:
- Add a test assembly fixture or collection fixture that sets the parser selector before tests run.
- Provide two test runs:
  - Default (V1): no changes.
  - V2: set selector once at assembly startup (e.g., module initializer or xUnit collection fixture).
- No test code should need to know about V1/V2; tests stay identical.

### Comparison Workflow
- In CI or local runs, execute tests twice (once per parser) using the selector.
- For quick local toggling: set an environment variable (e.g., `NETCSV_PARSER=V2`) read by the selector.

## Risks and Mitigations
- Buffer boundary handling is complex: cover with unit tests for long fields and quoted fields across buffer edges.
- Span parsers require careful culture handling: use `IFormatProvider` in `TryParse` overloads.
- Escaping logic must match current behavior: add regression tests for quotes and escape sequences.

## Test Coverage Suggestions
- Long quoted field spanning buffers.
- Mixed quoted/unquoted fields with trimming on/off.
- Missing field scenarios for each action.
- Schema conversions for numeric, DateTime, and bool formats.
- Header handling with empty columns.

# Performance Investigation

This document lists performance bottlenecks and potential improvements found while reviewing the current implementation. References use `path:line` notation.

## Reader Pipeline
- **Quadratic work while keeping the raw error buffer** – `CsvLineBuilder.ReadNext` trims `_rawData` by calling `StringBuilder.Remove(0, 1)` for every character once the buffer exceeds 32 chars (`Net.Code.Csv/Impl/CsvLineBuilder.cs:119-130`). Each `Remove` shifts the remaining characters and turns the hot path into O(n²) for large inputs. Switching to a ring buffer (e.g., fixed-size `char[]` with a rotating index) or `Queue<char>` backed by `ArrayPool<char>` would cap the cost to O(1) per character.
- **Char-by-char `TextReader` calls with small I/O buffers** – The parser fetches a single char via `TextReader.Read()` and then immediately calls `Peek()` for lookahead (`Net.Code.Csv/Impl/CsvLineBuilder.cs:119-127`). Combined with the default `StreamReader` constructors that allocate only 1 KB buffers (`Net.Code.Csv/ReadCsv.cs:41` and `:78`), this results in two virtual calls per character and frequent kernel reads on large files. Consider:
  - Passing a larger buffer size (e.g., 16–32 KB) and enabling `FileOptions.SequentialScan` when opening files.
  - Replacing the per-character loop with block reads (`Read(Span<char>)`) and hand-rolled indexing to eliminate `Peek()` altogether.
- **Missing-field padding allocates multiple enumerators per line** – When a line has fewer fields than expected, `CsvLineBuilder.ToLine` pads via `_fields.Concat(Enumerable.Repeat(...))` before copying into a new array (`Net.Code.Csv/Impl/CsvLineBuilder.cs:98-111`). This creates temporary iterators and a second pass through the data for every short line. A cheaper approach is to resize the backing list in place and fill the extra slots with pooled strings (empty or `null`) before creating the `CsvLine`.

## Writer Pipeline
- **Redundant allocations and LINQ joins in `WriteCsv.ToWriter`** – The synchronous writer builds `properties` with a deferred LINQ `Join` (`Net.Code.Csv/WriteCsv.cs:128-135`), causing the join lookup to be rebuilt for every row. Materializing this mapping once into an array avoids repeated hash table construction. Inside the write loop, each field string is produced inside `GetPropertyValuesAsString`, yet the outer loop ignores the yielded `v` and calls `sb.ToString()` again (`Net.Code.Csv/WriteCsv.cs:141-149`), effectively doubling string allocations per column. Writing `v` (or using `ValueStringBuilder`/`Span<char>` to stream directly to the `TextWriter`) would remove this overhead.

## Schema Binding & Conversion
- **Per-property ordinal lookups dominate row hydration** – During `AsEnumerable`, the generated activator executes `record.GetOrdinal(...)` for every property (`Net.Code.Csv/Extensions.cs:36-45`). `CsvDataReader.GetOrdinal` either scans the schema (`Net.Code.Csv/Impl/CsvSchemaBuilder.cs:11-22`) or the header list (`Net.Code.Csv/Impl/CsvHeader.cs:15-27`), so every property access becomes O(n). Compute ordinals once (e.g., build `(PropertyInfo property, int ordinal)` tuples) and have the activator read fields by index to linearize row materialization.
- **Fallback conversion relies on `TypeDescriptor` per value** – When schema columns target arbitrary types, `CsvSchemaBuilder` stores converters that call `_converter.FromString(type, s)` (`Net.Code.Csv/CsvSchemaBuilder.cs:69-72`), which in turn invokes `TypeDescriptor.GetConverter(destinationType)` on every field (`Net.Code.Csv/Impl/Converter.cs:60`). `TypeDescriptor` lookups involve global locks and cache misses; caching the `TypeConverter` per column (or compiling delegates once when the schema is built) would significantly reduce CPU time when custom types are frequent.

## IDataReader Surface
- **`GetBytes`/`GetChars` are non-streaming** – Both methods materialize the entire base64 or string payload before copying the requested slice (`Net.Code.Csv/Impl/CsvDataReader.cs:102-133`). Consumers that read large blobs in chunks incur repeated full decoding and temporary arrays. Holding onto the raw string and slicing via `Span<byte>`/`Span<char>` or caching the decoded buffer until the next row would make chunked reads effectively O(length requested).

## Suggested Next Steps
1. Capture a baseline using the new `CsvReaderBenchmark` scenarios (covers IDataReader, typed schema reads, and header-less parsing for 1k/100k rows; `CsvTest/Program.cs:1-191`) before touching the reader hot paths.
2. Prototype a buffered reader that processes `Span<char>` blocks and compare it to the current `Read()/Peek()` loop on multi-GB inputs.
3. Refactor the writer pipeline to cache column mappings and eliminate duplicate string allocations; validate via BenchmarkDotNet.
4. After baselines exist, experiment with pooled buffers (`ArrayPool<char/string>` or `ValueStringBuilder`) in both reader and writer paths, ensuring GC pressure stays flat via `dotnet-counters`.

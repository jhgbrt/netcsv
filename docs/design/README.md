# Design notes (historical)

These documents were written while the library was being reworked. They describe what was
planned at the time. They are kept for background and are not maintained as the code changes.

Things that have changed since they were written:

- There is a single parser. The documents that mention "V1" and "V2", or a way to switch
  between them (`CsvParserKind`, `NETCSV_PARSER`), are out of date: V1 was removed, and
  `CsvParserKind`/`CsvBehaviour.Parser` are obsolete and have no effect.
- Line numbers in `path:line` references no longer match the source.

| Document | Topic |
|---|---|
| [SPAN_STATE_MACHINE_SPEC.md](SPAN_STATE_MACHINE_SPEC.md) | Span-based streaming parser (what is now the only parser) |
| [SPAN_CONVERTER_SPEC.md](SPAN_CONVERTER_SPEC.md) | Converting fields from `ReadOnlySpan<char>` instead of strings |
| [TYPED_READER_SPEC.md](TYPED_READER_SPEC.md) | Typed reading (`FromFile<T>` and friends) without going through `IDataReader` |
| [CSV_BEHAVIOUR_MODES_SPEC.md](CSV_BEHAVIOUR_MODES_SPEC.md) | `CsvBehaviour.Strict()` / `Forgiving()` presets and disabling quotes |
| [PERFORMANCE_IMPROVEMENTS.md](PERFORMANCE_IMPROVEMENTS.md) | Earlier performance investigation and results |

For how to work on the code today, see [AGENTS.md](../../AGENTS.md).

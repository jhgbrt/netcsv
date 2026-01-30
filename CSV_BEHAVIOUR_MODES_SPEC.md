# CSV Behaviour Modes Spec

## Summary
Add two user-facing presets to make CSV parsing behavior predictable and easy to choose,
scoped to the V2 parser only:

- `CsvBehaviour.Strict()` for standards-style, fail-fast parsing.
- `CsvBehaviour.Literal()` for "raw field extraction" that can preserve every character between delimiters.

This spec also introduces a way to **disable quoting** so users can parse literally when required.

## Goals
- Provide **simple presets** that map to clear, documented behaviors.
- Allow **literal extraction** of field text (including whitespace and quotes) with a supported configuration.
- Make **quoting optional**, so a user can treat quote characters as normal content.
- Keep existing defaults unchanged unless a preset is explicitly used.
- Allow advanced users to override trimming and other flags after using a preset.

## Non-Goals
- No changes to the V1 parser.
- No change to default parser selection (`V1`/`V2`) or performance behavior.
- No change to schema conversion rules.
- No requirement to match RFC 4180 in all cases (strict is "strict within our rules").

## New API Surface
### Presets
```csharp
public static CsvBehaviour Strict();
public static CsvBehaviour Literal();
```

These return a `CsvBehaviour` instance with pre-selected flags. Users can still override
individual properties after calling the preset.

### Quoting Disablement
Quoting is disabled by setting the quote character to `null`:
```csharp
internal record CsvLayout(char? Quote = '"', ...);
```
If `Quote == null`, quoting is disabled (quote treated as normal content).

## Behaviour Definitions
### Common Rules (all modes)
- Delimiter always splits fields.
- Line endings terminate a record.
- Empty line / missing field behavior remains governed by `EmptyLineAction` and `MissingFieldAction`.
- Trimming is applied **after** field extraction and does not change parsing state.

### Strict Preset
Intent: "Parse valid CSV deterministically; fail on malformed quoting."

Preset values:
- `QuotesInsideQuotedFieldAction = ThrowException`
- `ValueTrimmingOptions = None` (user may override)
- Quoting **enabled** by default
- No special recovery (no "best effort" behavior)

Strict parsing rules (when quoting enabled):
- A quote starts a quoted field **only if it is the first character of a field**.
- A closing quote **must** be followed by delimiter, newline, or EOF.
- Any other character after a closing quote is a **parse error**.
- Unescaped quotes inside quoted fields are **errors**.
- When `ValueTrimmingOptions = All`, any whitespace outside quotes (before an opening quote or after a closing quote) is a **parse error**.

Notes:
- If a user wants whitespace after a closing quote to be preserved, they should use
  `Literal()` with quoting disabled, or a future "liberal" mode.

### Literal Preset
Intent: "Extract exactly what is between delimiters; allow full fidelity."

Preset values:
- `ValueTrimmingOptions = None`
- `QuotesInsideQuotedFieldAction = Ignore` (irrelevant if quoting disabled)
- Quoting **disabled** by default when `Quote` is set to `null` on the layout

Literal parsing rules:
- Quote characters are **never** treated as special; they are part of field content.
- Escape sequences are **not** interpreted (escape char treated as normal content).
- Result field text is exactly the characters between delimiters (unless trimming is enabled).

### Optional Overrides (advanced users)
Users may still override:
- `ValueTrimmingOptions` (e.g., `All`, `QuotedOnly`, `UnquotedOnly`)
- `QuotesInsideQuotedFieldAction` (where quoting is enabled)
- `EmptyLineAction`, `MissingFieldAction`, `Comment`, `Delimiter`, etc.

## Examples
Given input (delimiter `,`):

1) `a,"b",c`
- Strict (default trimming): fields = `["a", "b", "c"]`
- Literal (quoting disabled): fields = `["a", "\"b\"", "c"]`

2) ` "a" `
- Strict + `ValueTrimmingOptions.All`: parse error (whitespace before quote)
- Literal: fields = `[" \"a\" "]`
- Literal + `ValueTrimmingOptions.All`: fields = `["\"a\""]`

3) `"a"  ,b`
- Strict + `ValueTrimmingOptions.All`: parse error (whitespace after closing quote)
- Literal: fields = `["\"a\"  ", "b"]`

4) `"a"b"`
- Strict: parse error (unescaped quote inside quoted field)
- Literal: fields = `["\"a\"b\""]`

## Migration / Compatibility
- Default behavior remains unchanged.
- Presets are opt-in.
- Adding quoting disablement should not change any existing behavior unless a user sets it.

## Open Questions
- Should strict allow leading whitespace before quotes when trimming is not `All`?

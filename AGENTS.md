# Repository Guidelines

## Project Structure & Module Organization
- `Net.Code.Csv/`: Core library (targets `netstandard2.1`); public entry points in `ReadCsv.cs` and `WriteCsv.cs`; internal engines under `Impl/` (parser, state machine, layout).
- `Net.Code.Csv.Tests.Unit/`: xUnit tests targeting `net9.0`; sample inputs under `SampleFiles/`.
- `CsvTest/`: Small console app/benchmarks using the library; run locally for manual checks and benchmarks.
- `.github/workflows/`: CI for build, tests, and publishing.

## Build, Test, and Development Commands
- Prerequisite: .NET SDK 9.0 or later (`dotnet --version`).
- Restore: `dotnet restore Net.Code.Csv.sln`.
- Build: `dotnet build Net.Code.Csv.sln -c Release`.
- Test:
  Always run all tests with both parser versions by setting the environment variable.
  In PowerShell: 
  `$env:NETCSV_PARSER='V1'; dotnet test -c Release; $env:NETCSV_PARSER='V2'; dotnet test -c Release;` 
- Sample run: `dotnet run --project CsvTest -c Release` (uses `CsvTest/test.csv`).
- Pack (NuGet): `dotnet pack Net.Code.Csv -c Release -o ./artifacts`.

## Coding Style & Naming Conventions
- Language: C#; indent with 4 spaces; file‑scoped namespaces.
- Naming: PascalCase for types/methods; camelCase for locals/params; `_camelCase` for private fields.
- Brace/formatting: follow existing files. Use `dotnet format` if available to align with standard C# style.
- Public API changes: prefer minimal surface area; keep internals under `Impl/`.

## Testing Guidelines
- Framework: xUnit (`[Fact]`, `[Theory]`).
- Location: add tests in `Net.Code.Csv.Tests.Unit/*` mirroring feature area (e.g., `Csv/WriteCsvTests.cs`).
- Naming: method names in Given_When_Then or clear underscore style, e.g., `FromString_WhenInputNull_Throws`.
- Run: `dotnet test -c Release`. Aim to cover parsing, layout, and edge cases (quotes, headers, encodings).

## Performance Workflow
- Reader/writer performance work must follow a tight loop: **measure → change one thing → measure again**.
- Capture a BenchmarkDotNet baseline by running `dotnet run --project CsvTest -c Release` (records are emitted by `CsvReaderBenchmark`); save the relevant table for reference.
- Implement exactly one performance-focused change in the hot path, keeping unrelated refactors for later.
- Re-run the benchmark command, compare against the saved baseline, and summarize the delta (positive or negative) in your PR description.
- Focus improving performance efforts for the V2 parser

## Commit & Pull Request Guidelines
- Commits: imperative mood (“Add CsvStateMachine test”); keep focused; reference issues (`#123`) when applicable.
- PRs: clear description, rationale, and scope; link issues; include tests for behavior changes; update README if user-facing.
- CI: ensure all workflows pass; no failing or skipped tests without justification.

## Architecture Notes
- Entry points: `ReadCsv`/`WriteCsv` create `CsvLayout` + `CsvBehaviour` and drive a streaming state machine.
- Internals: `Impl/` contains parser, header detection, and conversion helpers; prefer extending via behavior/layout rather than ad‑hoc parsing.

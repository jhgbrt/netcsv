# GitHub Copilot Instructions for Net.Code.Csv

This document provides guidance for using GitHub Copilot effectively when contributing to the Net.Code.Csv library, a high-performance CSV parsing and writing library for .NET.

## Project Overview

Net.Code.Csv is a from-the-ground-up rewrite of the LumenWorks.Framework.IO parser, designed for:
- Robust CSV parsing with flexible configuration options
- DataReader-style interface for memory-efficient processing
- Comprehensive type conversion support
- Handling of malformed CSV files and edge cases

## Repository Structure

```
Net.Code.Csv/                 # Main library (.NET Standard 2.1)
├── ReadCsv.cs                # Static factory methods for reading CSV
├── WriteCsv.cs               # Static factory methods for writing CSV
├── Impl/                     # Core implementation
│   ├── CsvDataReader.cs      # Main DataReader implementation
│   ├── CsvParser.cs          # Core parsing logic
│   └── CsvStateMachine.cs    # State machine for parsing
└── Extensions.cs             # Extension methods

Net.Code.Csv.Tests.Unit/      # Unit tests
└── Csv/                      # Test files for CSV functionality

CsvTest/                      # Console application for testing
```

## Key Design Patterns

### 1. Factory Pattern
The library uses static factory methods for creating readers and writers:

```csharp
// Reading CSV files
using var reader = ReadCsv.FromFile("data.csv");
using var reader = ReadCsv.FromStream(stream);
using var reader = ReadCsv.FromString(csvContent);

// Writing CSV files
WriteCsv.ToFile(items, "output.csv");
WriteCsv.ToStream(items, stream);
```

### 2. DataReader Pattern
The CSV reader implements `IDataReader` for consistent data access:

```csharp
using var reader = ReadCsv.FromFile("data.csv", hasHeaders: true);
while (reader.Read())
{
    var name = reader["Name"];
    var age = reader.GetInt32("Age");
    var date = reader.GetDateTime("Date");
}
```

### 3. Configuration Objects
The library uses structured configuration through layout and behavior objects:

```csharp
var layout = new CsvLayout(
    quote: '"',
    delimiter: ',', 
    escape: '"',
    hasHeaders: true
);

var behavior = new CsvBehaviour(
    trimmingOptions: ValueTrimmingOptions.All,
    missingFieldAction: MissingFieldAction.ReplaceByEmpty
);
```

## Copilot Usage Patterns

### Working with CSV Parsing

When asking Copilot to help with CSV parsing tasks, use these patterns:

**✅ Good prompts:**
- "Create a CSV reader that handles quoted fields with embedded commas"
- "Parse CSV with custom delimiter and trim whitespace from unquoted fields only"
- "Handle missing fields by replacing with empty strings instead of throwing errors"
- "Convert CSV string values to strongly typed objects using the DataReader pattern"

**❌ Avoid:**
- Generic CSV parsing requests without specifying the DataReader pattern
- Requests that don't consider the extensive configuration options available

### Type Conversion and Schema Handling

The library provides flexible type conversion through schemas:

```csharp
// Prompt: "Create a CSV schema for parsing employee data with type conversion"
var schema = new CsvSchemaBuilder()
    .Add("Name", typeof(string))
    .Add("Age", typeof(int))
    .Add("Salary", typeof(decimal))
    .Add("HireDate", typeof(DateTime))
    .Build();

using var reader = ReadCsv.FromFile("employees.csv", schema: schema);
```

### Error Handling Patterns

**Copilot prompt examples:**
- "Handle malformed CSV with quotes inside quoted fields using AdvanceToNextLine action"
- "Configure CSV parser to skip empty lines and continue processing"
- "Create robust CSV processing that logs parsing errors but continues"

### Common CSV Scenarios

#### 1. Reading Large Files Efficiently
```csharp
// Prompt: "Create memory-efficient CSV processing for large files"
using var reader = ReadCsv.FromFile("large-file.csv", hasHeaders: true);
var processedCount = 0;
while (reader.Read())
{
    // Process one record at a time without loading entire file
    ProcessRecord(reader);
    if (++processedCount % 10000 == 0)
        Console.WriteLine($"Processed {processedCount} records");
}
```

#### 2. Handling Different CSV Formats
```csharp
// Prompt: "Parse European CSV format with semicolon delimiter and different quote handling"
using var reader = ReadCsv.FromFile("european-data.csv",
    delimiter: ';',
    quote: '"',
    hasHeaders: true,
    trimmingOptions: ValueTrimmingOptions.All);
```

#### 3. Dynamic CSV Processing
```csharp
// Prompt: "Process CSV files with unknown structure at runtime"
using var reader = ReadCsv.FromFile("unknown-structure.csv", hasHeaders: true);
var columns = new string[reader.FieldCount];
for (int i = 0; i < reader.FieldCount; i++)
{
    columns[i] = reader.GetName(i);
}
```

## Testing Patterns

### Unit Test Structure
When creating tests, follow the existing patterns:

```csharp
[Test]
public void Should_Parse_Csv_With_Quoted_Fields()
{
    // Arrange
    var csv = "\"Name\",\"Age\"\n\"John, Jr.\",25";
    
    // Act
    using var reader = ReadCsv.FromString(csv, hasHeaders: true);
    reader.Read();
    
    // Assert
    Assert.That(reader["Name"], Is.EqualTo("John, Jr."));
    Assert.That(reader.GetInt32("Age"), Is.EqualTo(25));
}
```

### Test Data Patterns
- Use inline CSV strings for simple tests
- Use sample files from `SampleFiles/` directory for complex scenarios
- Test edge cases like empty fields, malformed quotes, different encodings

**Copilot prompt examples for testing:**
- "Create unit test for CSV parsing with embedded newlines in quoted fields"
- "Test CSV writer with special characters and different culture settings"
- "Generate test data for stress testing large CSV files"

## Performance Considerations

### Memory Efficiency
- Use `IDataReader` pattern for streaming large files
- Avoid `ToList()` or similar operations that load entire dataset
- Configure appropriate buffer sizes for stream operations

### Parsing Performance
- Use appropriate `ValueTrimmingOptions` (only trim when necessary)
- Configure `EmptyLineAction.Skip` for files with empty lines
- Use schema when possible to avoid runtime type inference

**Copilot prompts for performance:**
- "Optimize CSV parsing for minimal memory allocation"
- "Create high-performance CSV processing pipeline for batch operations"
- "Profile CSV parsing performance and identify bottlenecks"

## Common Issues and Solutions

### 1. Encoding Issues
```csharp
// Prompt: "Handle CSV files with different text encodings"
using var reader = ReadCsv.FromFile("data.csv", 
    encoding: Encoding.GetEncoding("ISO-8859-1"));
```

### 2. Malformed CSV Handling
```csharp
// Prompt: "Configure CSV parser to handle malformed quotes gracefully"
using var reader = ReadCsv.FromFile("malformed.csv",
    quotesInsideQuotedFieldAction: QuotesInsideQuotedFieldAction.AdvanceToNextLine);
```

### 3. Culture-Specific Data
```csharp
// Prompt: "Parse CSV with European number formats and date formats"
using var reader = ReadCsv.FromFile("european-data.csv",
    cultureInfo: new CultureInfo("de-DE"));
```

## Integration with GitHub Copilot

### Recommended Copilot Settings
For optimal results when working with this codebase:

1. **Context Awareness**: Include relevant files when asking for suggestions:
   - `ReadCsv.cs` for reading operations
   - `WriteCsv.cs` for writing operations
   - Relevant test files for testing patterns

2. **Specific Prompts**: Be specific about CSV parsing requirements:
   - Mention delimiter, quote character, header presence
   - Specify error handling preferences
   - Include performance requirements

3. **Pattern Matching**: Reference existing patterns in the codebase:
   - Use factory method patterns from `ReadCsv`/`WriteCsv`
   - Follow configuration patterns from existing code
   - Maintain consistency with error handling approaches

### Example Copilot Chat Interactions

**Question**: "How do I parse a CSV file with custom delimiters and handle missing fields?"

**Better prompt**: "Using Net.Code.Csv library, create a CSV reader for pipe-delimited files with headers, where missing fields should be replaced with empty strings instead of throwing errors. Follow the factory pattern from ReadCsv.FromFile."

**Question**: "Create a CSV writer for my data class"

**Better prompt**: "Using WriteCsv.ToFile, create a CSV writer for Employee objects with headers, pipe delimiter, and German culture for number formatting. The Employee class has Name, Age, Salary, and HireDate properties."

## Additional Resources

- [Main Documentation](README.md) - Basic usage examples
- [Wiki](https://github.com/jhgbrt/netcsv/wiki) - Detailed documentation
- [Unit Tests](Net.Code.Csv.Tests.Unit/) - Comprehensive test examples
- [Sample Files](Net.Code.Csv.Tests.Unit/SampleFiles/) - Test data examples

## Contributing Guidelines

When contributing to this repository:

1. **Follow Existing Patterns**: Use the established factory methods and configuration patterns
2. **Add Tests**: Include comprehensive unit tests for new functionality
3. **Performance Awareness**: Consider memory and performance implications
4. **Documentation**: Update relevant documentation for new features
5. **Backwards Compatibility**: Maintain API compatibility when possible

Use GitHub Copilot to help generate tests, documentation, and implementation code, but always review and validate the suggestions against the existing codebase patterns and requirements.
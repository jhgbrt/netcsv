[![CI](https://github.com/jhgbrt/netcsv/actions/workflows/ci.yml/badge.svg)](https://github.com/jhgbrt/netcsv/actions/workflows/ci.yml)
[![Downloads](https://img.shields.io/nuget/dt/Net.Code.Csv.svg)](https://www.nuget.org/packages/Net.Code.Csv)

This CSV parser is a from-the-ground-up rewrite of the 
LumenWorks.Framework.IO parser. There were a few itches:

- First of all, I had a buggy CSV with double quotes inside
  double quoted fields. As I had no control over the input and
  it was virtually impossible to sanitize, I tried to modify the
  LumenWorks source code to fix this. However, I ended up rewriting
  the whole thing. The unit tests are actually the only code that
  was (almost) not touched :-)
- I wanted to add a more flexible way to convert string values to
  actual types. 
    
How to use?
===========

Install the [Net.Code.Csv](https://www.nuget.org/packages/Net.Code.Csv) package and add:

```csharp
using Net.Code.Csv;
```

Reading with a DataReader
-------------------------

The static methods on `ReadCsv` return an `IDataReader`:

```csharp
using var reader = ReadCsv.FromFile("path/to/file.csv", hasHeaders: true);
while (reader.Read())
{
    var name = reader.GetString(reader.GetOrdinal("Name"));
    var birthDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("BirthDate")));
}
```

You can also read from a stream (`ReadCsv.FromStream`) or a string (`ReadCsv.FromString`).

Reading typed records
---------------------

The generic overloads map each line to an instance of a type, using the property names
(or constructor parameter names) to find the matching columns:

```csharp
record Person(string Name, [CsvFormat("yyyyMMdd")] DateTime BirthDate, [CsvFormat("yes|no")] bool IsActive);

foreach (var person in ReadCsv.FromFile<Person>("path/to/file.csv", hasHeaders: true))
{
    Console.WriteLine(person);
}
```

`[CsvFormat]` is optional. It sets the format for dates, times and booleans
(for booleans, `"true|false"` form, e.g. `"yes|no"` or `"Y|N"`).

Writing
-------

```csharp
WriteCsv.ToFile(people, "path/to/output.csv", hasHeaders: true);
var text = WriteCsv.ToString(people, hasHeaders: true);
```

Options
-------

The examples above assume common defaults for the CSV layout and behaviour; all of them
can be changed through optional parameters. Layout parameters include the encoding
(detected automatically when not given), the quote, delimiter, escape and comment characters,
and whether the file has a header line. Behaviour parameters control trimming, missing
fields, empty lines and quotes inside quoted fields. `CsvBehaviour.Strict()` and
`CsvBehaviour.Forgiving()` are ready-made presets.

For more documentation, check the [wiki](https://github.com/jhgbrt/netcsv/wiki).
Background on the design is in [docs/design](docs/design/README.md).

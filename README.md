[![Build status](https://ci.appveyor.com/api/projects/status/7ne2rqat9o6g136s?svg=true)](https://ci.appveyor.com/project/jhgbrt/netcsv)

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

There are a few static methods available in the `ReadCsv` class to
easily read a CSV file using the DataReader style:

You need to add this using statement:

    using Net.Code.Csv;

Now you can use code like this:

    var myFileName = "path/to/csvfile";
    
    using (var reader = ReadCsv.FromFile(myFileName)) {
        while (reader.Read()) {
            var record = new { 
                Name = reader["Name"];
                BirthDate = DateTime.Parse(reader["BirthDate"]);
            }
        }
    }

If you have a string that actually contains the CSV content already, use this:

    void ParseCsv(string content) {
        using (var reader = ReadCsv.FromString(content)) {
        // ...
        }
    }
    
 Finally, you can also use the ReadCsv.FromStream() method.
 
 The examples assume some common defaults about the actual CSV layout
 and behaviour. You can of course change those through parameters.
 
 Parameters that specify the CSV format include the encoding, the quote character, the field delimiter, a.o.
 Other parameters specify the 'behaviour' of the CSV reader: what to do with empty lines or missing fields, 
 whether fields should be trimmed or not, etc.
 

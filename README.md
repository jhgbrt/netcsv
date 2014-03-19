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

There are a few extension methods on string and stream available to
easily read a CSV file using the DataReader style:

You need to add this using statement:

    using Net.Code.Csv;

Now you can use code like this:

    var myFileName = "path/to/csvfile";
    myFileName.ReadFileAsCsv(Encoding.Default);

If you have a string that actually contains the CSV content already, use this:

    void ParseCsv(string content) {
        content.ReadStringAsCsv();
    }
    
 Finally, you can also use the ReadStreamAsCsv() extension method.
 
 The examples assume some common defaults about the actual CSV layout
 and behaviour, but you can of course change those by using the overloads
 that accept an instance of the CsvBehaviour and CsvLayout classes.
 

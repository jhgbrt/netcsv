using Net.Code.Csv;
using System;
using System.Data;
using System.Linq;
using System.Text;

using var reader = ReadCsv.FromFile(
    "test.csv",
    encoding: Encoding.UTF8,
    delimiter: ';',
    hasHeaders: true
);

var table = reader.GetSchemaTable();

var headerNames = (
    from row in table.Rows.OfType<DataRow>()
    select (string)row["ColumnName"]
).ToList();

while (reader.Read())
{
    var item = string.Join(", ", headerNames.Select(name => $"{name}: {reader[name]}"));

    Console.WriteLine(item);
}

using Net.Code.Csv;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace CsvTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ReadFileAsCsvTest();
        }
        static void ReadFileAsCsvTest()
        {
            var reader = ReadCsv.FromFile("test.csv", Encoding.UTF8, new CsvLayout(delimiter: ';', hasHeaders: true));
            using (reader)
            {
                var table = reader.GetSchemaTable();

                var headerNames = (
                    from row in reader.GetSchemaTable().Rows.OfType<DataRow>()
                    select (string)row["ColumnName"]
                ).ToList();

                while (reader.Read())
                {
                    var values = (
                        from header in headerNames
                        select new { key = header.Trim('"'), value = (string)reader[header] }
                    );

                    foreach (var v in values) { Console.WriteLine(v); }
                }
            }

        }
    }
}

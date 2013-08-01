using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Code.Csv.Impl;

namespace Net.Code.Csv.Tests.Unit.IO.Csv
{
    class CsvParserTest
    {
        public void Test()
        {
            var data = CsvReaderSampleData.SampleData1;
            var reader = new StringReader(data);
            var parser = new CsvParser(reader, 4096, new CsvLayout(), new CsvBehaviour());
            foreach (var line in parser)
            {
                
            }
        }
    }
}

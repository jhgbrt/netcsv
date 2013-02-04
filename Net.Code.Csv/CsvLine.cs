using System.Collections.Generic;
using System.Linq;

namespace Net.Code.Csv
{
    /// <summary>
    /// A CSV line
    /// </summary>
    public class CsvLine
    {
        private readonly bool _isEmpty;
        private readonly string[] _fields;

        public CsvLine(IEnumerable<string> fields, bool isEmpty)
        {
            _isEmpty = isEmpty;
            _fields = fields.ToArray();
        }

        public bool IsEmpty
        {
            get { return _isEmpty; }
        }

        public string[] Fields
        {
            get
            {
                return  _fields;
            }
        }

        public static readonly CsvLine Empty = new CsvLine(Enumerable.Empty<string>(), true);
    }
}
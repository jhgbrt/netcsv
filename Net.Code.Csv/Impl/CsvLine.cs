using System.Collections.Generic;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    /// <summary>
    /// A CSV line
    /// </summary>
    class CsvLine
    {
        private readonly bool _isEmpty;
        private readonly string[] _fields;

        /// <summary>
        /// Constructs a line from a collection of fields
        /// </summary>
        /// <param name="fields">The fields of the line</param>
        /// <param name="isEmpty">indicates whether this is an empty line</param>
        public CsvLine(IEnumerable<string> fields, bool isEmpty)
        {
            _isEmpty = isEmpty;
            _fields = fields.ToArray();
        }

        /// <summary>
        /// Is this an empty line?
        /// </summary>
        public bool IsEmpty
        {
            get { return _isEmpty; }
        }

        /// <summary>
        /// The fields for a line
        /// </summary>
        public string[] Fields
        {
            get
            {
                return  _fields;
            }
        }

        /// <summary>
        /// An empty CSV line
        /// </summary>
        public static readonly CsvLine Empty = new CsvLine(Enumerable.Empty<string>(), true);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    /// <summary>
    /// A CSV line
    /// </summary>
    class CsvLine
    {
        /// <summary>
        /// Constructs a line from a collection of fields
        /// </summary>
        /// <param name="fields">The fields of the line</param>
        /// <param name="isEmpty">indicates whether this is an empty line</param>
        public CsvLine(IEnumerable<string> fields, bool isEmpty)
        {
            IsEmpty = isEmpty;
            Fields = fields.ToArray();
        }

        /// <summary>
        /// Is this an empty line?
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// The fields for a line
        /// </summary>
        public string[] Fields { get; }

        /// <summary>
        /// An empty CSV line
        /// </summary>
        public static readonly CsvLine Empty = new CsvLine(Enumerable.Empty<string>(), true);

        public override string ToString() => string.Join(";", Fields);

        public string this[int field]
        {
            get
            {
                if (field < Fields.Length)
                    return Fields[field];
                if (IsEmpty) return string.Empty;
                throw new ArgumentOutOfRangeException(nameof(field));
            }
        }
    }
}
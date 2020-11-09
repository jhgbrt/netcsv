using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    /// <summary>
    /// A CSV line
    /// </summary>
    record CsvLine(string[] Fields, bool IsEmpty)
    {

        /// <summary>
        /// An empty CSV line
        /// </summary>
        public static readonly CsvLine Empty = new CsvLine(Array.Empty<string>(), true);

        public override string ToString() => string.Join(";", Fields);

        public string this[int field]
        {
            get
            {
                if (field < Fields.Length)
                {
                    return Fields[field];
                }

                if (IsEmpty)
                {
                    return string.Empty;
                }

                throw new ArgumentOutOfRangeException(nameof(field));
            }
        }
    }
}
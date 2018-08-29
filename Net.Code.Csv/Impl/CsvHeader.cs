using System.Collections.Generic;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    /// <summary>
    /// A CSV header line
    /// </summary>
    internal class CsvHeader : CsvLine
    {
        private readonly Dictionary<string, int> _fieldHeaderIndexes;

        public CsvHeader(IEnumerable<string> fields, string defaultHeaderName)
            : base(DefaultWhereEmpty(fields, defaultHeaderName), false)
            => _fieldHeaderIndexes = Fields.Select((f, i) => new { f, i }).ToDictionary(x => x.f, x => x.i);

        private static IEnumerable<string> DefaultWhereEmpty(IEnumerable<string> fields, string defaultHeaderName)
            => fields.Select((f, i) => string.IsNullOrWhiteSpace(f) ? defaultHeaderName + i : f).ToList();

        public int this[string headerName] => _fieldHeaderIndexes[headerName];

        public bool TryGetIndex(string name, out int index) => _fieldHeaderIndexes.TryGetValue(name, out index);
    }
}
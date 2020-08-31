using System.Collections.Generic;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    /// <summary>
    /// A CSV header line
    /// </summary>
    class CsvHeader : CsvLine
    {
        private readonly IReadOnlyDictionary<string, int> _fieldHeaderIndexes;

        public CsvHeader(IEnumerable<string> fields, string defaultHeaderName)
            : base(DefaultWhereEmpty(fields, defaultHeaderName), false)
            => _fieldHeaderIndexes = Fields.WithIndex().ToDictionary(x => x.item, x => x.index);

        private static IEnumerable<string> DefaultWhereEmpty(IEnumerable<string> fields, string defaultHeaderName)
            => fields.Select((f, i) => string.IsNullOrWhiteSpace(f) ? defaultHeaderName + i : f).ToList();

        public int this[string headerName] => _fieldHeaderIndexes[headerName];

        public bool TryGetIndex(string name, out int index) => _fieldHeaderIndexes.TryGetValue(name, out index);
    }

    static class EnumerableEx
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> input) => input.Select((t, i) => (t, i));
    }
}
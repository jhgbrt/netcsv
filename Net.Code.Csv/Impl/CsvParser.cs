using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    class CsvParser : IEnumerable<CsvLine>, IDisposable
    {
        private readonly CsvStateMachine _csvStateMachine;
        private bool _disposed;
        private readonly IDisposable _textReader;

        public CsvParser(TextReader textReader, CsvLayout layOut, CsvBehaviour behaviour)
        {
            _csvStateMachine = new CsvStateMachine(textReader, layOut, behaviour);
            _enumerator = _csvStateMachine.Lines().GetEnumerator();
            _textReader = textReader;

            var firstLine = Lines().FirstOrDefault();

            Header = (layOut, firstLine) switch
            {
                ({ Schema: not null }, _) 
                    => new CsvHeader(layOut.Schema.Columns.Select(s => s.Name).ToArray()),

                ({ HasHeaders: true }, firstLine: not null)
                    => new CsvHeader(firstLine.Fields),

                (_, firstLine: not null) 
                    => new CsvHeader(Enumerable.Repeat(string.Empty, firstLine.Fields.Length).ToArray()),
                _
                    => new CsvHeader(Array.Empty<string>())
            };

            if (!layOut.HasHeaders) 
                _cachedLine = firstLine;
        }

        private CsvLine _cachedLine;

        private readonly IEnumerator<CsvLine> _enumerator;

        public CsvHeader Header { get; }

        public int FieldCount => _csvStateMachine.FieldCount ?? -1;

        private IEnumerable<CsvLine> Lines()
        {
            if (_cachedLine != null)
            {
                yield return _cachedLine;
                _cachedLine = null;
            }

            while (_enumerator.MoveNext())
            {
                var readLine = _enumerator.Current;

                yield return readLine;
            }
        }

        public IEnumerator<CsvLine> GetEnumerator() => Lines().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _textReader.Dispose();
            _disposed = true;
        }

    }
}
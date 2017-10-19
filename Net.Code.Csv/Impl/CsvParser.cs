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
        private string _defaultHeaderName;
        IDisposable _textReader;

        public CsvParser(TextReader textReader, CsvLayout layOut, CsvBehaviour behaviour, string defaultHeaderName = "Column")
        {
            _csvStateMachine = new CsvStateMachine(textReader, layOut, behaviour);
            _enumerator = _csvStateMachine.Lines().GetEnumerator();
            Layout = layOut;
            _defaultHeaderName = defaultHeaderName ?? "Column";
            _textReader = textReader;

            var firstLine = Lines().FirstOrDefault();

            if (Layout.HasHeaders && firstLine != null)
            {
                Header = new CsvHeader(firstLine.Fields, _defaultHeaderName);
            }
            else
            {
                _cachedLine = firstLine;
            }
        }

        private CsvLine _cachedLine;

        private readonly IEnumerator<CsvLine> _enumerator;

        private CsvLayout Layout { get; }

        public CsvHeader Header { get; private set; }

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
            if (_disposed) return;
            _textReader.Dispose();
            _disposed = true;
        }

    }
}
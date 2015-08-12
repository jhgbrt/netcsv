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

        public CsvParser(TextReader textReader, CsvLayout layOut, CsvBehaviour behaviour)
        {
            _csvStateMachine = new CsvStateMachine(textReader, layOut, behaviour);
            _enumerator = _csvStateMachine.GetEnumerator();
            Layout = layOut;
        }

        public int LineNumber => _csvStateMachine.LineNumber;
        public int ColumnNumber { get; set; }
        private CsvLine _cachedLine;
        private bool _initialized;
        private CsvHeader _header;
        private readonly IEnumerator<CsvLine> _enumerator;

        private CsvLayout Layout { get; }

        public CsvHeader Header
        {
            get
            {
                Initialize();
                return _header;
            }
            private set { _header = value; }
        }

        public int FieldCount => _csvStateMachine.FieldCount ?? -1;

        public string DefaultHeaderName { private get; set; } = "Column";

        private IEnumerable<CsvLine> Lines()
        {
            Initialize();

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

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var firstLine = Lines().FirstOrDefault();

            if (Layout.HasHeaders && firstLine != null)
            {
                Header = new CsvHeader(firstLine.Fields, DefaultHeaderName);
            }
            else
            {
                _cachedLine = firstLine;
            }
        }

        public IEnumerator<CsvLine> GetEnumerator()
        {
            return Lines().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _csvStateMachine.Dispose();
            _disposed = true;
        }

    }
}
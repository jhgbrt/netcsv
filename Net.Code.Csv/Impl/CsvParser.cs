using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Net.Code.Csv.Impl
{
    class CsvParser : IEnumerable<CsvLine>, IDisposable
    {
        private readonly BufferedCsvLineGenerator _bufferedCsvLineGenerator;
        private bool _disposed;

        public CsvParser(TextReader textReader, int bufferSize, CsvLayout layOut, CsvBehaviour behaviour)
        {
            _bufferedCsvLineGenerator = new BufferedCsvLineGenerator(textReader, bufferSize, layOut, behaviour);
            _enumerator = _bufferedCsvLineGenerator.GetEnumerator();
            Layout = layOut;
        }

        public int LineNumber { get { return _bufferedCsvLineGenerator.LineNumber; }}
        public int ColumnNumber { get; set; }
        private CsvLine _cachedLine;
        private bool _initialized;
        private CsvHeader _header;
        private string _defaultHeaderName = "Column";
        private IEnumerator<CsvLine> _enumerator;

        private CsvLayout Layout { get; set; }

        public CsvHeader Header
        {
            get
            {
                Initialize();
                return _header;
            }
            set { _header = value; }
        }

        public int FieldCount
        {
            get { return _bufferedCsvLineGenerator.FieldCount ?? -1; }
        }

        public string DefaultHeaderName
        {
            get { return _defaultHeaderName; }
            set { _defaultHeaderName = value; }
        }

        public IEnumerable<CsvLine> Lines()
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
            _bufferedCsvLineGenerator.Dispose();
            _disposed = true;
        }

    }
}
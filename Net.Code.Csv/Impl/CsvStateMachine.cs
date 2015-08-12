using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Code.Csv.Impl
{
    class BufferedCharEnumerator : IEnumerator<char>
    {

        char? Peek ()
        {
            if (_idx + 1 >= _buffer.Length)
            {
                int peek = _textReader.Peek();
                return peek < 0 ? null : (char?)peek;
            }
            return _buffer[_idx + 1];
        }

        private int _lineNumber;
        private int _currentColumn;
        private int _idx;
        private readonly char[] _buffer;
        private readonly TextReader _textReader;
        private int _charsRead;

        public BufferedCharEnumerator(TextReader textReader, int bufferSize = 16)
        {
            _textReader = textReader;
            _buffer = new char[bufferSize];
            _lineNumber = 0;
            _currentColumn = 0;
            _idx = -1;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            _idx++;
            if (_idx >= _buffer.Length)
            {
                _charsRead = _textReader.ReadBlock(_buffer, 0, _buffer.Length);
                _idx = 0;
            }
            if (_charsRead < 0) return false;
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public char Current => _buffer[_idx];
        object IEnumerator.Current => Current;
    }

    internal class CsvStateMachine : IEnumerable<CsvLine>
    {
        private bool _debug;
        private Location _whereAmI;
        private bool _wasQuoted;
        private readonly StringBuilder _field = new StringBuilder();
        private readonly StringBuilder _mayHaveToBeAdded = new StringBuilder();
        private readonly TextReader _textReader;
        private readonly CsvLayout _csvLayout;
        private readonly CsvBehaviour _behaviour;
        private bool _disposed;
        private int _lineNumber;
        private int _currentColumn;
        private List<string> _fields;
        private char _currentChar;


        public CsvStateMachine(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour, bool debug = false)
        {
            _textReader = textReader;
            _csvLayout = csvLayout;
            _behaviour = behaviour;
            _debug = debug;
        }

        public int LineNumber => _lineNumber;

        private string CurrentRawData => _field.ToString(); // TODO keep last x chars?

        public int? FieldCount { get; private set; }

        private Location WhereAmI
        {
            get { return _whereAmI; }
            set
            {
                DbgLog($"{_whereAmI} => {value}");
                _whereAmI = value;
            }
        }

        private bool WasQuoted
        {
            get { return _wasQuoted; }
            set
            {
                if (_wasQuoted != value)
                {
                    DbgLog($"WasQuoted = {value}");
                }
                _wasQuoted = value;
            }
        }

        public IEnumerable<CsvLine> Split()
        {
            return SplitPrivate().Where(line => !line.IsEmpty || !_behaviour.SkipEmptyLines);
        }

        private char? Peek()
        {
            int peek = _textReader.Peek();
            return peek < 0 ? null : (char?)peek;
        }

        private bool AtNewLine()
        {
            return _currentChar == '\r' || _currentChar == '\n';
        }

        private bool AtCRLF()
        {
            return _currentChar == '\r' && Peek() == '\n';
        }

        private static CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

        private static string ToLiteral(object input)
        {
            using (var writer = new StringWriter())
            {
                provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                return writer.ToString();
            }
        }
        private IEnumerable<CsvLine> SplitPrivate()
        {
            StartLine();

            bool lastCharWasDelimiter = false;

            foreach (char c in ReadCharacters())
            {
                _currentChar = c;
                _currentColumn++;
                DbgLog($"Current character: '{ToLiteral(_currentChar)}'. Location is {WhereAmI} ({LineNumber},{_currentColumn}).");

                switch (WhereAmI)
                {
                    case Location.BeginningOfLine:
                        if (AtCRLF())
                        {
                            // consider \r\n as one: skip \r
                            continue;
                        }

                        if (AtNewLine())
                        {
                            if (!_behaviour.SkipEmptyLines)
                                yield return CsvLine.Empty;
                            StartLine();
                        }
                        else if (_currentChar == _csvLayout.Comment)
                        {
                            WhereAmI = Location.Comment;
                        }
                        else if (_currentChar == _csvLayout.Quote)
                        {
                            WasQuoted = true;
                            WhereAmI = Location.InsideQuotedField;
                        }
                        else
                        {
                            WhereAmI = Location.InsideField;
                            goto InsideField;
                        }
                        break;
                    case Location.Comment:
                        if (AtCRLF())
                        {
                            // consider \r\n as one: skip \r
                            continue;
                        }
                        if (AtNewLine())
                        {
                            StartLine();
                        }
                        break;
                    case Location.OutsideField:
                        // after a delimiter: decide whether to go to normal or quoted field
                        // white space may have to be added to the next field
                        // if the next field is quoted, whitespace has to be skipped

                        if (AtNewLine())
                        {
                            _field.Append(_mayHaveToBeAdded);
                            AddField();
                            WhereAmI = Location.EndOfLine;
                            goto EndOfLine;
                        }
                        else if (char.IsWhiteSpace(_currentChar))
                        {
                            _mayHaveToBeAdded.Append(_currentChar);
                        }
                        else if (_currentChar == _csvLayout.Quote)
                        {
                            // the next field is quoted
                            // accumulated white space must be ignored
                            WasQuoted = true;
                            _mayHaveToBeAdded.Clear();
                            WhereAmI = Location.InsideQuotedField;
                        }
                        else
                        {
                            // found a 'normal' character, we were actually inside the next field already
                            _field.Append(_mayHaveToBeAdded);
                            _mayHaveToBeAdded.Clear();
                            WhereAmI = Location.InsideField;
                            goto InsideField;
                        }
                        break;
                    case Location.EndOfLine:
                        EndOfLine:
                        if (_currentChar == '\r' && Peek() == '\n')
                        {
                            // consider \r\n as one: skip \r
                            continue;
                        }
                        else if (_currentChar == '\r' || _currentChar == '\n')
                        {
                            //AddField();
                            var line = CreateLine(_fields);
                            yield return line;
                            StartLine();
                        }
                        break;

                    case Location.Escaped:
                        _field.Append(_currentChar);
                        WhereAmI = Location.InsideQuotedField;
                        break;
                    case Location.InsideQuotedField:
                        if (_currentChar == _csvLayout.Escape &&
                            (Peek() == _csvLayout.Quote || Peek() == _csvLayout.Escape))
                        {
                            WhereAmI = Location.Escaped;
                            break; // skip the escape character
                        }
                        if (_currentChar == _csvLayout.Quote)
                        {
                            // there are 2 possibilities: 
                            // - either the quote is just part of the field
                            //   (e.g. "foo,"bar "baz"", foobar")
                            // - or the quote is actually the end of this field
                            // => start capturing after the quote; check for delimiter 
                            WhereAmI = Location.AfterSecondQuote;
                            _mayHaveToBeAdded.Length = 0;
                            _mayHaveToBeAdded.Append(_currentChar);
                        }
                        else
                        {
                            _field.Append(_currentChar);
                        }
                        break;

                    case Location.AfterSecondQuote:
                        // we need to detect if we're actually at the end of a field. This is
                        // the case when the first non-whitespace character is the delimiter
                        // or end of line

                        if (_currentChar == _csvLayout.Delimiter)
                        {
                            // the second quote did mark the end of the field
                            _mayHaveToBeAdded.Clear();
                            AddField();
                            WasQuoted = false;
                        }
                        else if (_currentChar == '\r' || _currentChar == '\n')
                        {
                            _mayHaveToBeAdded.Clear();
                            AddField();
                            WasQuoted = false;
                            WhereAmI = Location.EndOfLine;
                            goto EndOfLine;
                        }
                        else if (_currentChar == _csvLayout.Quote)
                        {
                            _field.Append(_mayHaveToBeAdded);
                            _mayHaveToBeAdded.Clear();
                            _mayHaveToBeAdded.Append(_currentChar);
                        }
                        else if (char.IsWhiteSpace(_currentChar))
                        {
                            // as long as we encounter white space, keep accumulating
                            _mayHaveToBeAdded.Append(_currentChar);
                        }
                        else
                        {
                            // the second quote did NOT mark the end of the field, so we're still 'inside' the field
                            // this means that the quote is now confirmed to have occurred inside the quoted field
                            if (_behaviour.QuotesInsideQuotedFieldAction == QuotesInsideQuotedFieldAction.ThrowException)
                                throw new MalformedCsvException(CurrentRawData, _currentColumn, LineNumber,
                                    _fields.Count);

                            if (_behaviour.QuotesInsideQuotedFieldAction ==
                                QuotesInsideQuotedFieldAction.AdvanceToNextLine)
                            {
                                WhereAmI = Location.ParseError;
                                break;
                            }

                            _field.Append(_mayHaveToBeAdded);
                            _field.Append(_currentChar);
                            WhereAmI = Location.InsideQuotedField;
                        }
                        break;
                    case Location.ParseError:
                        // A parse error was detected. Ignore until EOL.
                        if (_currentChar == '\r' && Peek() == '\n')
                        {
                            continue;
                        }

                        if (_currentChar == '\r' || _currentChar == '\n')
                        {
                            StartLine();
                            continue;
                        }
                        break;
                    case Location.InsideField:
                        InsideField:
                        if (_currentChar == _csvLayout.Delimiter)
                        {
                            AddField();
                        }
                        else if (_currentChar == '\r' || _currentChar == '\n')
                        {
                            AddField();
                            WhereAmI = Location.EndOfLine;
                            goto EndOfLine;
                        }
                        else
                        {
                            _field.Append(_currentChar);
                        }
                        break;
                }

                if (!lastCharWasDelimiter || !Char.IsWhiteSpace(_currentChar))
                {
                    lastCharWasDelimiter = _currentChar == _csvLayout.Delimiter;
                }

            }

            // if last line does not end with a newline, we still need to yield it
            if (WhereAmI != Location.EndOfLine)
            {
                AddField();
                var line = CreateLine(_fields);
                yield return line;
            }
        }

        private IEnumerable<char> ReadCharacters()
        {
            var i = _textReader.Read();
            while (i >= 0)
            {
                yield return (char) i;
                i = _textReader.Read();
            }
        }

        private void AddField()
        {
            var result = _field.ToString();
            if (_behaviour.TrimmingOptions == ValueTrimmingOptions.All
                || (WasQuoted && _behaviour.TrimmingOptions == ValueTrimmingOptions.QuotedOnly)
                || (!WasQuoted && _behaviour.TrimmingOptions == ValueTrimmingOptions.UnquotedOnly))
                result = result.Trim();
            DbgLog($"Adding field: {ToLiteral(result)}");
            _fields.Add(result);
            _field.Clear();
            WhereAmI = Location.OutsideField;
        }

        private void DbgLog(string message)
        {
            Debug.WriteLineIf(_debug, message);
        }

        private CsvLine CreateLine(List<string> fields)
        {
            DbgLog($"Creating line at {LineNumber} ({fields.Count} fields)");
            bool isEmpty = fields.Count == 0 || (fields.Count == 1 && string.IsNullOrEmpty(fields[0]));
            var line = new CsvLine(fields, isEmpty);

            if (!FieldCount.HasValue && (!line.IsEmpty || !_behaviour.SkipEmptyLines))
                FieldCount = fields.Count;

            var count = fields.Count();

            if (line.IsEmpty || count < FieldCount)
            {
                DbgLog($"Line is empty or has too little fields, missing field action = {_behaviour.MissingFieldAction}");
            }

            if (!line.IsEmpty && count < FieldCount)
            {
                if (_behaviour.MissingFieldAction == MissingFieldAction.ParseError)
                    throw new MissingFieldCsvException(CurrentRawData, _currentColumn, LineNumber, fields.Count());
            }

            if (count < FieldCount)
            {
                string s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
                while (fields.Count < FieldCount) fields.Add(s);
                line = new CsvLine(fields, isEmpty);
            }
            DbgLog("Yielding line: " + line);
            return line;
        }

        private void StartLine()
        {
            _fields = new List<string>();
            _lineNumber++;
            _currentColumn = 0;
            _field.Clear();
            _mayHaveToBeAdded.Clear();
            WhereAmI = Location.BeginningOfLine;
            WasQuoted = false;
            _field.Clear();
            DbgLog($"Start line {LineNumber}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _textReader.Dispose();
            _disposed = true;
        }

        public IEnumerator<CsvLine> GetEnumerator()
        {
            return Split().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
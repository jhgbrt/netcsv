using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Net.Code.Csv.Exceptions;

namespace Net.Code.Csv
{
    internal class BufferedCsvLineGenerator : IEnumerable<CsvLine>
    {
        private Location _whereAmI;
        private int _idx;
        private bool _wasQuoted;
        private StringBuilder _field;
        private StringBuilder _mayHaveToBeAdded;
        private readonly char[] _buffer;
        private readonly TextReader _textReader;
        private readonly CsvLayout _csvLayout;
        private readonly CsvBehaviour _behaviour;
        private bool _disposed;
        private int _lineNumber;
        private int? _fieldCount;
        private int _currentColumn;

        public BufferedCsvLineGenerator(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour)
            : this(textReader, 16, csvLayout, behaviour)
        {
            
        }   

        public BufferedCsvLineGenerator(TextReader textReader, int bufferSize, CsvLayout csvLayout, CsvBehaviour behaviour)
        {
            _textReader = textReader;
            _csvLayout = csvLayout;
            _behaviour = behaviour;
            _buffer = new char[bufferSize];
        }

        public int LineNumber
        {
            get { return _lineNumber; }
        }

        public string CurrentRawData
        {
            get { return new string(_buffer); }
        }

        public int? FieldCount
        {
            get
            {
                return _fieldCount;
            }
        }

        string CreateField(StringBuilder builder, bool quoted)
        {
            var result = builder.ToString();
            if (_behaviour.TrimmingOptions == ValueTrimmingOptions.All
                || (quoted && _behaviour.TrimmingOptions == ValueTrimmingOptions.QuotedOnly)
                || (!quoted && _behaviour.TrimmingOptions == ValueTrimmingOptions.UnquotedOnly))
                result = result.Trim();
            return result;
        }

        public IEnumerable<CsvLine> Split()
        {
            return SplitPrivate().Where(line => !line.IsEmpty || !_behaviour.SkipEmptyLines);
        }

        private IEnumerable<CsvLine> SplitPrivate()
        {

            var fields = new List<string>();

            Func<char?> peekNext = () =>
                                    {
                                        if (_idx + 1 >= _buffer.Length)
                                        {
                                            int peek = _textReader.Peek();
                                            return peek < 0 ? null : (char?)peek;
                                        }
                                        return _buffer[_idx + 1];
                                    };

            StartLine();

            bool lastCharWasDelimiter = false;

            while (_textReader.Peek() > 0)
            {

                var charsRead = _textReader.ReadBlock(_buffer, 0, _buffer.Length);
                _idx = 0;
                while (_idx < charsRead)
                {
                    char currentChar = _buffer[_idx];

                    switch (_whereAmI)
                    {
                        case Location.BeginningOfLine:
                            _currentColumn = 0;
                            if (currentChar == _csvLayout.Delimiter)
                            {
                                _whereAmI = Location.InsideField;
                                continue;
                            }
                            if (currentChar == '\r' || currentChar == '\n')
                            {
                                _whereAmI = Location.EndOfLine;
                                continue;
                            }
                            _whereAmI = currentChar == _csvLayout.Comment ? Location.Comment : Location.OutsideField;
                            continue;
                        case Location.Comment:
                            if (currentChar == '\r' || currentChar == '\n')
                            {
                                _whereAmI = Location.EndOfLine;
                                continue;
                            }
                            break;
                        case Location.OutsideField:
                            if (currentChar == '\r' || currentChar == '\n')
                            {
                                _whereAmI = Location.EndOfLine;
                                continue;
                            }

                            if (char.IsWhiteSpace(currentChar))
                            {
                                _mayHaveToBeAdded.Append(currentChar);
                                break;
                            }

                            if (currentChar == _csvLayout.Quote)
                            {
                                _wasQuoted = true;
                                _mayHaveToBeAdded.Length = 0;
                                _whereAmI = Location.InsideQuotedField;
                            }
                            else
                            {
                                _field.Append(_mayHaveToBeAdded);
                                _mayHaveToBeAdded.Length = 0;
                                _whereAmI = Location.InsideField;
                                continue;
                            }
                            break;
                        case Location.EndOfLine:
                            if (currentChar == '\r' && peekNext() == '\n')
                            {
                                _idx++;
                                _currentColumn++;
                            }

                            if (_field.Length > 0 || fields.Count > 0)
                            {
                                if (_field.Length > 0 || lastCharWasDelimiter)
                                    fields.Add(CreateField(_field, _wasQuoted));
                            }
                            var line = CreateLine(fields);
                            yield return line;
                            fields.Clear();
                            StartLine();
                            break;

                        case Location.Escaped:
                            _field.Append(currentChar);
                            _whereAmI = Location.InsideQuotedField;
                            break;
                        case Location.InsideQuotedField:
                            if (currentChar == _csvLayout.Escape &&
                                (peekNext() == _csvLayout.Quote || peekNext() == _csvLayout.Escape))
                            {
                                _whereAmI = Location.Escaped;
                                break; // skip the escape character
                            }
                            if (currentChar == _csvLayout.Quote)
                            {
                                // there are 2 possibilities: 
                                // - either the quote is just part of the field
                                //   (e.g. "foo,"bar "baz"", foobar")
                                // - or the quote is actually the end of this field
                                // => start capturing after the quote; check for delimiter 
                                _whereAmI = Location.AfterSecondQuote;
                                _mayHaveToBeAdded.Length = 0;
                                _mayHaveToBeAdded.Append(currentChar);
                            }
                            else
                            {
                                _field.Append(currentChar);
                            }
                            break;

                        case Location.AfterSecondQuote:
                            // we need to detect if we're actually at the end of a field. This is
                            // the case when the first non-whitespace character is the delimiter
                            // or end of line

                            if (currentChar == _csvLayout.Delimiter || currentChar == '\r' || currentChar == '\n')
                            {
                                // the second quote did mark the end of the field
                                _mayHaveToBeAdded.Length = 0;
                                fields.Add(CreateField(_field, _wasQuoted));
                                _field.Length = 0;
                                _wasQuoted = false;
                                if (currentChar == '\r' || currentChar == '\n')
                                {
                                    _whereAmI = Location.EndOfLine;
                                    continue;
                                }
                                _whereAmI = Location.OutsideField;
                                break;
                            }

                            if (currentChar == _csvLayout.Quote)
                            {
                                _field.Append(_mayHaveToBeAdded);
                                _mayHaveToBeAdded.Length = 0;
                                _mayHaveToBeAdded.Append(currentChar);
                                break;
                            }

                            _mayHaveToBeAdded.Append(currentChar);

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                // the second quote did NOT mark the end of the field, so we're still 'inside' the field
                            
                                if (_behaviour.QuotesInsideQuotedFieldAction == QuotesInsideQuotedFieldAction.ThrowException)
                                    throw new MalformedCsvException(new string(_buffer), _idx, LineNumber-1, fields.Count);

                                if (_behaviour.QuotesInsideQuotedFieldAction == QuotesInsideQuotedFieldAction.AdvanceToNextLine)
                                {
                                    _whereAmI = Location.ParseError;
                                    break;
                                }

                                _field.Append(_mayHaveToBeAdded);
                                _whereAmI = Location.InsideQuotedField;
                            }
                            break;
                        case Location.ParseError :
                            if (currentChar == '\r' || currentChar == '\n')
                            {
                                if (peekNext() == '\r' || peekNext() == '\n')
                                {
                                    _idx++;
                                    _currentColumn++;
                                }
                                fields.Clear();
                                _mayHaveToBeAdded.Length = 0;
                                _field.Length = 0;
                                _whereAmI = Location.BeginningOfLine;
                                continue;
                            }
                            break;
                        case Location.InsideField:
                            if (currentChar == _csvLayout.Delimiter)
                            {
                                fields.Add(CreateField(_field, _wasQuoted));
                                _field.Length = 0;
                                _whereAmI = Location.OutsideField;
                                break;
                            }
                            if (currentChar == '\r' || currentChar == '\n')
                            {
                                _whereAmI = Location.EndOfLine;
                                continue;
                            }
                            _field.Append(currentChar);
                            break;
                    }
                    _idx++;
                    _currentColumn++;
                    if (_whereAmI == Location.EndOfLine)
                    {
                        break;
                    }

                    if (!lastCharWasDelimiter || !Char.IsWhiteSpace(currentChar))
                    {
                        lastCharWasDelimiter = currentChar == _csvLayout.Delimiter;
                    }
                }


                if (_whereAmI == Location.EndOfLine)
                {
                    var line = CreateLine(fields);
                    yield return line;
                }
            }

            if (_whereAmI != Location.EndOfLine)
            {
                fields.Add(CreateField(_field, _wasQuoted));
                var line = CreateLine(fields);
                yield return line;
            }
        }

        private CsvLine CreateLine(List<string> fields)
        {
            bool isEmpty = fields.Count == 0 || (fields.Count == 1 && string.IsNullOrEmpty(fields[0]));
            var line = new CsvLine(fields, isEmpty);

            if (!_fieldCount.HasValue && (!line.IsEmpty || !_behaviour.SkipEmptyLines))
                _fieldCount = fields.Count;

            var count = fields.Count();

            if (!line.IsEmpty && count < _fieldCount)
            {
                if (_behaviour.MissingFieldAction == MissingFieldAction.ParseError)
                    throw new MissingFieldCsvException(CurrentRawData, _currentColumn, LineNumber, fields.Count());
            }

            if (count < _fieldCount)
            {
                string s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
                while (fields.Count < _fieldCount) fields.Add(s);
                line = new CsvLine(fields, isEmpty);
            }
            return line;
        }

        private void StartLine()
        {
            _lineNumber = LineNumber + 1;
            _mayHaveToBeAdded = new StringBuilder();
            _whereAmI = Location.BeginningOfLine;
            _wasQuoted = false;
            _field = new StringBuilder();
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
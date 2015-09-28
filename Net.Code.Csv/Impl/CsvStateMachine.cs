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
    internal class CsvStateMachine : IEnumerable<CsvLine>
    {
        private readonly bool _debug;
        private Location _whereAmI;
        private bool _wasQuoted;
        private readonly StringBuilder _field = new StringBuilder();
        private readonly StringBuilder _tentative = new StringBuilder();
        private readonly TextReader _textReader;
        private readonly CsvLayout _csvLayout;
        private readonly CsvBehaviour _behaviour;
        private bool _disposed;
        private int _lineNumber;
        private int _currentColumn;
        private List<string> _fields;


        public CsvStateMachine(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour, bool debug = false)
        {
            _textReader = textReader;
            _csvLayout = csvLayout;
            _behaviour = behaviour;
            _debug = debug;
        }

        public int LineNumber => _lineNumber;

        private StringBuilder CurrentRawData = new StringBuilder(); // TODO keep last x chars?

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

        private bool AtNewLine(char current)
        {
            return current == '\r' || current == '\n';
        }

        private static readonly CodeDomProvider Provider = CodeDomProvider.CreateProvider("CSharp");

        private static readonly IDictionary<string, string> Literals = new Dictionary<string, string>();
        private static string ToLiteral(string input)
        {
            if (!Literals.ContainsKey(input))
            {
                using (var writer = new StringWriter())
                {
                    Provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    Literals[input] = writer.ToString();
                }
            }
            return Literals[input];
        }

        private IEnumerable<CsvLine> SplitPrivate()
        {
            StartLine();

            foreach (char c in ReadCharacters())
            {
                var currentChar = c;

                _currentColumn++;
                CurrentRawData.Append(c);
                if (CurrentRawData.Length > 32) CurrentRawData.Remove(0, 1);
                DbgLog($"Current character: '{ToLiteral(currentChar.ToString())}'. Location is {WhereAmI} ({LineNumber},{_currentColumn}).");

                switch (WhereAmI)
                {
                    case Location.BeginningOfLine:
                        if (AtNewLine(currentChar))
                        {
                            if (!_behaviour.SkipEmptyLines)
                                yield return CsvLine.Empty;
                            StartLine();
                        }
                        else if (currentChar == _csvLayout.Comment)
                        {
                            WhereAmI = Location.Comment;
                        }
                        else if (currentChar == _csvLayout.Quote)
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
                        if (AtNewLine(currentChar))
                        {
                            StartLine();
                        }
                        break;
                    case Location.OutsideField:
                        // after a delimiter: decide whether to go to normal or quoted field
                        // white space may have to be added to the next field
                        // if the next field is quoted, whitespace has to be skipped

                        if (AtNewLine(currentChar))
                        {
                            _field.Append(_tentative);
                            AddField();
                            WhereAmI = Location.EndOfLine;
                            goto EndOfLine;
                        }
                        else if (char.IsWhiteSpace(currentChar))
                        {
                            _tentative.Append(currentChar);
                        }
                        else if (currentChar == _csvLayout.Quote)
                        {
                            // the next field is quoted
                            // accumulated white space must be ignored
                            WasQuoted = true;
                            _tentative.Clear();
                            WhereAmI = Location.InsideQuotedField;
                        }
                        else
                        {
                            // found a 'normal' character, we were actually inside the next field already
                            _field.Append(_tentative);
                            _tentative.Clear();
                            WhereAmI = Location.InsideField;
                            goto InsideField;
                        }
                        break;
                    case Location.EndOfLine:
                        EndOfLine:
                        if (AtNewLine(currentChar))
                        {
                            var line = CreateLine(_fields);
                            yield return line;
                            StartLine();
                        }
                        break;

                    case Location.Escaped:
                        _field.Append(currentChar);
                        WhereAmI = Location.InsideQuotedField;
                        break;
                    case Location.InsideQuotedField:
                        if (currentChar == _csvLayout.Escape &&
                            (Peek() == _csvLayout.Quote || Peek() == _csvLayout.Escape))
                        {
                            WhereAmI = Location.Escaped;
                            break; // skip the escape character
                        }
                        if (currentChar == _csvLayout.Quote)
                        {
                            // there are 2 possibilities: 
                            // - either the quote is just part of the field
                            //   (e.g. "foo,"bar "baz"", foobar")
                            // - or the quote is actually the end of this field
                            // => start capturing after the quote; check for delimiter 
                            WhereAmI = Location.AfterSecondQuote;
                            _tentative.Clear();
                            _tentative.Append(currentChar);
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

                        if (currentChar == _csvLayout.Delimiter)
                        {
                            // the second quote did mark the end of the field
                            _tentative.Clear();
                            AddField();
                            WasQuoted = false;
                        }
                        else if (AtNewLine(currentChar))
                        {
                            // the second quote did mark the end of the field
                            _tentative.Clear();
                            AddField();
                            WasQuoted = false;
                            WhereAmI = Location.EndOfLine;
                            goto EndOfLine;
                        }
                        else if (currentChar == _csvLayout.Quote)
                        {
                            _field.Append(_tentative);
                            _tentative.Clear();
                            _tentative.Append(currentChar);
                        }
                        else if (char.IsWhiteSpace(currentChar))
                        {
                            // as long as we encounter white space, keep accumulating
                            _tentative.Append(currentChar);
                        }
                        else
                        {
                            // the second quote did NOT mark the end of the field, so we're still 'inside' the field
                            // this means that the quote is now confirmed to have occurred inside the quoted field
                            if (_behaviour.QuotesInsideQuotedFieldAction == QuotesInsideQuotedFieldAction.ThrowException)
                                throw new MalformedCsvException(CurrentRawData.ToString(), _currentColumn, LineNumber,
                                    _fields.Count);

                            if (_behaviour.QuotesInsideQuotedFieldAction ==
                                QuotesInsideQuotedFieldAction.AdvanceToNextLine)
                            {
                                WhereAmI = Location.ParseError;
                                break;
                            }

                            _field.Append(_tentative);
                            _field.Append(currentChar);
                            WhereAmI = Location.InsideQuotedField;
                        }
                        break;
                    case Location.ParseError:
                        // A parse error was detected. Ignore until EOL.
                        if (AtNewLine(currentChar))
                        {
                            StartLine();
                        }
                        break;
                    case Location.InsideField:
                        InsideField:
                        if (currentChar == _csvLayout.Delimiter)
                        {
                            AddField();
                        }
                        else if (AtNewLine(currentChar))
                        {
                            AddField();
                            WhereAmI = Location.EndOfLine;
                            goto EndOfLine;
                        }
                        else
                        {
                            _field.Append(currentChar);
                        }
                        break;
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
                yield return (char)i;
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

            if (fields.Count == 1)
                Debugger.Break();

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
                    throw new MissingFieldCsvException(CurrentRawData.ToString(), _currentColumn, LineNumber, fields.Count());
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

        class Line
        {
            public string Number { get; }
            public string Field { get; }

        }

        private void StartLine()
        {
            _fields = new List<string>();
            _lineNumber++;
            _currentColumn = 0;
            _field.Clear();
            _tentative.Clear();
            WhereAmI = Location.BeginningOfLine;
            WasQuoted = false;
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
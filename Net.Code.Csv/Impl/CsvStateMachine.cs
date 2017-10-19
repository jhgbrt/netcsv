using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Code.Csv.Impl
{

    internal class CsvStateMachine
    {
        private readonly bool _debug;
        private readonly TextReader _textReader;
        private readonly CsvLayout _csvLayout;
        private readonly CsvBehaviour _behaviour;
        public CsvStateMachine(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour, bool debug = false)
        {
            _textReader = textReader;
            _csvLayout = csvLayout;
            _behaviour = behaviour;
            _debug = debug;
        }


        Location _whereAmI = Location.BeginningOfLine;
        bool _skipNextChar = false;
        char _currentChar = '\0';
        bool _wasQuoted = false;
        StringBuilder _field = new StringBuilder();
        StringBuilder _tentative = new StringBuilder();
        List<string> _fields = null;
        StringBuilder _currentRawData = new StringBuilder();
        private int _lineNumber;
        private int _currentColumn;


        public int? FieldCount { get; private set; }

        public IEnumerable<CsvLine> Lines()
        {
            return LinesImpl().Where(line => !line.IsEmpty || !_behaviour.SkipEmptyLines);
        }

        private static readonly IDictionary<string, string> Literals = new Dictionary<string, string>();
        private static string ToLiteral(string input)
        {
            if (!Literals.ContainsKey(input))
            {
                Literals[input] = input;
                // TODO use Roslyn 
                // Literals[input] = Literal(input).ToString();
            }
            return Literals[input];
        }

        private IEnumerable<CsvLine> LinesImpl()
        {

            StartLine();

            while (ReadNextCharacter())
            {
                Func<IEnumerable<CsvLine>> f = () => Enumerable.Empty<CsvLine>();
                switch (_whereAmI)
                {
                    case Location.BeginningOfLine:
                        f = BeginningOfLine;
                        break;
                    case Location.Comment:
                        f = Comment;
                        break;
                    case Location.InsideField:
                        f = InsideField;
                        break;
                    case Location.OutsideField:
                        f = OutsideField;
                        break;
                    case Location.EndOfLine:
                        f = EndOfLine;
                        break;
                    case Location.Escaped:
                        f = Escaped;
                        break;
                    case Location.InsideQuotedField:
                        f = InsideQuotedField;
                        break;
                    case Location.AfterSecondQuote:
                        f = AfterSecondQuote;
                        break;
                    case Location.ParseError:
                        f = ParseError;
                        break;
                    default:
                        f = () => Enumerable.Empty<CsvLine>();
                        break;
                }
                foreach (var line in f()) yield return line;
            }

            foreach (var line in Remaining())
                yield return line;

        }
        bool ReadNextCharacter()
        {
            if (!_skipNextChar)
            {
                var i = _textReader.Read();
                if (i < 0) return false;
                _currentChar = (char)i;
                _currentColumn++;
                _currentRawData.Append(_currentChar);
                if (_currentRawData.Length > 32) _currentRawData.Remove(0, 1);
                DbgLog($"Current character: '{ToLiteral(_currentChar.ToString())}'. Location is {_whereAmI} ({_lineNumber},{_currentColumn}).");
            }
            else
            {
                _skipNextChar = false;
            }
            return true;
        }

        void AddField()
        {
            var result = _field.ToString();
            if (_behaviour.TrimmingOptions == ValueTrimmingOptions.All
                || (_wasQuoted && _behaviour.TrimmingOptions == ValueTrimmingOptions.QuotedOnly)
                || (!_wasQuoted && _behaviour.TrimmingOptions == ValueTrimmingOptions.UnquotedOnly))
                result = result.Trim();
            DbgLog($"Adding field: {ToLiteral(result)}");
            _fields.Add(result);
            _field.Clear();
        }

        CsvLine CreateLine()
        {
            DbgLog($"Creating line at {_lineNumber} ({_fields.Count} fields)");

            bool isEmpty = _fields.Count == 0 || (_fields.Count == 1 && string.IsNullOrEmpty(_fields[0]));

            var line = new CsvLine(_fields, isEmpty);

            if (!FieldCount.HasValue && (!line.IsEmpty || !_behaviour.SkipEmptyLines))
                FieldCount = _fields.Count;

            var count = _fields.Count();

            if (line.IsEmpty || count < FieldCount)
            {
                DbgLog($"Line is empty or has too little fields, missing field action = {_behaviour.MissingFieldAction}");
            }

            if (!line.IsEmpty && count < FieldCount)
            {
                if (_behaviour.MissingFieldAction == MissingFieldAction.ParseError)
                    throw new MissingFieldCsvException(_currentRawData.ToString(), _currentColumn, _lineNumber, _fields.Count());
            }

            if (count < FieldCount)
            {
                string s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
                while (_fields.Count < FieldCount) _fields.Add(s);
                line = new CsvLine(_fields, isEmpty);
            }
            DbgLog("Yielding line: " + line);
            return line;
        }

        void StartLine()
        {
            _fields = new List<string>();
            _lineNumber++;
            _currentColumn = 0;
            _field.Clear();
            _tentative.Clear();
            DbgLog($"Start line {_lineNumber}");
        }

        IEnumerable<CsvLine> BeginningOfLine()
        {
            // begin of line can be newline, comment, quote or other 
            if (_currentChar.IsNewLine())
            {
                yield return CsvLine.Empty;
                StartLine();
                _wasQuoted = false;
            }
            else if (_currentChar == _csvLayout.Comment)
            {
                _whereAmI = Location.Comment;
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                _wasQuoted = true;
                _whereAmI = Location.InsideQuotedField;
            }
            else
            {
                _whereAmI = Location.InsideField;
                _skipNextChar = true;
            }
        }

        IEnumerable<CsvLine> Comment()
        {
            // If we're processing a comment, there is nothing to do. 
            // When we encounter a newline character we simply start a new line.
            if (_currentChar.IsNewLine())
            {
                StartLine();
                _wasQuoted = false;
                _whereAmI = Location.BeginningOfLine;
            }
            yield break;
        }

        IEnumerable<CsvLine> InsideField()
        {
            // Inside a non-quoted field, we can encounter either a delimiter
            // or a new line. Otherwise, we accumulate the character for the current field.
            if (_currentChar == _csvLayout.Delimiter)
            {
                // end of field because delimiter
                AddField();
                _whereAmI = Location.OutsideField;
            }
            else if (_currentChar.IsNewLine())
            {
                // end of field because newline
                AddField();
                _whereAmI = Location.EndOfLine;
                _skipNextChar = true;
            }
            else
            {
                // keep going
                _field.Append(_currentChar);
            }
            yield break;
        }

        IEnumerable<CsvLine> OutsideField()
        {
            // Outside field (after a delimiter): find out whether next field is quoted
            // white space may have to be added to the next field (if it is not quoted 
            // and we don't want to trim
            // if the next field is quoted, whitespace has to be skipped in any case
            if (_currentChar.IsNewLine())
            {
                _field.Append(_tentative);
                _tentative.Clear();
                AddField();
                // transition to end of line
                _whereAmI = Location.EndOfLine;
                _skipNextChar = true;
            }
            else if (char.IsWhiteSpace(_currentChar))
            {
                // found more whitespace, accumulate until we find a non-whitespace character
                _tentative.Append(_currentChar);
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                // the next field is quoted
                // accumulated white space must be ignored
                _wasQuoted = true;
                _tentative.Clear();
                _whereAmI = Location.InsideQuotedField;
            }
            else
            {
                // found a 'normal' (non-newline, non-whitespace, non-quote) character, 
                // this means we were actually inside the next field already
                // accumulated whitespace should be added to the current field
                _field.Append(_tentative);
                _tentative.Clear();
                _whereAmI = Location.InsideField;
                _skipNextChar = true;
            }
            yield break;
        }

        IEnumerable<CsvLine> EndOfLine()
        {
            yield return CreateLine();
            StartLine();
            _wasQuoted = false;
            _whereAmI = Location.BeginningOfLine;
        }

        IEnumerable<CsvLine> Escaped()
        {
            _field.Append(_currentChar);
            _whereAmI = Location.InsideQuotedField;
            yield break;
        }

        IEnumerable<CsvLine> InsideQuotedField()
        {
            if (_csvLayout.IsEscape(_currentChar, Peek()))
            {
                _whereAmI = Location.Escaped;
                // skip the escape character
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                // there are 2 possibilities: 
                // - either the quote is just part of the field
                //   (e.g. "foo,"bar "baz"", foobar")
                // - or the quote is actually the end of this field
                // => start capturing after the quote; check for delimiter 
                _whereAmI = Location.AfterSecondQuote;
                _tentative.Clear();
                _tentative.Append(_currentChar);
            }
            else
            {
                _field.Append(_currentChar);
            }
            yield break;
        }

        IEnumerable<CsvLine> AfterSecondQuote()
        {
            // after second quote, we need to detect if we're actually 
            // at the end of a field. This is
            // the case when the first non-whitespace character is the delimiter
            // or end of line
            if (_currentChar == _csvLayout.Delimiter)
            {
                // the second quote did mark the end of the field
                _tentative.Clear();
                AddField();
                _wasQuoted = false;
                _whereAmI = Location.OutsideField;
            }
            else if (_currentChar.IsNewLine())
            {
                // the second quote did mark the end of the field and we're at the end of the line
                _tentative.Clear();
                AddField();
                _wasQuoted = false;
                _whereAmI = Location.EndOfLine;
                _skipNextChar = true;
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                _field.Append(_tentative);
                _tentative.Clear();
                _tentative.Append(_currentChar);
            }
            else if (char.IsWhiteSpace(_currentChar))
            {
                // as long as we encounter white space, keep accumulating
                _tentative.Append(_currentChar);
            }
            else
            {
                // the second quote did NOT mark the end of the field, so we're still 'inside' the field
                // this means that the quote is now confirmed to have occurred inside the quoted field
                switch (_behaviour.QuotesInsideQuotedFieldAction)
                {
                    case QuotesInsideQuotedFieldAction.ThrowException:
                        throw new MalformedCsvException(_currentRawData.ToString(), _currentColumn, _lineNumber, _fields.Count);
                    case QuotesInsideQuotedFieldAction.AdvanceToNextLine:
                        _whereAmI = Location.ParseError;
                        break;
                    case QuotesInsideQuotedFieldAction.Ignore:
                        _field.Append(_tentative);
                        _tentative.Clear();
                        _field.Append(_currentChar);
                        _whereAmI = Location.InsideQuotedField;
                        break;
                }
            }
            yield break;
        }

        IEnumerable<CsvLine> ParseError()
        {
            // A parse error was detected. Ignore unless EOL.
            if (_currentChar.IsNewLine())
            {
                StartLine();
                _wasQuoted = false;
                _whereAmI = Location.BeginningOfLine;
            }
            yield break;
        }

        IEnumerable<CsvLine> Remaining()
        {
            // if last line does not end with a newline, we still need to yield it
            if (_whereAmI != Location.EndOfLine)
            {
                AddField();
                yield return CreateLine();
            }
        }

        void DbgLog(string message)
        {
            Debug.WriteLineIf(_debug, message);
        }

        char? Peek()
        {
            int peek = _textReader.Peek();
            return peek < 0 ? null : (char?)peek;
        }
    }
}
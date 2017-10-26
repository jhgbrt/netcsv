using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Code.Csv.Impl
{
    internal struct Location
    {
        public readonly int Line;
        public readonly int Column;

        private Location(int lineNumber, int columnNumber)
        {
            Line = lineNumber;
            Column = columnNumber;
        }

        public static Location Origin() => new Location(0, 0);
        public Location NextColumn() => new Location(Line, Column + 1);
        public Location NextLine() => new Location(Line + 1, 0);

        public override string ToString() => $"{Line},{Column}";
    }
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
            TransitionTo(BeginningOfLine);
        }

        bool _quoted = false;
        StringBuilder _field = new StringBuilder();
        StringBuilder _tentative = new StringBuilder();
        List<string> _fields = null;
        StringBuilder _currentRawData = new StringBuilder();
        char _currentChar = '\0';
        private Location _location = Location.Origin();

        bool _skipNextChar = false;
        Func<IEnumerable<CsvLine>> _state;

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
                foreach (var line in _state()) yield return line;
            }

            // if last line does not end with a newline, we still need to yield it
            if (_state != EndOfLine)
            {
                AddField();
                yield return CreateLine();
            }

        }
        bool ReadNextCharacter()
        {
            if (!_skipNextChar)
            {
                var i = _textReader.Read();
                if (i < 0) return false;
                _currentChar = (char)i;
                _location = _location.NextColumn();
                _currentRawData.Append(_currentChar);
                if (_currentRawData.Length > 32) _currentRawData.Remove(0, 1);
                DbgLog($"Current character: '{ToLiteral(_currentChar.ToString())}'. Location is {_state.Method.Name} ({_location}).");
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
            
            if (ShouldTrim)
                result = result.Trim();

            DbgLog($"Adding field: {ToLiteral(result)}");

            _fields.Add(result);
            _field.Clear();
        }

        private bool ShouldTrim
        {
            get
            {
                var trimmingOptions = _behaviour.TrimmingOptions;
                return (trimmingOptions == ValueTrimmingOptions.All)
                                || (_quoted && trimmingOptions == ValueTrimmingOptions.QuotedOnly)
                                || (!_quoted && trimmingOptions == ValueTrimmingOptions.UnquotedOnly);
            }
        }

        CsvLine CreateLine()
        {
            var fields = _fields;

            DbgLog($"Creating line at {_location.Line} ({fields.Count} fields)");

            bool isEmpty = fields.Count == 0 || (fields.Count == 1 && string.IsNullOrEmpty(fields[0]));

            if (!FieldCount.HasValue && (!isEmpty || !_behaviour.SkipEmptyLines))
                FieldCount = fields.Count;

            var count = fields.Count();

            if (!isEmpty && count < FieldCount)
            {
                if (_behaviour.MissingFieldAction == MissingFieldAction.ParseError)
                    throw new MissingFieldCsvException(_currentRawData.ToString(), _location.Column, _location.Line, fields.Count());
            }

            if (count < FieldCount)
            {
                string s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
                while (fields.Count < FieldCount) fields.Add(s);
            }

            var line = new CsvLine(fields, isEmpty);
            DbgLog("Yielding line: " + line);
            return line;
        }

        void StartLine()
        {
            _fields = new List<string>();
            _location = _location.NextLine();
            _field.Clear();
            _tentative.Clear();
            _quoted = false;
            DbgLog($"Start line {_location.Line}");
        }

        private void TransitionTo(Func<IEnumerable<CsvLine>> state, bool skipNext = false)
        {
            _state = state;
            _skipNextChar = skipNext;
        }

        IEnumerable<CsvLine> BeginningOfLine()
        {
            // begin of line can be newline, comment, quote or other 
            if (_currentChar.IsNewLine())
            {
                yield return CsvLine.Empty;
                StartLine();
            }
            else if (_currentChar == _csvLayout.Comment)
            {
                TransitionTo(Comment);
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                _quoted = true;
                TransitionTo(InsideQuotedField);
            }
            else
            {
                TransitionTo(InsideField, true);
            }
        }

        IEnumerable<CsvLine> Comment()
        {
            // If we're processing a comment, there is nothing to do. 
            // When we encounter a newline character we simply start a new line.
            if (_currentChar.IsNewLine())
            {
                StartLine();
                TransitionTo(BeginningOfLine);
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
                TransitionTo(OutsideField);
            }
            else if (_currentChar.IsNewLine())
            {
                // end of field because newline
                AddField();
                TransitionTo(EndOfLine, true);
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
                // Found newline. Accumulated whitespace belongs to last field.
                _field.Append(_tentative);
                _tentative.Clear();
                AddField();
                // transition to end of line
                TransitionTo(EndOfLine, true);
            }
            else if (char.IsWhiteSpace(_currentChar))
            {
                // Found whitespace, accumulate until we find a non-whitespace character
                _tentative.Append(_currentChar);
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                // There is another field, and it's quoted
                // Accumulated white space should be ignored, and we are now in a quoted field.
                _quoted = true;
                _tentative.Clear();
                TransitionTo(InsideQuotedField);
            }
            else
            {
                // Found a 'normal' (non-newline, non-whitespace, non-quote) character.
                // There is another field and it's not quoted.
                // Accumulated whitespace should be added to this current field
                _field.Append(_tentative);
                _tentative.Clear();
                TransitionTo(InsideField, true);
            }
            yield break;
        }

        IEnumerable<CsvLine> EndOfLine()
        {
            yield return CreateLine();
            StartLine();
            TransitionTo(BeginningOfLine);
        }

        IEnumerable<CsvLine> Escaped()
        {
            _field.Append(_currentChar);
            TransitionTo(InsideQuotedField);
            yield break;
        }

        IEnumerable<CsvLine> InsideQuotedField()
        {
            if (_csvLayout.IsEscape(_currentChar, Peek()))
            {
                TransitionTo(Escaped);
                // skip the escape character
            }
            else if (_currentChar == _csvLayout.Quote)
            {
                // there are 2 possibilities: 
                // - either the quote is just part of the field
                //   (e.g. "foo,"bar "baz"", foobar")
                // - or the quote is actually the end of this field
                // => start capturing after the quote; check for delimiter 
                TransitionTo(AfterSecondQuote);
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
                _quoted = false;
                TransitionTo(OutsideField);
            }
            else if (_currentChar.IsNewLine())
            {
                // the second quote did mark the end of the field and we're at the end of the line
                _tentative.Clear();
                AddField();
                _quoted = false;
                TransitionTo(EndOfLine, true);
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
                        throw new MalformedCsvException(_currentRawData.ToString(), _location.Column, _location.Line, _fields.Count);
                    case QuotesInsideQuotedFieldAction.AdvanceToNextLine:
                        TransitionTo(ParseError);
                        break;
                    case QuotesInsideQuotedFieldAction.Ignore:
                        _field.Append(_tentative);
                        _tentative.Clear();
                        _field.Append(_currentChar);
                        TransitionTo(InsideQuotedField);
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
                TransitionTo(BeginningOfLine);
            }
            yield break;
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
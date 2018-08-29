using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Code.Csv.Impl
{
    internal class CsvStateMachine
    {
        private readonly TextReader _textReader;
        private readonly CsvLayout _csvLayout;
        private readonly CsvBehaviour _behaviour;

        private static void Log(string text)
        {
            //Console.WriteLine(text);
        }

        public CsvStateMachine(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour)
        {
            _textReader = textReader;
            _csvLayout = csvLayout;
            _behaviour = behaviour;
            TransitionTo(BeginningOfLine);
        }

        private bool _quoted;
        private bool _skipNextChar;
        private readonly StringBuilder _field = new StringBuilder();
        private readonly StringBuilder _tentative = new StringBuilder();
        private readonly StringBuilder _rawData = new StringBuilder();
        private List<string> _fields;
        private char _currentChar = '\0';
        private Location _location = Location.Origin();
        private Func<IEnumerable<CsvLine>> CurrentState;

        public int? FieldCount { get; private set; }

        public IEnumerable<CsvLine> Lines() => LinesImpl().Where(line => !line.IsEmpty || !_behaviour.SkipEmptyLines);

        private IEnumerable<CsvLine> LinesImpl()
        {

            StartLine();

            while (ReadNextCharacter())
            {
                foreach (var line in CurrentState())
                {
                    yield return line;
                }
            }

            // if last line does not end with a newline, we still need to yield it
            if (CurrentState != EndOfLine)
            {
                AddField();
                yield return CreateLine();
            }

        }

        private bool ReadNextCharacter()
        {
            if (!_skipNextChar)
            {
                var i = _textReader.Read();
                if (i < 0)
                {
                    Log("ReadNextCharacter - no result");
                    return false;
                }
                _currentChar = (char)i;
                _location = _location.NextColumn();
                _rawData.Append(_currentChar);
                if (_rawData.Length > 32)
                {
                    _rawData.Remove(0, 1);
                }

                Log($"ReadNextCharacter: {_currentChar}");
            }
            else
            {
                _skipNextChar = false;
                Log("ReadNextCharacter skipped");
            }
            return true;
        }

        private void AddField()
        {
            var result = _field.ToString();

            if (ShouldTrim)
            {
                result = result.Trim();
            }

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

        private CsvLine CreateLine()
        {
            var fields = _fields;

            var isEmpty = fields.Count == 0 || (fields.Count == 1 && string.IsNullOrEmpty(fields[0]));

            if (!FieldCount.HasValue && (!isEmpty || !_behaviour.SkipEmptyLines))
            {
                FieldCount = fields.Count;
            }

            var count = fields.Count;

            if (!isEmpty && count < FieldCount)
            {
                if (_behaviour.MissingFieldAction == MissingFieldAction.ParseError)
                {
                    throw new MissingFieldCsvException(_rawData.ToString(), _location, fields.Count);
                }
            }

            if (count < FieldCount)
            {
                var s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
                while (fields.Count < FieldCount)
                {
                    fields.Add(s);
                }
            }

            var line = new CsvLine(fields, isEmpty);
            return line;
        }

        private void StartLine()
        {
            _fields = new List<string>();
            _location = _location.NextLine();
            _field.Clear();
            _tentative.Clear();
            _quoted = false;
            _skipNextChar = false;
        }

        private void TransitionTo(Func<IEnumerable<CsvLine>> state, bool skipNext = false)
        {
            Log($"TransitionTo state = {state.Method.Name}, skipNext = {skipNext}");
            CurrentState = state;
            _skipNextChar = skipNext;
        }

        private IEnumerable<CsvLine> BeginningOfLine()
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

        private IEnumerable<CsvLine> Comment()
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

        private IEnumerable<CsvLine> InsideField()
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

        private IEnumerable<CsvLine> OutsideField()
        {
            // Outside field (after a delimiter): find out whether next field is quoted
            // white space may have to be added to the next field (if it is not quoted 
            // and we don't want to trim
            // if the next field is quoted, whitespace has to be skipped in any case
            if (_currentChar == _csvLayout.Delimiter)
            {
                // Found the next delimiter. We found an empty field.
                // Accumulated whitespace should be added to this current field
                _field.Append(_tentative);
                _tentative.Clear();
                AddField();
            }
            else if (_currentChar.IsNewLine())
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

        private IEnumerable<CsvLine> EndOfLine()
        {
            yield return CreateLine();
            StartLine();
            TransitionTo(BeginningOfLine);
        }

        private IEnumerable<CsvLine> Escaped()
        {
            _field.Append(_currentChar);
            TransitionTo(InsideQuotedField);
            yield break;
        }

        private IEnumerable<CsvLine> InsideQuotedField()
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

        private IEnumerable<CsvLine> AfterSecondQuote()
        {
            // after second quote, we need to detect if we're actually at the end of a field. This is
            // the case when the first non-whitespace character is the delimiter or end of line
            if (_currentChar == _csvLayout.Delimiter)
            {
                // we encountered a field delimiter after the second quote, so we're actually at the end of the field
                // any accumulated whitespace should be ignored, and a new field starts
                _tentative.Clear();
                AddField();
                _quoted = false;
                TransitionTo(OutsideField);
            }
            else if (_currentChar.IsNewLine())
            {
                // we encountered a newline after the second quote, so we're actually at the end of the field
                // we will transition to EndOfLine
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
                // we found a 'normal' (non-whitespace, non-delimiter, non-newline) character after 
                // the second quote, therefore the second quote did NOT mark the end of the field.
                // this means that the quote is now confirmed to have occurred inside the quoted field
                switch (_behaviour.QuotesInsideQuotedFieldAction)
                {
                    case QuotesInsideQuotedFieldAction.ThrowException:
                        throw new MalformedCsvException(_rawData.ToString(), _location, _fields.Count);
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

        private IEnumerable<CsvLine> ParseError()
        {
            // A parse error was detected. Ignore unless EOL.
            if (_currentChar.IsNewLine())
            {
                StartLine();
                TransitionTo(BeginningOfLine);
            }
            yield break;
        }

        private char? Peek()
        {
            var peek = _textReader.Peek();
            return peek < 0 ? null : (char?)peek;
        }
    }
}
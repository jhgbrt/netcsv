using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Code.Csv.Impl
{
    struct MyChar
    {
        public readonly char Value;
        private readonly char? _next;
        private readonly CsvLayout _layout;

        public MyChar(char c, CsvLayout layout, char? next)
        {
            Value = c;
            _layout = layout;
            _next = next;
        }

        public bool IsNewLine => Value == '\r' || Value == '\n';
        public bool IsComment => Value == _layout.Comment;
        public bool IsQuote => Value == _layout.Quote;
        public bool IsDelimiter => Value == _layout.Delimiter;
        public bool IsWhiteSpace => char.IsWhiteSpace(Value);
        public bool IsEscape => _layout.IsEscape(Value, _next);
    }

    static class Option
    {
        public static Option<T> Some<T>(T value) where T : class => new Option<T>(value);
    }
    struct Option<T> where T : class
    {
        public T Value { get; set; }
        public bool HasValue => Value is not null;
        internal Option(T value)
        {
            Value = value;
        }
    }

    internal class CsvState
    {
        private char _currentChar;
        private char? _next;
        private bool _quoted;
        private bool _unprocessed;
        private Location _location = Location.Origin().NextLine();

        readonly StringBuilder _field = new StringBuilder();
        readonly StringBuilder _tentative = new StringBuilder();
        readonly StringBuilder _rawData = new StringBuilder();
        private List<string> _fields = new List<string>();

        private readonly CsvLayout _layout;
        private readonly CsvBehaviour _behaviour;
        public string RawData => _rawData.ToString();

        public MyChar CurrentChar => new MyChar(_currentChar, _layout, _next);

        public List<string> Fields => _fields;

        public Location Location => _location;

        public int? FieldCount { get; set; }
        public CsvState MarkUnprocessed()
        {
            _unprocessed = true;
            return this;
        }

        public CsvState(CsvLayout layout, CsvBehaviour behaviour)
        {
            _layout = layout;
            _behaviour = behaviour;
        }

        internal CsvState ClearForNextLine()
        {
            _fields.Clear();
            _field.Clear();
            _tentative.Clear();
            _quoted = false;
            _location = _location.NextLine();
            _unprocessed = false;
            return this;
        }

        internal CsvState AppendCharToField()
        {
            _field.Append(_currentChar);
            return this;
        }

        internal CsvState ConsolidateTentative()
        {
            _field.Append(_tentative);
            _tentative.Clear();
            return this;
        }

        internal CsvState AsTentative() { _tentative.Append(_currentChar); return this; }

        internal CsvState DiscardTentative() { _tentative.Clear(); return this; }

        internal CsvState CurrentFieldIsComplete()
        {
            var result = _field.ToString();
            if (_behaviour.ShouldTrim(_quoted))
            {
                result = result.Trim();
            }
            _field.Clear();
            _fields.Add(result);
            _quoted = false;
            return this;
        }

        internal CsvState MarkQuoted() { _quoted = true; return this;}

        internal CsvLine YieldLine()
        {
            var isEmpty = _fields.Count == 0 || (_fields.Count == 1 && string.IsNullOrEmpty(_fields[0]));

            if (!FieldCount.HasValue && (!isEmpty || !_behaviour.SkipEmptyLines))
            {
                FieldCount = _fields.Count;
            }

            var count = _fields.Count;

            if (!isEmpty && count < FieldCount)
            {
                if (_behaviour.MissingFieldAction == MissingFieldAction.ParseError)
                {
                    throw new MissingFieldCsvException(RawData, Location, _fields.Count);
                }
            }

            if (count < FieldCount)
            {
                var s = _behaviour.MissingFieldAction == MissingFieldAction.ReplaceByNull ? null : "";
                while (_fields.Count < FieldCount)
                {
                    _fields.Add(s);
                }
            }

            var line = new CsvLine(_fields.ToArray(), isEmpty);
            ClearForNextLine();
            return line;
        }

        internal bool ReadNext(TextReader textReader) 
        {
            if (!_unprocessed) // previous value not yet processed
            {
                var i = textReader.Read();
                if (i < 0)
                {
                    return false;
                }
                var currentChar = (char)i;
                _next = Peek(textReader);
                _location = _location.NextColumn();
                _currentChar = currentChar;
                _rawData.Append(currentChar);
                if (_rawData.Length > 32)
                {
                    _rawData.Remove(0, 1);
                }
            }
            else
            {
                _unprocessed = false;
            }
            return true;
            
            char? Peek(TextReader textReader)
            {
                var peek = textReader.Peek();
                return peek < 0 ? null : (char?)peek;
            }
        }
    }

    internal class CsvStateMachine
    {
        private readonly TextReader _textReader;
        private readonly CsvLayout _csvLayout;
        private readonly CsvBehaviour _behaviour;
        static readonly Option<CsvLine> NoLine = new Option<CsvLine>();

        public CsvStateMachine(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour)
        {
            _textReader = textReader;
            _csvLayout = csvLayout;
            _behaviour = behaviour;
        }

        public int? FieldCount { get; set; }

        public static Action<string> Log = s => { Console.WriteLine(s); };

        public IEnumerable<CsvLine> Lines() => LinesImpl().Where(line => !line.IsEmpty || !_behaviour.SkipEmptyLines);
        private IEnumerable<CsvLine> LinesImpl()
        {
            Func<CsvState, CsvBehaviour, ProcessingResult> CurrentFunction = BeginningOfLine;
            var state = new CsvState(_csvLayout, _behaviour);
            while (state.ReadNext(_textReader))
            {
                var result = CurrentFunction(state, _behaviour);
                var line = result.Line;
                if (line.HasValue)
                {
                    FieldCount = state.FieldCount;
                    yield return line.Value;
                }
                CurrentFunction = result.Next;
                state = result.State;
            }

            // if last line does not end with a newline, we still need to yield it
            if (CurrentFunction != EndOfLine)
            {
                var line = state.CurrentFieldIsComplete().YieldLine();
                FieldCount = state.FieldCount;
                yield return line;
            }

        }

        record ProcessingResult(Option<CsvLine> Line, Func<CsvState, CsvBehaviour, ProcessingResult> Next, CsvState State)
        {
            public ProcessingResult(Func<CsvState, CsvBehaviour, ProcessingResult> Next, CsvState state) : this(NoLine, Next, state) { }
            public ProcessingResult(CsvLine line, Func<CsvState, CsvBehaviour, ProcessingResult> Next, CsvState state) : this(Option.Some(line), Next, state) { }
        }

        // begin of line can be newline, comment, quote or other 
        private static ProcessingResult BeginningOfLine(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsNewLine: true }
                => new(CsvLine.Empty, BeginningOfLine, state.ClearForNextLine()),
            { IsComment: true }
                => new(Comment, state),
            { IsQuote: true }
                => new(InsideQuotedField, state.MarkQuoted()),
            _
                => new(InsideField, state.MarkUnprocessed())
        };

        // If we're processing a comment, there is nothing to do. 
        // When we encounter a newline character we simply start a new line.
        private static ProcessingResult Comment(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsNewLine: true }
                => new(BeginningOfLine, state.ClearForNextLine()),
            _ 
                => new(Comment, state)
        };

        // Inside a non-quoted field, we can encounter either a delimiter
        // or a new line. Otherwise, we accumulate the character for the current field.
        private static ProcessingResult InsideField(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsDelimiter: true }
                // end of field because delimiter
                => new(OutsideField, state.CurrentFieldIsComplete()),
            { IsNewLine: true }
                // end of field because newline
                => new(EndOfLine, state.CurrentFieldIsComplete().MarkUnprocessed()),
            _
                // keep going
                => new(InsideField, state.AppendCharToField())
        };

        // Outside field (after a delimiter): find out whether next field is quoted
        // white space may have to be added to the next field (if it is not quoted 
        // and we don't want to trim)
        // if the next field is quoted, whitespace has to be skipped in any case
        private static ProcessingResult OutsideField(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsNewLine: true }
                // Found newline. Accumulated whitespace belongs to last field.
                // transition to end of line
                => new(EndOfLine, state.ConsolidateTentative().CurrentFieldIsComplete().MarkUnprocessed()),
            { IsQuote: true }
                // There is another field, and it's quoted
                // Accumulated white space should be ignored, and we are now in a quoted field.
                => new(InsideQuotedField, state.MarkQuoted().DiscardTentative()),
            { IsDelimiter: true }
                // Found the next delimiter. We found an empty field.
                // Accumulated whitespace should be added to this current field
                => new(OutsideField, state.ConsolidateTentative().CurrentFieldIsComplete()),
            { IsWhiteSpace: true }
                // Found whitespace, accumulate until we find a non-whitespace character
                => new(OutsideField, state.AsTentative()),
            _
                // Found a 'normal' (non-newline, non-whitespace, non-quote) character.
                // There is another field and it's not quoted.
                // Accumulated whitespace should be added to this current field
                => new(InsideField, state.ConsolidateTentative().MarkUnprocessed())
        };

        private static ProcessingResult EndOfLine(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            _ => new(state.YieldLine(), BeginningOfLine, state)
        };

        private static ProcessingResult Escaped(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            _ => new(InsideQuotedField, state.AppendCharToField())
        };

        private static ProcessingResult InsideQuotedField(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsEscape: true }
                => new(Escaped, state),
            { IsQuote: true }
                // there are 2 possibilities: 
                // - either the quote is just part of the field
                //   (e.g. "foo,"bar "baz"", foobar")
                // - or the quote is actually the end of this field
                // => start capturing after the quote; check for delimiter 
                => new(AfterSecondQuote, state.DiscardTentative().AsTentative()),
            _
                => new(InsideQuotedField, state.AppendCharToField())
        };

        // after second quote, we need to detect if we're actually at the end of a field. This is
        // the case when the first non-whitespace character is the delimiter or end of line
        private static ProcessingResult AfterSecondQuote(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsDelimiter: true }
                // we encountered a field delimiter after the second quote, so we're actually at the end of the field
                // any accumulated whitespace should be ignored, and a new field starts
                => new(OutsideField, state.DiscardTentative().CurrentFieldIsComplete()),
            { IsNewLine: true }
                // we encountered a newline after the second quote, so we're actually at the end of the field
                // we will transition to EndOfLine
                // the second quote did mark the end of the field and we're at the end of the line
                => new(EndOfLine, state.DiscardTentative().CurrentFieldIsComplete().MarkUnprocessed()),
            { IsQuote: true }
                => new(AfterSecondQuote, state.ConsolidateTentative().AsTentative()),
            { IsWhiteSpace: true }
                // as long as we encounter white space, keep accumulating
                => new(AfterSecondQuote, state.AsTentative()),
            _
                // we found a 'normal' (non-whitespace, non-delimiter, non-newline) character after 
                // the second quote, therefore the second quote did NOT mark the end of the field.
                // this means that the quote is now confirmed to have occurred inside the quoted field
                => behaviour.QuotesInsideQuotedFieldAction switch
                {
                    QuotesInsideQuotedFieldAction.ThrowException => throw new MalformedCsvException(state.RawData, state.Location, state.Fields.Count),
                    QuotesInsideQuotedFieldAction.AdvanceToNextLine => new(ParseError, state),
                    QuotesInsideQuotedFieldAction.Ignore => new(InsideQuotedField, state.ConsolidateTentative().AppendCharToField())
                }
        };

        // A parse error was detected. Ignore unless EOL.
        private static ProcessingResult ParseError(CsvState state, CsvBehaviour behaviour) => state.CurrentChar switch
        {
            { IsNewLine: true }
                => new(BeginningOfLine, state.ClearForNextLine()),
            _
                => new(ParseError, state)
        };
    }
}
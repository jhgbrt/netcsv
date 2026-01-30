namespace Net.Code.Csv.Impl.V2
{
    /// <summary>
    /// CsvStateMachineV2 is the streaming CSV parser that turns raw text into CsvLine records.
    /// It uses a state machine to handle quoted fields, escapes, comments, delimiters, whitespace,
    /// and line endings while keeping allocations low.
    ///
    /// Key Responsibilities:
    /// - Iteratively processes buffered spans and transitions between parsing states.
    /// - Builds field slices via CsvLineSliceBuilder, keeping zero-copy slices when possible.
    /// - Applies CsvLayout and CsvBehaviour rules for delimiters, quotes, and error handling.
    ///
    /// Usage:
    /// This is an internal component. Consumers should use ReadCsv APIs rather than interacting
    /// with this class directly.
    ///
    /// Implementation Details:
    /// - Uses span scanning (IndexOf/IndexOfAny) to skip runs of ordinary characters.
    /// - Mirrors V1 parsing semantics but executes in a tight span-based loop.
    /// - Pooled buffers are returned once the next line is emitted.
    ///
    /// Note:
    /// Changes here can subtly alter CSV semantics; keep behavior aligned with V1.
    /// </summary>
    internal sealed class CsvStateMachineV2(BufferedCharReader reader, CsvLayout csvLayout, CsvBehaviour behaviour)
    {
        private readonly BufferedCharReader _reader = reader;
        private readonly CsvLayout _layout = csvLayout;
        private readonly CsvBehaviour _behaviour = behaviour;

        public CsvStateMachineV2(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour)
            : this(new BufferedCharReader(textReader ?? throw new ArgumentNullException(nameof(textReader))), csvLayout, behaviour)
        {
        }

        public int? FieldCount { get; set; }

        public IEnumerable<CsvLineSlice> Lines() => new LineEnumerable(this);

        private sealed class LineEnumerable(CsvStateMachineV2 owner) : IEnumerable<CsvLineSlice>
        {
            public IEnumerator<CsvLineSlice> GetEnumerator() => new LineEnumerator(owner);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class LineEnumerator(CsvStateMachineV2 owner) : IEnumerator<CsvLineSlice>
        {
            private readonly BufferedCharReader _reader = owner._reader;
            private readonly CsvLayout _layout = owner._layout;
            private readonly CsvBehaviour _behaviour = owner._behaviour;
            private readonly CsvLineSliceBuilder _builder = new(owner._layout, owner._behaviour);
            private readonly bool _strictQuotedWhitespace =
                owner._behaviour.StrictMode
                && owner._behaviour.TrimmingOptions == ValueTrimmingOptions.All
                && owner._layout.Quote.HasValue;

            private ScanState _state = ScanState.BeginningOfLine;
            private CsvLineSlice _current;
            private bool _finished;
            private bool _returnedCurrent;

            public CsvLineSlice Current => _current;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_finished)
                {
                    return false;
                }

                if (!_returnedCurrent)
                {
                    // Return the previous line's pooled buffer once we advance.
                    _current.ReturnToPool();
                    _returnedCurrent = true;
                }

                while (true)
                {
                    if (!_reader.TryGetSpan(out var span))
                    {
                        // End of input: finalize the current line.
                        var finalLine = _builder.NextField().ToLine();
                        owner.FieldCount = _builder.FieldCount;
                        _finished = true;
                        if (_behaviour.EmptyLineAction == EmptyLineAction.Skip && finalLine.IsEmpty)
                        {
                            // Empty lines don't escape; return their pooled storage immediately.
                            finalLine.ReturnToPool();
                            return false;
                        }
                        _current = finalLine;
                        _returnedCurrent = false;
                        return true;
                    }

                    // Keep a stable identity for the current read buffer so
                    // direct field slices can reference it without copying.
                    var buffer = _reader.CurrentBuffer;
                    var baseIndex = _reader.CurrentIndex;
                    var i = 0;
                    while (i < span.Length)
                    {
                        switch (_state)
                        {
                            case ScanState.BeginningOfLine:
                                {
                                    // Beginning of line: decide what kind of line/field we are entering.
                                    // - newline => empty line (emit)
                                    // - comment => ignore until newline
                                    // - quote => start quoted field
                                    // - delimiter => empty field
                                    // - other => start unquoted field
                                    var c = span[i];
                                    if (_strictQuotedWhitespace && char.IsWhiteSpace(c) && c != '\r' && c != '\n')
                                    {
                                        _builder.SetCurrent(c);
                                        _builder.AddToTentative();
                                        _state = ScanState.OutsideField;
                                        i++;
                                        break;
                                    }
                                    if (c == '\r')
                                    {
                                        _builder.SetCurrent(c);
                                        i++;
                                        break;
                                    }
                                    if (c == '\n')
                                    {
                                        _builder.SetCurrent(c);
                                        var line = _builder.ToLine();
                                        owner.FieldCount = _builder.FieldCount;
                                        if (ShouldReturn(line, i + 1))
                                        {
                                            return true;
                                        }
                                        i++;
                                        break;
                                    }
                                    if (_layout.Comment.HasValue && c == _layout.Comment.Value)
                                    {
                                        _builder.SetCurrent(c);
                                        _state = ScanState.InComment;
                                        i++;
                                        break;
                                    }
                                    if (_layout.Quote.HasValue && c == _layout.Quote.Value)
                                    {
                                        _builder.SetCurrent(c);
                                        _builder.MarkQuoted();
                                        _state = ScanState.InsideQuotedField;
                                        i++;
                                        break;
                                    }
                                    if (c == _layout.Delimiter)
                                    {
                                        _builder.SetCurrent(c);
                                        _builder.NextField();
                                        _state = ScanState.OutsideField;
                                        i++;
                                        break;
                                    }

                                    _state = ScanState.InsideField;
                                    continue;
                                }

                            case ScanState.InComment:
                                {
                                    // Comment line: ignore everything until newline ends the comment.
                                    // Carriage returns are ignored to support CRLF.
                                    var c = span[i];
                                    if (c == '\r')
                                    {
                                        i++;
                                        break;
                                    }
                                    if (c == '\n')
                                    {
                                        _builder.PrepareNextLine();
                                        _state = ScanState.BeginningOfLine;
                                        i++;
                                        break;
                                    }
                                    i++;
                                    break;
                                }

                            case ScanState.InsideField:
                                {
                                    // Inside an unquoted field: scan ahead to delimiter or newline.
                                    // Use direct buffer slices when possible to avoid copying.
                                    var slice = span[i..];
                                    var idx = slice.IndexOfAny(_layout.Delimiter, '\n', '\r');
                                    if (idx < 0)
                                    {
                                        if (!_builder.TrySetDirectSlice(buffer, baseIndex + i, slice.Length))
                                        {
                                            _builder.AddToField(slice);
                                        }
                                        _builder.AdvanceSpan(slice);
                                        i = span.Length;
                                        break;
                                    }
                                    if (idx > 0)
                                    {
                                        var run = slice[..idx];
                                        if (!_builder.TrySetDirectSlice(buffer, baseIndex + i, idx))
                                        {
                                            _builder.AddToField(run);
                                        }
                                        _builder.AdvanceSpan(run);
                                        i += idx;
                                    }

                                    var c = span[i];
                                    _builder.SetCurrent(c);
                                    if (c == '\r')
                                    {
                                        i++;
                                        break;
                                    }
                                    if (c == _layout.Delimiter)
                                    {
                                        _builder.NextField();
                                        _state = ScanState.OutsideField;
                                        i++;
                                        break;
                                    }
                                    if (c == '\n')
                                    {
                                        _builder.NextField();
                                        var line = _builder.ToLine();
                                        owner.FieldCount = _builder.FieldCount;
                                        _state = ScanState.BeginningOfLine;
                                        if (ShouldReturn(line, i + 1))
                                        {
                                            return true;
                                        }
                                        i++;
                                        break;
                                    }

                                    i++;
                                    break;
                                }

                            case ScanState.OutsideField:
                                {
                                    // After a delimiter: decide if the next field is quoted, empty, or unquoted.
                                    // Whitespace is tentative and only committed for unquoted fields.
                                    var c = span[i];
                                    if (c == '\r')
                                    {
                                        _builder.SetCurrent(c);
                                        i++;
                                        break;
                                    }
                                    if (c == '\n')
                                    {
                                        _builder.SetCurrent(c);
                                        _builder.AcceptTentative().NextField();
                                        var line = _builder.ToLine();
                                        owner.FieldCount = _builder.FieldCount;
                                        _state = ScanState.BeginningOfLine;
                                        if (ShouldReturn(line, i + 1))
                                        {
                                            return true;
                                        }
                                        i++;
                                        break;
                                    }
                                    if (_layout.Quote.HasValue && c == _layout.Quote.Value)
                                    {
                                        if (_strictQuotedWhitespace && _builder.HasTentative)
                                        {
                                            throw new MalformedCsvException(_builder.RawData, _builder.Location, _builder.FieldsCount);
                                        }
                                        _builder.SetCurrent(c);
                                        _builder.MarkQuoted().DiscardTentative();
                                        _state = ScanState.InsideQuotedField;
                                        i++;
                                        break;
                                    }
                                    if (c == _layout.Delimiter)
                                    {
                                        _builder.SetCurrent(c);
                                        _builder.AcceptTentative().NextField();
                                        _state = ScanState.OutsideField;
                                        i++;
                                        break;
                                    }
                                    if (char.IsWhiteSpace(c))
                                    {
                                        _builder.SetCurrent(c);
                                        _builder.AddToTentative();
                                        i++;
                                        break;
                                    }

                                    _builder.AcceptTentative();
                                    _state = ScanState.InsideField;
                                    continue;
                                }

                            case ScanState.Escaped:
                                {
                                    // Escape sequence inside a quoted field: take the next char verbatim
                                    // and return to InsideQuotedField.
                                    var c = span[i];
                                    _builder.SetCurrent(c);
                                    _builder.AddToField();
                                    _state = ScanState.InsideQuotedField;
                                    i++;
                                    break;
                                }

                            case ScanState.InsideQuotedField:
                                {
                                    // Inside quoted field: scan to next quote or escape marker.
                                    // Everything else is literal data.
                                    if (!_layout.Quote.HasValue)
                                    {
                                        var remaining = span[i..];
                                        _builder.AddToField(remaining);
                                        _builder.AdvanceSpan(remaining);
                                        i = span.Length;
                                        break;
                                    }
                                    var slice = span[i..];
                                    var quote = _layout.Quote.Value;
                                    var idx = _layout.Escape == quote
                                        ? slice.IndexOf(quote)
                                        : slice.IndexOfAny(quote, _layout.Escape);
                                    if (idx < 0)
                                    {
                                        _builder.AddToField(slice);
                                        _builder.AdvanceSpan(slice);
                                        i = span.Length;
                                        break;
                                    }
                                    if (idx > 0)
                                    {
                                        var run = slice[..idx];
                                        _builder.AddToField(run);
                                        _builder.AdvanceSpan(run);
                                        i += idx;
                                    }

                                    var c = span[i];
                                    var next = i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1);
                                    _builder.SetCurrent(c, next);
                                    if (_layout.IsEscape(c, next))
                                    {
                                        _state = ScanState.Escaped;
                                        i++;
                                        break;
                                    }
                                    if (_layout.Quote.HasValue && c == _layout.Quote.Value)
                                    {
                                        _builder.DiscardTentative().AddToTentative();
                                        _state = ScanState.AfterSecondQuote;
                                        i++;
                                        break;
                                    }

                                    _builder.AddToField();
                                    i++;
                                    break;
                                }

                            case ScanState.AfterSecondQuote:
                                {
                                    // After a quote in a quoted field: decide whether it ends the field.
                                    // Delimiter/newline => end field/line; quote => literal quote;
                                    // whitespace stays tentative; other => quote-in-field or error.
                                    var c = span[i];
                                    _builder.SetCurrent(c);
                                    if (c == _layout.Delimiter)
                                    {
                                        _builder.DiscardTentative().NextField();
                                        _state = ScanState.OutsideField;
                                        i++;
                                        break;
                                    }
                                    if (c == '\n')
                                    {
                                        _builder.DiscardTentative().NextField();
                                        var line = _builder.ToLine();
                                        owner.FieldCount = _builder.FieldCount;
                                        _state = ScanState.BeginningOfLine;
                                        if (ShouldReturn(line, i + 1))
                                        {
                                            return true;
                                        }
                                        i++;
                                        break;
                                    }
                                    if (_layout.Quote.HasValue && c == _layout.Quote.Value)
                                    {
                                        _builder.AcceptTentative().AddToTentative();
                                        i++;
                                        break;
                                    }
                                    if (c == '\r')
                                    {
                                        _builder.AddToTentative();
                                        i++;
                                        break;
                                    }
                                    if (char.IsWhiteSpace(c))
                                    {
                                        if (_strictQuotedWhitespace)
                                        {
                                            throw new MalformedCsvException(_builder.RawData, _builder.Location, _builder.FieldsCount);
                                        }
                                        _builder.AddToTentative();
                                        i++;
                                        break;
                                    }

                                    if (_behaviour.QuotesInsideQuotedFieldAction == QuotesInsideQuotedFieldAction.AdvanceToNextLine)
                                    {
                                        _state = ScanState.ParseError;
                                        i++;
                                        break;
                                    }
                                    if (_behaviour.QuotesInsideQuotedFieldAction == QuotesInsideQuotedFieldAction.Ignore)
                                    {
                                        _builder.AcceptTentative().AddToField();
                                        _state = ScanState.InsideQuotedField;
                                        i++;
                                        break;
                                    }
                                    throw new MalformedCsvException(_builder.RawData, _builder.Location, _builder.FieldsCount);
                                }

                            case ScanState.ParseError:
                                {
                                    // Parse error: ignore until newline, then recover at beginning of line.
                                    var c = span[i];
                                    _builder.SetCurrent(c);
                                    if (c == '\n')
                                    {
                                        _builder.PrepareNextLine();
                                        _state = ScanState.BeginningOfLine;
                                        i++;
                                        break;
                                    }
                                    i++;
                                    break;
                                }
                        }
                    }

                    // The reader will reuse its buffer after advancing, so
                    // copy any fields still pointing at the current buffer.
                    _builder.MaterializeDirectSlices(buffer);
                    _reader.Advance(span.Length);
                }
            }

            private bool ShouldReturn(CsvLineSlice line, int consumed)
            {
                if (_behaviour.EmptyLineAction != EmptyLineAction.Skip || !line.IsEmpty)
                {
                    _current = line;
                    _returnedCurrent = false;
                    _reader.Advance(consumed);
                    return true;
                }
                line.ReturnToPool();
                return false;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
                if (!_returnedCurrent)
                {
                    _current.ReturnToPool();
                    _returnedCurrent = true;
                }
            }
        }
    }
}

internal enum ScanState
{
    BeginningOfLine,
    InComment,
    InsideField,
    OutsideField,
    Escaped,
    InsideQuotedField,
    AfterSecondQuote,
    ParseError
}

/*
State transitions (simplified, mirrors V1 behavior)

BeginningOfLine
  '\n'         -> BeginningOfLine (emit line)
  comment      -> InComment
  quote        -> InsideQuotedField
  delimiter    -> OutsideField (empty field)
  other        -> InsideField

InComment
  '\n'         -> BeginningOfLine
  other        -> InComment

InsideField (unquoted)
  delimiter    -> OutsideField
  '\n'         -> BeginningOfLine (emit line)
  other        -> InsideField

OutsideField (after delimiter)
  '\n'         -> BeginningOfLine (emit line)
  quote        -> InsideQuotedField
  delimiter    -> OutsideField (empty field)
  whitespace   -> OutsideField (tentative)
  other        -> InsideField

InsideQuotedField
  escape       -> Escaped
  quote        -> AfterSecondQuote
  other        -> InsideQuotedField

Escaped
  any          -> InsideQuotedField (take next char verbatim)

AfterSecondQuote
  delimiter    -> OutsideField
  '\n'         -> BeginningOfLine (emit line)
  quote        -> AfterSecondQuote (double-quote inside quoted field)
  whitespace   -> AfterSecondQuote (tentative)
  other        -> InsideQuotedField or ParseError (behavior dependent)

ParseError
  '\n'         -> BeginningOfLine
  other        -> ParseError

EOF: finalize current field and emit final line.
*/

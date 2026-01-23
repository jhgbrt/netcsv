namespace Net.Code.Csv.Impl.V2
{
    internal sealed class CsvStateMachineV2
{
    private readonly BufferedCharReader _reader;
    private readonly CsvLayout _layout;
    private readonly CsvBehaviour _behaviour;

    public CsvStateMachineV2(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour)
        : this(new BufferedCharReader(textReader ?? throw new ArgumentNullException(nameof(textReader))), csvLayout, behaviour)
    {
    }

    public CsvStateMachineV2(BufferedCharReader reader, CsvLayout csvLayout, CsvBehaviour behaviour)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _layout = csvLayout ?? throw new ArgumentNullException(nameof(csvLayout));
        _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
    }

    public int? FieldCount { get; set; }

    public IEnumerable<CsvLineSlice> Lines() => new LineEnumerable(this);

    private sealed class LineEnumerable : IEnumerable<CsvLineSlice>
    {
        private readonly CsvStateMachineV2 _owner;

        public LineEnumerable(CsvStateMachineV2 owner)
        {
            _owner = owner;
        }

        public IEnumerator<CsvLineSlice> GetEnumerator() => new LineEnumerator(_owner);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class LineEnumerator : IEnumerator<CsvLineSlice>
    {
        private readonly BufferedCharReader _reader;
        private readonly CsvLayout _layout;
        private readonly CsvBehaviour _behaviour;
        private readonly CsvStateMachineV2 _owner;
        private CsvLineSliceBuilder _builder;
        private ScanState _state;
        private CsvLineSlice _current;
        private bool _finished;

        public LineEnumerator(CsvStateMachineV2 owner)
        {
            _owner = owner;
            _reader = owner._reader;
            _layout = owner._layout;
            _behaviour = owner._behaviour;
            _builder = new CsvLineSliceBuilder(_layout, _behaviour);
            _state = ScanState.BeginningOfLine;
        }

        public CsvLineSlice Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_finished)
            {
                return false;
            }

            while (true)
            {
                if (!_reader.TryGetSpan(out var span))
                {
                    var finalLine = _builder.NextField().ToLine();
                    _owner.FieldCount = _builder.FieldCount;
                    _finished = true;
                    if (_behaviour.EmptyLineAction == EmptyLineAction.Skip && finalLine.IsEmpty)
                    {
                        return false;
                    }
                    _current = finalLine;
                    return true;
                }

                var buffer = _reader.CurrentBuffer;
                var baseIndex = _reader.CurrentIndex;
                var i = 0;
                while (i < span.Length)
                {
                    switch (_state)
                    {
                        case ScanState.BeginningOfLine:
                        {
                            var c = span[i];
                            var next = i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1);
                            if (c == '\r')
                            {
                                _builder.SetCurrent(c, next);
                                i++;
                                break;
                            }
                            if (c == '\n')
                            {
                                _builder.SetCurrent(c, next);
                                var line = _builder.ToLine();
                                _owner.FieldCount = _builder.FieldCount;
                                if (ShouldReturn(line, i + 1))
                                {
                                    return true;
                                }
                                i++;
                                break;
                            }
                            if (c == _layout.Comment)
                            {
                                _builder.SetCurrent(c, next);
                                _state = ScanState.InComment;
                                i++;
                                break;
                            }
                            if (c == _layout.Quote)
                            {
                                _builder.SetCurrent(c, next);
                                _builder.MarkQuoted();
                                _state = ScanState.InsideQuotedField;
                                i++;
                                break;
                            }
                            if (c == _layout.Delimiter)
                            {
                                _builder.SetCurrent(c, next);
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
                            var c = span[i];
                            var next = i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1);
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
                            var slice = span.Slice(i);
                            var idx = slice.IndexOfAny(_layout.Delimiter, '\n', '\r');
                            if (idx < 0)
                            {
                                _builder.AddToField(slice);
                                _builder.AdvanceSpan(slice);
                                i = span.Length;
                                break;
                            }
                            if (idx > 0)
                            {
                                var run = slice.Slice(0, idx);
                                if (!_builder.TrySetDirectSlice(buffer, baseIndex + i, idx))
                                {
                                    _builder.AddToField(run);
                                }
                                _builder.AdvanceSpan(run);
                                i += idx;
                            }

                            var c = span[i];
                            _builder.SetCurrent(c, i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1));
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
                                _owner.FieldCount = _builder.FieldCount;
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
                            var c = span[i];
                            var next = i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1);
                            if (c == '\r')
                            {
                                _builder.SetCurrent(c, next);
                                i++;
                                break;
                            }
                            if (c == '\n')
                            {
                                _builder.SetCurrent(c, next);
                                _builder.AcceptTentative().NextField();
                                var line = _builder.ToLine();
                                _owner.FieldCount = _builder.FieldCount;
                                _state = ScanState.BeginningOfLine;
                                if (ShouldReturn(line, i + 1))
                                {
                                    return true;
                                }
                                i++;
                                break;
                            }
                            if (c == _layout.Quote)
                            {
                                _builder.SetCurrent(c, next);
                                _builder.MarkQuoted().DiscardTentative();
                                _state = ScanState.InsideQuotedField;
                                i++;
                                break;
                            }
                            if (c == _layout.Delimiter)
                            {
                                _builder.SetCurrent(c, next);
                                _builder.AcceptTentative().NextField();
                                _state = ScanState.OutsideField;
                                i++;
                                break;
                            }
                            if (char.IsWhiteSpace(c))
                            {
                                _builder.SetCurrent(c, next);
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
                            var c = span[i];
                            _builder.SetCurrent(c, i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1));
                            _builder.AddToField();
                            _state = ScanState.InsideQuotedField;
                            i++;
                            break;
                        }

                        case ScanState.InsideQuotedField:
                        {
                            var slice = span.Slice(i);
                            var idx = _layout.Escape == _layout.Quote
                                ? slice.IndexOf(_layout.Quote)
                                : slice.IndexOfAny(_layout.Quote, _layout.Escape);
                            if (idx < 0)
                            {
                                _builder.AddToField(slice);
                                _builder.AdvanceSpan(slice);
                                i = span.Length;
                                break;
                            }
                            if (idx > 0)
                            {
                                var run = slice.Slice(0, idx);
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
                            if (c == _layout.Quote)
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
                            var c = span[i];
                            _builder.SetCurrent(c, i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1));
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
                                _owner.FieldCount = _builder.FieldCount;
                                _state = ScanState.BeginningOfLine;
                                if (ShouldReturn(line, i + 1))
                                {
                                    return true;
                                }
                                i++;
                                break;
                            }
                            if (c == _layout.Quote)
                            {
                                _builder.AcceptTentative().AddToTentative();
                                i++;
                                break;
                            }
                            if (char.IsWhiteSpace(c) || c == '\r')
                            {
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
                            var c = span[i];
                            _builder.SetCurrent(c, i + 1 < span.Length ? span[i + 1] : _reader.PeekAt(baseIndex + i + 1));
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

                _builder.MaterializeDirectSlices(buffer);
                _reader.Advance(span.Length);
            }
        }

        private bool ShouldReturn(CsvLineSlice line, int consumed)
        {
            if (_behaviour.EmptyLineAction != EmptyLineAction.Skip || !line.IsEmpty)
            {
                _current = line;
                _reader.Advance(consumed);
                return true;
            }
            return false;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
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

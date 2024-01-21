namespace Net.Code.Csv.Impl;

using System.Diagnostics;

using ProcessStateFunc = Func<CsvLineBuilder, CsvBehaviour, ProcessingResult>;
/// <summary>
/// CsvStateMachine is a core component of this CSV parsing library, responsible for parsing CSV data
/// into structured CsvLine records. It implements a state machine pattern to manage the complexity
/// of parsing CSV files, which can vary in format and content.
///
/// Key Responsibilities:
/// - Iteratively processes CSV data character-by-character, handling different scenarios like quoted fields,
///   escaped characters, comments, and varying line endings.
/// - Transitions between different states based on the current character and CSV parsing rules. The states include
///   handling the beginning of lines, inside/outside of fields, quoted fields, comments, and handling parse errors.
/// - Works in conjunction with CsvLineBuilder to construct CsvLine records, which represent individual lines
///   in the CSV file with their respective fields.
/// - Utilizes CsvLayout to understand the CSV format (such as delimiters, quote characters) and CsvBehaviour
///   to determine how to handle specific parsing scenarios (like missing fields or quotes inside quoted fields).
///
/// Usage:
/// The state machine is used internally by the CSV parser to process CSV data. It is not intended for direct
/// use outside of this library. Instead, users should interact with higher-level CSV parsing functions that
/// instantiate and manage this class.
///
/// Implementation Details:
/// - The state machine uses a series of delegate functions (ProcessStateFunc) to represent different parsing states.
/// - Each state function handles specific parsing logic and determines the next state based on the current character
///   and CSV rules, making the parser adaptable to various CSV formats.
/// - Error handling is integral to the state machine, with custom exceptions (MalformedCsvException, MissingFieldCsvException)
///   providing detailed error information.
///
/// Note:
/// This class is internally used and should be maintained with care. Changes to the state machine logic can
/// significantly impact the CSV parsing capabilities and correctness.
/// </summary>
internal class CsvStateMachine
{
    private readonly TextReader _textReader;
    private readonly CsvLayout _csvLayout;
    private readonly CsvBehaviour _behaviour;

    public CsvStateMachine(TextReader textReader, CsvLayout csvLayout, CsvBehaviour behaviour)
    {
        _textReader = textReader;
        _csvLayout = csvLayout;
        _behaviour = behaviour;
    }

    public int? FieldCount { get; set; }

    public IEnumerable<CsvLine> Lines() => LinesImpl().Where(line
        => !line.IsEmpty || _behaviour.EmptyLineAction != EmptyLineAction.Skip);
    private IEnumerable<CsvLine> LinesImpl()
    {
        ProcessStateFunc ProcessState = BeginningOfLine;
        var state = new CsvLineBuilder(_csvLayout, _behaviour);
        while (state.ReadNext(_textReader))
        {
            Trace.TraceInformation("");
            var result = ProcessState(state, _behaviour);
            var line = result.Line;
            if (line.HasValue)
            {
                FieldCount = state.FieldCount;
                yield return line.Value;
            }
            ProcessState = result.Next;
            state = result.State;
        }

        var finalLine = state.NextField().ToLine();
        FieldCount = state.FieldCount;
        yield return finalLine;
    }

    // begin of line can be newline, comment, quote or other 
    private static ProcessingResult BeginningOfLine(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsCarriageReturn: true }
            => new ProcessingResult(BeginningOfLine, state),
        { IsNewLine: true }
            // We are at the beginnig of the line and immediately find a newline, so this was an empty line
            // Stay on Beginning of line and prepare for the next 
            => new(BeginningOfLine, state.Ignore(), state.ToLine()),
        { IsComment: true }
            // The beginning of the line is marked as a comment. Ignore this character and transition to InComment
            => new(InComment, state.Ignore()),
        { IsQuote: true }
            // Beginning of line is a quote. Mark as such and transition to InsideQuotedField.
            => new(InsideQuotedField, state.MarkQuoted()),
        { IsDelimiter: true }
            // Delimiter: line starts with empty field
            => new(OutsideField, state.NextField()),
        _
            // Not a newline, comment, quote or delimiter => Transition to InsideField. 
            => new(InsideField, state.AddToField())
    };

    // If we're processing a comment, there is nothing to do. 
    // When we encounter a newline character we simply start a new line.
    private static ProcessingResult InComment(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsCarriageReturn: true }
            => new ProcessingResult(InComment, state),
        { IsNewLine: true }
            => new(BeginningOfLine, state.PrepareNextLine()),
        _
            => new(InComment, state)
    };

    // Inside a non-quoted field, we can encounter either a delimiter
    // or a new line. Otherwise, we accumulate the character for the current field.
    private static ProcessingResult InsideField(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsCarriageReturn: true }
            // ignore 
            => new ProcessingResult(InsideField, state),
        { IsDelimiter: true }
            // end of field because delimiter
            => new(OutsideField, state.NextField()),
        { IsNewLine: true }
            // end of field because newline
            => new(BeginningOfLine, state.NextField(), state.ToLine()),
        _
            // keep going
            => new(InsideField, state.AddToField())
    };

    // Outside field (after a delimiter): find out whether next field is quoted
    // white space may have to be added to the next field (if it is not quoted 
    // and we don't want to trim)
    // if the next field is quoted, whitespace has to be skipped in any case
    private static ProcessingResult OutsideField(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsCarriageReturn: true }
            => new(OutsideField, state.Ignore()), 
        { IsNewLine: true }
            // Found newline. Accumulated whitespace belongs to last field.
            // yield line and transition to next
            => new(BeginningOfLine, state.AcceptTentative().NextField(), state.ToLine()),
        { IsQuote: true }
            // There is another field, and it's quoted
            // Accumulated white space should be ignored, and we are now in a quoted field.
            => new(InsideQuotedField, state.MarkQuoted().DiscardTentative()),
        { IsDelimiter: true }
            // Found the next delimiter. We found an empty field.
            // Accumulated whitespace should be added to this current field
            => new(OutsideField, state.AcceptTentative().NextField()),
        { IsWhiteSpace: true } or { IsCarriageReturn: true }
            // Found whitespace, accumulate until we find a non-whitespace character
            => new(OutsideField, state.AddToTentative()),
        _
            // Found a 'normal' (non-newline, non-whitespace, non-quote) character.
            // There is another field and it's not quoted.
            // Accumulated whitespace should be added to this current field
            => new(InsideField, state.AcceptTentative().AddToField())
    };

    private static ProcessingResult Escaped(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        _ => new(InsideQuotedField, state.AddToField())
    };

    private static ProcessingResult InsideQuotedField(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsEscape: true }
            => new(Escaped, state),
        { IsQuote: true }
            // there are 2 possibilities: 
            // - either the quote is just part of the field
            //   (e.g. "foo,"bar "baz"", foobar")
            // - or the quote is actually the end of this field
            // => start capturing after the quote; check for delimiter 
            => new(AfterSecondQuote, state.DiscardTentative().AddToTentative()),
        _
            => new(InsideQuotedField, state.AddToField())
    };

    // after second quote, we need to detect if we're actually at the end of a field. This is
    // the case when the first non-whitespace character is the delimiter or end of line
    private static ProcessingResult AfterSecondQuote(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsDelimiter: true }
            // we encountered a field delimiter after the second quote, so we're actually at the end of the field
            // any accumulated whitespace should be ignored, and a new field starts
            => new(OutsideField, state.DiscardTentative().NextField()),
        { IsNewLine: true }
            // we encountered a newline after the second quote, so we're actually at the end of the field
            // we will transition to EndOfLine
            // the second quote did mark the end of the field and we're at the end of the line
            => new(BeginningOfLine, state.DiscardTentative().NextField(), state.ToLine()),
        { IsQuote: true }
            => new(AfterSecondQuote, state.AcceptTentative().AddToTentative()),
        { IsWhiteSpace: true } or { IsCarriageReturn: true }
            // as long as we encounter white space, keep accumulating
            => new(AfterSecondQuote, state.AddToTentative()),
        _
            // we found a 'normal' (non-whitespace, non-delimiter, non-newline) character after 
            // the second quote, therefore the second quote did NOT mark the end of the field.
            // this means that the quote is now confirmed to have occurred inside the quoted field.
            // Strictly speaking, this is an error; depending on the configured behaviour we 
            // - throw an exception, 
            // - ignore the rest of the line 
            // - ignore the situation and consider the quote as part of the field
            => behaviour.QuotesInsideQuotedFieldAction switch
            {
                QuotesInsideQuotedFieldAction.AdvanceToNextLine => new(ParseError, state),
                QuotesInsideQuotedFieldAction.Ignore => new(InsideQuotedField, state.AcceptTentative().AddToField()),
                _ => throw new MalformedCsvException(state.RawData, state.Location, state.Fields.Count)
            }
    };

    // A parse error was detected. Ignore unless EOL.
    private static ProcessingResult ParseError(CsvLineBuilder state, CsvBehaviour behaviour) => state.CurrentChar switch
    {
        { IsNewLine: true }
            // at end of line; transition to beginning of line for next line
            => new(BeginningOfLine, state.PrepareNextLine()),
        _
            // As long as we're not on the EOL, ignore any read characters
            => new(ParseError, state.Ignore())
    };
}
record struct ProcessingResult(ProcessStateFunc Next, CsvLineBuilder State, CsvLine? Line = null);

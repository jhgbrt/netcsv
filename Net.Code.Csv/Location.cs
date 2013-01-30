namespace Net.Code.Csv
{
    enum Location
    {
        InsideField,
        InsideQuotedField,
        AfterSecondQuote,
        OutsideField,
        Escaped,
        EndOfLine,
        BeginningOfLine,
        Comment,
        ParseError
    }
}
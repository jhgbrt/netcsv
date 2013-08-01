namespace Net.Code.Csv.Impl
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
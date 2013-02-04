namespace Net.Code.Csv
{
	/// <summary>
	/// Specifies the action to take when a quote is found inside a quoted field
	/// </summary>
	public enum QuotesInsideQuotedFieldAction
	{
        /// <summary>
        /// Ignore the quote
        /// </summary>
		Ignore = 0,
        /// <summary>
        /// Skip the line
        /// </summary>
		AdvanceToNextLine = 1,
        /// <summary>
        /// Throw an exception
        /// </summary>
		ThrowException = 2,
	}
}

namespace Net.Code.Csv
{
	/// <summary>
	/// Specifies the action to take when a quote is found inside a quoted field
	/// </summary>
	public enum QuotesInsideQuotedFieldAction
	{
		Ignore = 0,
		AdvanceToNextLine = 1,
		ThrowException = 2,
	}
}

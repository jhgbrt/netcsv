namespace Net.Code.Csv
{
	/// <summary>
	/// Drives the behaviour of the CSV parser when a missing field is encountered
	/// </summary>
	public enum MissingFieldAction
	{
		/// <summary>
        /// Consider a missing field as a parse error and throw a <see cref="MissingFieldCsvException"/>
		/// </summary>
		ParseError = 0,
		/// <summary>
		/// Replace the missing field by an empty string
		/// </summary>
		ReplaceByEmpty = 1,
		/// <summary>
		/// Replace the missing field by a null value
		/// </summary>
		ReplaceByNull = 2,
	}
}
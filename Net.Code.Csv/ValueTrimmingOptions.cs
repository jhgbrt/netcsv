using System;

namespace Net.Code.Csv
{
	/// <summary>
	/// Defines the different possibilities for trimming field values
	/// </summary>
	[Flags]
	public enum ValueTrimmingOptions
	{
		/// <summary>
		/// Do nothing when a field starts or ends with white space
		/// </summary>
		None = 0,
        /// <summary>
        /// Only unquoted fields are trimmed
        /// </summary>
		UnquotedOnly = 1,
        /// <summary>
        /// Only quoted fields are trimmed
        /// </summary>
		QuotedOnly = 2,
        /// <summary>
        /// Both quoted and unquoted fields are trimmed
        /// </summary>
		All = UnquotedOnly | QuotedOnly
	}
}
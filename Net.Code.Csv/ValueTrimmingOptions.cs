using System;

namespace Net.Code.Csv
{
	[Flags]
	public enum ValueTrimmingOptions
	{
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
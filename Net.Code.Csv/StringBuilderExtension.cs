using System.Text;

namespace Net.Code.Csv.Tests.Unit.Csv
{
    static class StringBuilderExtension
    {
        public static StringBuilder QuoteIfNecessary(this StringBuilder sb, char quote, char delimiter, char escape)
        {
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == quote || sb[i] == delimiter || sb[i] == '\n' || sb[i] == '\r')
                {
                    return sb.EscapeQuotes(quote, escape).Insert(0, quote).Append(quote);
                }
            }
            return sb;
        }

        public static StringBuilder EscapeQuotes(this StringBuilder sb, char quote, char escape)
        {
            int i = 0;
            while (i < sb.Length)
            {
                if (sb[i] == quote)
                {
                    sb.Insert(i++, escape);
                }
                i++;
            }
            return sb;
        }

    }
}

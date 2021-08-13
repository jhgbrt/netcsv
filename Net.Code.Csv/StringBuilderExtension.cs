namespace Net.Code.Csv;

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

    public static string Trim(this StringBuilder sb)
    {
        if (sb == null) return null;
        if (sb.Length == 0) return string.Empty;
        var start = 0;
        while (start < sb.Length && char.IsWhiteSpace(sb[start])) start++;
        if (start == sb.Length) return string.Empty;
        var end = sb.Length - 1;
        while (end > 0 && char.IsWhiteSpace(sb[end])) end--;
        return sb.ToString(start, end - start + 1);
    }
}

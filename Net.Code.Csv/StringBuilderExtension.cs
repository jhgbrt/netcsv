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
        // Single linear pass: grow once, then shift characters from the back (no per-quote Insert).
        var length = sb.Length;
        var quotes = 0;
        for (var i = 0; i < length; i++)
        {
            if (sb[i] == quote) quotes++;
        }
        if (quotes == 0)
        {
            return sb;
        }

        sb.Length = length + quotes;
        var dst = sb.Length - 1;
        for (var src = length - 1; src >= 0 && dst > src; src--)
        {
            var c = sb[src];
            sb[dst--] = c;
            if (c == quote)
            {
                sb[dst--] = escape;
            }
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

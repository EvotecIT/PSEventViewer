namespace EventViewerX;

public partial class SearchEvents {
    private static string EscapeXPathValue(string value) {
        return System.Security.SecurityElement.Escape(value);
    }
    private static string JoinXPathFilter(string newFilter, string existingFilter = "", string logic = "and", bool noParenthesis = false) {
        if (!string.IsNullOrEmpty(existingFilter)) {
            return noParenthesis
                ? $"{existingFilter} {logic} {newFilter}"
                : $"({existingFilter}) {logic} ({newFilter})";
        }
        return newFilter;
    }

    private static string InitializeXPathFilter(IEnumerable<object> items, string forEachFormatString, string finalizeFormatString, string logic = "or", bool noParenthesis = false, bool escapeItems = true) {
        var filter = string.Empty;
        foreach (var item in items) {
            var value = escapeItems ? EscapeXPathValue(item.ToString()) : item.ToString();
            var formatted = forEachFormatString.Replace("{0}", $"{value}");
            filter = JoinXPathFilter(formatted, filter, logic, noParenthesis);
        }
        return finalizeFormatString.Replace("{0}", $"{filter}");
    }

    private static IEnumerable<string> AsEnumerable(object obj) {
        if (obj is IEnumerable enumerable and not string) {
            foreach (var o in enumerable) {
                if (o != null) {
                    yield return o.ToString();
                }
            }
        } else if (obj != null) {
            yield return obj.ToString();
        }
    }

    /// <summary>
    /// Cache for translated user identifiers to avoid repeated lookups
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> userSidCache = new ConcurrentDictionary<string, string>();

    /// <summary>
    /// Builds an XPath query string based on provided filter criteria.
    /// </summary>

}
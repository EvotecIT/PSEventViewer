using System;

namespace EventViewerX.Reports.QueryHelpers;

internal static class QueryFailureHelpers {
    internal static bool IsTimeoutLike(string? message) {
        if (string.IsNullOrWhiteSpace(message)) {
            return false;
        }

        var text = message!;
        return text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

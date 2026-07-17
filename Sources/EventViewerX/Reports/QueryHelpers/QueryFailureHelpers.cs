using System;
using System.Diagnostics.Eventing.Reader;

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

    internal static bool IsInvalidEventQuery(EventLogException exception) {
        const int ErrorEvtInvalidQuery = 15001;
        return (exception.HResult & 0xffff) == ErrorEvtInvalidQuery ||
               exception.Message.IndexOf("query is invalid", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

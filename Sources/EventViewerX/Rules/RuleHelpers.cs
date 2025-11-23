using System;
using System.Globalization;

namespace EventViewerX.Rules;

internal static class RuleHelpers
{
    /// <summary>
    /// Parses the common NoNameA0/NoNameA1 date+time fragments into UTC.
    /// </summary>
    internal static DateTime? ParseUnlabeledOsTimestamp(EventObject e)
    {
        if (e == null) return null;

        var date = e.GetValueFromDataDictionary("NoNameA1");
        var time = e.GetValueFromDataDictionary("NoNameA0");

        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(time))
        {
            return null;
        }

        var candidates = new[]
        {
            $"{date} {time}",
            $"{date}T{time}Z"
        };

        foreach (var candidate in candidates)
        {
            if (DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
            {
                return dt.ToUniversalTime();
            }
        }

        return null;
    }

    internal static DateTime? ParseDateTimeLoose(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            return dt.ToUniversalTime();
        }
        return null;
    }

    /// <summary>
    /// Backward-compat shim for older rule files expecting ParseUnlabeledDateTime.
    /// </summary>
    internal static DateTime? ParseUnlabeledDateTime(EventObject e) => ParseUnlabeledOsTimestamp(e);

    internal static bool IsProvider(EventObject e, params string[] providers)
    {
        if (e == null || providers == null) return false;
        foreach (var p in providers)
        {
            if (string.Equals(e.ProviderName, p, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    internal static bool IsChannel(EventObject e, params string[] channels)
    {
        if (e == null || channels == null) return false;
        foreach (var c in channels)
        {
            if (string.Equals(e.ContainerLog, c, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    internal static int? GetInt(EventObject e, params string[] keys)
    {
        if (e == null || keys == null) return null;
        foreach (var k in keys)
        {
            var v = e.GetValueFromDataDictionary(k);
            if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var i)) return i;
        }
        return null;
    }
}

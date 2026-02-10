using System;
using System.Collections.Generic;
using System.Linq;

namespace EventViewerX.Reports;

/// <summary>
/// Small aggregation helpers for report builders.
/// </summary>
public static class ReportAggregates {
    /// <summary>
    /// Increments a count for <paramref name="key"/> in <paramref name="dict"/>.
    /// </summary>
    /// <param name="dict">Target dictionary.</param>
    /// <param name="key">Key to increment.</param>
    /// <param name="useUnknownPlaceholder">When true, empty keys are counted as <c>(unknown)</c>.</param>
    public static void AddCount(Dictionary<string, long> dict, string? key, bool useUnknownPlaceholder = false) {
        if (dict is null) throw new ArgumentNullException(nameof(dict));

        string? k;
        if (string.IsNullOrWhiteSpace(key)) {
            if (!useUnknownPlaceholder) return;
            k = "(unknown)";
        } else {
            k = key!.Trim();
        }

        dict.TryGetValue(k, out var cur);
        dict[k] = cur + 1;
    }

    /// <summary>
    /// Increments a count for <paramref name="key"/> in <paramref name="dict"/>.
    /// </summary>
    /// <param name="dict">Target dictionary.</param>
    /// <param name="key">Key to increment.</param>
    public static void AddCount(Dictionary<int, long> dict, int key) {
        if (dict is null) throw new ArgumentNullException(nameof(dict));

        dict.TryGetValue(key, out var cur);
        dict[key] = cur + 1;
    }

    /// <summary>
    /// Returns the top <paramref name="top"/> pairs ordered by count desc then key asc (case-insensitive for strings).
    /// </summary>
    /// <param name="dict">Source counts.</param>
    /// <param name="top">Maximum items to return.</param>
    public static IReadOnlyList<KeyValuePair<string, long>> TopStringPairs(Dictionary<string, long> dict, int top) {
        if (dict is null) throw new ArgumentNullException(nameof(dict));
        if (top <= 0) return Array.Empty<KeyValuePair<string, long>>();

        return dict
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToList();
    }

    /// <summary>
    /// Returns the top <paramref name="top"/> pairs ordered by count desc then key asc.
    /// </summary>
    /// <param name="dict">Source counts.</param>
    /// <param name="top">Maximum items to return.</param>
    public static IReadOnlyList<KeyValuePair<int, long>> TopIntPairs(Dictionary<int, long> dict, int top) {
        if (dict is null) throw new ArgumentNullException(nameof(dict));
        if (top <= 0) return Array.Empty<KeyValuePair<int, long>>();

        return dict
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(top)
            .ToList();
    }
}


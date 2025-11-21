using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace EventViewerX;

/// <summary>
/// Channel policy getters (modern wevt + classic fallback).
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Returns a channel policy for a single log.
    /// </summary>
    public static ChannelPolicy? GetChannelPolicy(string logName, string? machineName = null) {
        if (string.IsNullOrWhiteSpace(logName)) {
            throw new ArgumentException("logName cannot be null or empty", nameof(logName));
        }

        EventLogSession? session = null;
        try {
            session = CreateSession(machineName, "ChannelPolicy.Get", logName, DefaultSessionTimeoutMs);
            if (session == null) return null;

            try {
                using var cfg = new EventLogConfiguration(logName, session);
                return new ChannelPolicy {
                    LogName = cfg.LogName,
                    MachineName = machineName,
                    IsEnabled = cfg.IsEnabled,
                    MaximumSizeInBytes = cfg.MaximumSizeInBytes,
                    LogFilePath = cfg.LogFilePath,
                    Isolation = cfg.LogIsolation,
                    Mode = cfg.LogMode,
                    SecurityDescriptor = cfg.SecurityDescriptor,
                };
            } catch (EventLogException ex) {
                _logger.WriteVerbose($"EventLogConfiguration not available for '{logName}' on '{machineName ?? GetFQDN()}': {ex.Message}. Falling back to classic API where possible.");
                // Classic fallback (limited surface)
                try {
                    using var classic = string.IsNullOrEmpty(machineName)
                        ? new EventLog(logName)
                        : new EventLog(logName, machineName);

                    EventLogMode? mode = null;
                    switch (classic.OverflowAction) {
                        case OverflowAction.OverwriteAsNeeded:
                            mode = EventLogMode.Circular;
                            break;
                        case OverflowAction.DoNotOverwrite:
                            mode = EventLogMode.Retain;
                            break;
                        case OverflowAction.OverwriteOlder:
                            mode = EventLogMode.Circular; // best-effort mapping
                            break;
                    }

                    return new ChannelPolicy {
                        LogName = logName,
                        MachineName = machineName,
                        MaximumSizeInBytes = classic.MaximumKilobytes * 1024L,
                        Mode = mode,
                        // Classic API has no IsEnabled/Isolation/SDDL surface
                    };
                } catch (Exception exClassic) {
                    _logger.WriteWarning($"Failed to read classic log policy for '{logName}' on '{machineName ?? GetFQDN()}': {exClassic.Message}");
                    return null;
                }
            }
        } finally {
            session?.Dispose();
        }
    }

    /// <summary>
    /// Enumerates policies for all logs on a machine.
    /// </summary>
    /// <param name="machineName">Machine name or null for local.</param>
    /// <param name="includePatterns">Optional wildcard filters.</param>
    /// <param name="parallel">If true, enumerate policies in parallel. Defaults to false.</param>
    /// <param name="degreeOfParallelism">When parallel, max concurrency. Defaults to Environment.ProcessorCount.</param>
    public static IEnumerable<ChannelPolicy> GetChannelPolicies(string? machineName = null, string[]? includePatterns = null, bool parallel = false, int? degreeOfParallelism = null) {
        EventLogSession? session = null;
        try {
            session = string.IsNullOrEmpty(machineName)
                ? new EventLogSession()
                : new EventLogSession(machineName);

            var names = session.GetLogNames();
            IEnumerable<string> filtered = names;
            if (includePatterns != null && includePatterns.Length > 0) {
                filtered = names.Where(n => includePatterns.Any(p => MatchesWildcard(n, p)));
            }

            if (parallel) {
                int dop = Math.Max(1, degreeOfParallelism ?? Environment.ProcessorCount);
                var query = filtered
                    .AsParallel()
                    .WithDegreeOfParallelism(dop)
                    .Select(n => GetChannelPolicy(n, machineName))
                    .Where(p => p != null)
                    .Cast<ChannelPolicy>();
                foreach (var pol in query) {
                    yield return pol;
                }
            } else {
                foreach (var name in filtered) {
                    var pol = GetChannelPolicy(name, machineName);
                    if (pol != null) yield return pol;
                }
            }
        } finally {
            session?.Dispose();
        }
    }

    private static bool MatchesWildcard(string input, string pattern) {
        if (string.IsNullOrEmpty(pattern)) return true;
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

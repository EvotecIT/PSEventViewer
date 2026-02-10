using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventViewerX.Reports;

namespace EventViewerX.Reports.Stats;

/// <summary>
/// Report builder for basic EVTX statistics (counts by ID/provider/level/computer, min/max times).
/// </summary>
public sealed class EvtxStatsReportBuilder {
    private readonly Dictionary<int, long> _byEventId = new();
    private readonly Dictionary<string, long> _byProviderName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _byComputerName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, EvtxLevelStats> _byLevel = new();

    private int _scanned;
    private DateTime? _minUtc;
    private DateTime? _maxUtc;

    /// <summary>Number of scanned events passed into the builder.</summary>
    public int Scanned => _scanned;
    /// <summary>Minimum event time (UTC) among scanned events.</summary>
    public DateTime? MinUtc => _minUtc;
    /// <summary>Maximum event time (UTC) among scanned events.</summary>
    public DateTime? MaxUtc => _maxUtc;

    /// <summary>
    /// Adds a detailed event object to the report.
    /// </summary>
    public void Add(EventObject ev) {
        if (ev is null) throw new ArgumentNullException(nameof(ev));

        Add(
            id: ev.Id,
            timeCreatedUtc: ev.TimeCreated.ToUniversalTime(),
            providerName: ev.ProviderName,
            computerName: ev.ComputerName,
            level: (int)(ev.Level ?? 0),
            levelDisplayName: ev.LevelDisplayName);
    }

    /// <summary>
    /// Adds a single event's key fields to the report.
    /// </summary>
    public void Add(
        int id,
        DateTime timeCreatedUtc,
        string? providerName,
        string? computerName,
        int level,
        string? levelDisplayName) {

        _scanned++;

        if (!_minUtc.HasValue || timeCreatedUtc < _minUtc.Value) _minUtc = timeCreatedUtc;
        if (!_maxUtc.HasValue || timeCreatedUtc > _maxUtc.Value) _maxUtc = timeCreatedUtc;

        ReportAggregates.AddCount(_byEventId, id);
        ReportAggregates.AddCount(_byProviderName, providerName, useUnknownPlaceholder: true);
        AddComputer(_byComputerName, computerName);
        AddLevel(_byLevel, level, levelDisplayName);
    }

    /// <summary>
    /// Builds a report snapshot.
    /// </summary>
    public EvtxStatsReport Build() {
        return new EvtxStatsReport {
            Scanned = _scanned,
            MinUtc = _minUtc,
            MaxUtc = _maxUtc,
            ByEventId = _byEventId,
            ByProviderName = _byProviderName,
            ByComputerName = _byComputerName,
            ByLevel = _byLevel
        };
    }

    /// <summary>
    /// Convenience API: reads an EVTX file using <see cref="SearchEvents.QueryLogFile"/> and returns a stats report.
    /// </summary>
    public static EvtxStatsReport BuildFromFile(
        string filePath,
        List<int>? eventIds = null,
        string? providerName = null,
        DateTime? startTimeUtc = null,
        DateTime? endTimeUtc = null,
        int maxEvents = 0,
        bool oldestFirst = false,
        CancellationToken cancellationToken = default) {

        var builder = new EvtxStatsReportBuilder();
        foreach (var ev in SearchEvents.QueryLogFile(
                     filePath: filePath,
                     eventIds: eventIds,
                     providerName: providerName,
                     startTime: startTimeUtc,
                     endTime: endTimeUtc,
                     maxEvents: maxEvents,
                     oldest: oldestFirst,
                     cancellationToken: cancellationToken)) {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Add(ev);
        }
        return builder.Build();
    }

    /// <summary>
    /// Returns the top <paramref name="top"/> event IDs by count.
    /// </summary>
    public IReadOnlyList<KeyValuePair<int, long>> GetTopEventIds(int top) => ReportAggregates.TopIntPairs(_byEventId, top);

    /// <summary>
    /// Returns the top <paramref name="top"/> provider names by count.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, long>> GetTopProviders(int top) => ReportAggregates.TopStringPairs(_byProviderName, top);

    /// <summary>
    /// Returns the top <paramref name="top"/> computer names by count.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, long>> GetTopComputers(int top) => ReportAggregates.TopStringPairs(_byComputerName, top);

    /// <summary>
    /// Returns the top <paramref name="top"/> levels by count.
    /// </summary>
    public IReadOnlyList<EvtxLevelStats> GetTopLevels(int top) {
        if (top <= 0) return Array.Empty<EvtxLevelStats>();
        return _byLevel.Values
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Level)
            .Take(top)
            .ToList();
    }

    private static void AddComputer(Dictionary<string, long> dict, string? computerName) {
        if (string.IsNullOrWhiteSpace(computerName)) {
            return;
        }
        var k = computerName!.Trim();
        dict.TryGetValue(k, out var cur);
        dict[k] = cur + 1;
    }

    private static void AddLevel(Dictionary<int, EvtxLevelStats> dict, int level, string? displayName) {
        dict.TryGetValue(level, out var cur);
        cur ??= new EvtxLevelStats(level, displayName ?? string.Empty);
        cur.Count++;
        dict[level] = cur;
    }
}

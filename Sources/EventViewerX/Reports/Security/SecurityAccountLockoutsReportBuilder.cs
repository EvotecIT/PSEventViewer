using System;
using System.Collections.Generic;
using EventViewerX.Rules.ActiveDirectory;

namespace EventViewerX.Reports.Security;

/// <summary>
/// Report builder for Windows Security account lockouts (4740).
/// </summary>
public sealed class SecurityAccountLockoutsReportBuilder {
    private readonly bool _includeSamples;
    private readonly int _sampleSize;

    private readonly Dictionary<string, long> _byTargetUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _byTargetDomain = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _byCallerComputer = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _byComputer = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _bySubjectUser = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<SecurityAccountLockoutSample> _samples = new();

    private int _scanned;
    private int _matched;
    private DateTime? _minUtc;
    private DateTime? _maxUtc;

    /// <summary>
    /// Creates a new report builder.
    /// </summary>
    /// <param name="includeSamples">When true, captures up to <paramref name="sampleSize"/> sample events.</param>
    /// <param name="sampleSize">Maximum sample events to capture.</param>
    public SecurityAccountLockoutsReportBuilder(bool includeSamples, int sampleSize) {
        _includeSamples = includeSamples;
        _sampleSize = Math.Max(0, sampleSize);
    }

    /// <summary>Number of scanned events passed into the builder.</summary>
    public int Scanned => _scanned;
    /// <summary>Number of matched events.</summary>
    public int Matched => _matched;
    /// <summary>Minimum event time (UTC) among matched events.</summary>
    public DateTime? MinUtc => _minUtc;
    /// <summary>Maximum event time (UTC) among matched events.</summary>
    public DateTime? MaxUtc => _maxUtc;

    /// <summary>
    /// Adds an event to the report.
    /// </summary>
    public void Add(EventObject ev) {
        _scanned++;
        _matched++;

        var utc = ev.TimeCreated.ToUniversalTime();
        if (!_minUtc.HasValue || utc < _minUtc.Value) _minUtc = utc;
        if (!_maxUtc.HasValue || utc > _maxUtc.Value) _maxUtc = utc;

        SecurityAggregates.AddCount(_byComputer, ev.ComputerName ?? string.Empty);

        var data = ev.Data ?? new Dictionary<string, string>();
        var rule = new ADUserLockouts(ev);

        var (targetDomain, targetUser) = SecurityEventText.SplitAccount(rule.UserAffected);
        if (string.IsNullOrWhiteSpace(targetUser)) {
            targetUser = Get(data, "TargetUserName") ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(targetDomain)) {
            targetDomain = Get(data, "TargetDomainName") ?? string.Empty;
        }

        var caller = SecurityEventText.NormalizePlaceholder(Get(data, "CallerComputerName") ?? string.Empty);

        var subjectUser = rule.Who;
        if (string.IsNullOrWhiteSpace(subjectUser)) {
            subjectUser = Get(data, "SubjectUserName") ?? string.Empty;
        }

        SecurityAggregates.AddCount(_byTargetUser, targetUser);
        SecurityAggregates.AddCount(_byTargetDomain, targetDomain);
        SecurityAggregates.AddCount(_byCallerComputer, caller);
        SecurityAggregates.AddCount(_bySubjectUser, subjectUser);

        if (_includeSamples && _samples.Count < _sampleSize) {
            _samples.Add(new SecurityAccountLockoutSample {
                TimeCreatedUtc = utc,
                Id = ev.Id,
                ComputerName = ev.ComputerName ?? string.Empty,
                TargetUser = targetUser,
                TargetDomain = targetDomain,
                CallerComputerName = caller,
                SubjectUser = subjectUser
            });
        }
    }

    /// <summary>
    /// Builds a report snapshot.
    /// </summary>
    public SecurityAccountLockoutsReport Build() {
        return new SecurityAccountLockoutsReport {
            Scanned = _scanned,
            Matched = _matched,
            MinUtc = _minUtc,
            MaxUtc = _maxUtc,
            ByTargetUser = _byTargetUser,
            ByTargetDomain = _byTargetDomain,
            ByCallerComputerName = _byCallerComputer,
            BySubjectUser = _bySubjectUser,
            ByComputerName = _byComputer,
            Samples = _samples
        };
    }

    /// <summary>
    /// Returns top target users by count.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, long>> GetTopTargetUsers(int top) => SecurityAggregates.TopStringPairs(_byTargetUser, top);

    private static string? Get(Dictionary<string, string> dict, string key) {
        return dict.TryGetValue(key, out var v) ? v : null;
    }
}

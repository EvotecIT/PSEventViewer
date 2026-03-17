using System;
using System.Collections.Generic;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventStructuredQueryFilterService {
    [Fact]
    public void TryNormalize_ShouldRejectUnknownKeywords() {
        var ok = EventStructuredQueryFilterService.TryNormalize(
            new EventStructuredQueryFilterInput {
                Keywords = "not_a_keyword"
            },
            out _,
            out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("keywords", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryNormalize_ShouldRejectTooManyEventIds() {
        var values = new List<int>();
        for (var i = 0; i < EventStructuredQueryFilterService.MaxEventIds + 1; i++) {
            values.Add(i + 1);
        }

        var ok = EventStructuredQueryFilterService.TryNormalize(
            new EventStructuredQueryFilterInput {
                EventIds = values
            },
            out _,
            out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("event_ids", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildXPath_ShouldIncludeCoreStructuredFilters() {
        var ok = EventStructuredQueryFilterService.TryNormalize(
            new EventStructuredQueryFilterInput {
                EventIds = new[] { 4624 },
                ProviderName = "Microsoft-Windows-Security-Auditing",
                Level = "error",
                UserId = "S-1-5-18",
                NamedDataFilter = new Dictionary<string, IReadOnlyList<string>> {
                    ["TargetUserName"] = new[] { "alice" }
                }
            },
            out var filter,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        var xpath = EventStructuredQueryFilterService.BuildXPath(filter);
        Assert.Contains("EventID=4624", xpath, StringComparison.Ordinal);
        Assert.Contains("Provider", xpath, StringComparison.Ordinal);
        Assert.Contains("TargetUserName", xpath, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("none")]
    public void TryNormalize_ShouldTreatZeroKeywordMaskAsUnfiltered(string rawKeywords) {
        var ok = EventStructuredQueryFilterService.TryNormalize(
            new EventStructuredQueryFilterInput {
                Keywords = rawKeywords
            },
            out var filter,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(filter);
        Assert.False(filter!.Keywords.HasValue);
    }

    [Fact]
    public void TryNormalize_ShouldPreserveCaseDistinctNamedDataValues() {
        var ok = EventStructuredQueryFilterService.TryNormalize(
            new EventStructuredQueryFilterInput {
                NamedDataFilter = new Dictionary<string, IReadOnlyList<string>> {
                    ["TargetUserName"] = new[] { "Alice", "alice" }
                }
            },
            out var filter,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(filter);

        var values = Assert.IsType<string[]>(filter!.NamedDataFilter!["TargetUserName"]);
        Assert.Equal(new[] { "Alice", "alice" }, values);
    }
}

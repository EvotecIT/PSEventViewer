using System.Linq;
using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace EventViewerX.Tests;

public class TestChannelPolicyDetailed
{
    [Fact]
    public void ChannelPolicy_TrySetModeName_UsesCanonicalModeNames()
    {
        var policy = new ChannelPolicy();

        var success = policy.TrySetModeName("AUTO_BACKUP", out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("auto_backup", policy.ModeName);
    }

    [Fact]
    public void ChannelPolicy_TrySetModeName_RejectsUnknownValues()
    {
        var policy = new ChannelPolicy();

        var success = policy.TrySetModeName("archive_forever", out var error);

        Assert.False(success);
        Assert.Equal("mode must be one of: circular, retain, auto_backup.", error);
        Assert.Null(policy.ModeName);
    }

    [Fact]
    public void GetChannelPolicies_ParallelEnumerates()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Parallel enumeration should not throw and produce items when available
        var items = SearchEvents
            .GetChannelPolicies(machineName: null, includePatterns: new[] { "*" }, parallel: true, degreeOfParallelism: 2)
            .Take(5)
            .ToList();

        Assert.NotNull(items);
        foreach (var p in items)
        {
            Assert.NotNull(p);
            Assert.False(string.IsNullOrWhiteSpace(p.LogName));
        }
    }

    [Fact]
    public void SetChannelPolicyDetailed_IsolationUnsupported_ReturnsPartialSuccess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var result = SearchEvents.SetChannelPolicyDetailed(new ChannelPolicy
        {
            LogName = "Application",
            Isolation = EventLogIsolation.Application
        });

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.True(result.PartialSuccess);
        Assert.Contains("Isolation", result.SkippedOrUnsupported);
        Assert.Empty(result.Errors);
    }
}

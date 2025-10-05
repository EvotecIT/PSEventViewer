using System.Linq;
using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace EventViewerX.Tests;

public class TestChannelPolicyDetailed
{
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


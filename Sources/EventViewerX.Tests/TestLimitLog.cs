using System.Diagnostics;
using Xunit;

namespace EventViewerX.Tests;

public class TestLimitLog {
    [Fact]
    public void LimitExistingLog() {
        if (!OperatingSystem.IsWindows()) return;
        const string logName = "EVXLimitTestLog";
        if (SearchEvents.LogExists(logName)) {
            SearchEvents.RemoveLog(logName);
        }
        SearchEvents.CreateLog(logName, logName, null, 1024, OverflowAction.OverwriteAsNeeded, 1);
        bool limited = SearchEvents.LimitLog(logName, null, 2048, OverflowAction.OverwriteOlder, 2);
        Assert.True(limited);
        using EventLog log = new(logName);
        Assert.Equal(2048, log.MaximumKilobytes);
        Assert.Equal(OverflowAction.OverwriteOlder, log.OverflowAction);
        Assert.Equal(2, log.MinimumRetentionDays);
        SearchEvents.RemoveLog(logName);
    }

    [Fact]
    public void LimitLogOverwriteAsNeeded() {
        if (!OperatingSystem.IsWindows()) return;
        const string logName = "EVXLimitTestLog";
        if (SearchEvents.LogExists(logName)) {
            SearchEvents.RemoveLog(logName);
        }
        SearchEvents.CreateLog(logName, logName, null, 1024, OverflowAction.OverwriteAsNeeded, 1);
        bool limited = SearchEvents.LimitLog(logName, null, 4096, OverflowAction.OverwriteAsNeeded);
        Assert.True(limited);
        using EventLog log = new(logName);
        Assert.Equal(4096, log.MaximumKilobytes);
        Assert.Equal(OverflowAction.OverwriteAsNeeded, log.OverflowAction);
        SearchEvents.RemoveLog(logName);
    }
}
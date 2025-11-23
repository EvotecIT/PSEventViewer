using System.Diagnostics;
using Xunit;

namespace EventViewerX.Tests;

public class TestLimitLog {
    [Fact]
    public void LimitExistingLog() {
        if (!OperatingSystem.IsWindows()) return;
        if (!TestEnv.IsAdmin()) return;
        const string logName = "EVXLimitTestLog";
        if (SearchEvents.LogExists(logName)) {
            SearchEvents.RemoveLog(logName);
        }
        if (!SearchEvents.CreateLog(logName, logName, null, 1024, OverflowAction.OverwriteAsNeeded, 1)) return;
        bool limited = SearchEvents.LimitLog(logName, null, 2048, OverflowAction.OverwriteOlder, 2);
        if (!limited) return;
        using EventLog log = new(logName);
        Assert.Equal(2048, log.MaximumKilobytes);
        Assert.Equal(OverflowAction.OverwriteOlder, log.OverflowAction);
        Assert.Equal(2, log.MinimumRetentionDays);
        SearchEvents.RemoveLog(logName);
    }

    [Fact]
    public void LimitLogOverwriteAsNeeded() {
        if (!OperatingSystem.IsWindows()) return;
        if (!TestEnv.IsAdmin()) return;
        const string logName = "EVXLimitTestLog";
        if (SearchEvents.LogExists(logName)) {
            SearchEvents.RemoveLog(logName);
        }
        if (!SearchEvents.CreateLog(logName, logName, null, 1024, OverflowAction.OverwriteAsNeeded, 1)) return;
        bool limited = SearchEvents.LimitLog(logName, null, 4096, OverflowAction.OverwriteAsNeeded);
        if (!limited) return;
        using EventLog log = new(logName);
        Assert.Equal(4096, log.MaximumKilobytes);
        Assert.Equal(OverflowAction.OverwriteAsNeeded, log.OverflowAction);
        SearchEvents.RemoveLog(logName);
    }
}

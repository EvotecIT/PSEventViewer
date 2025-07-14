using System.Diagnostics;
using Xunit;

namespace EventViewerX.Tests;

    /// <summary>
    /// Unit tests for event log management.
    /// </summary>

public class TestLogManagement {
    [Fact]
    public void CreateAndRemoveLog() {
        if (!OperatingSystem.IsWindows()) return;

        const string logName = "EVXTestCustomLog";
        if (SearchEvents.LogExists(logName)) {
            SearchEvents.RemoveLog(logName);
        }

        bool created = SearchEvents.CreateLog(logName, logName, null, 256, OverflowAction.OverwriteAsNeeded, 1);
        Assert.True(created);
        Assert.True(SearchEvents.LogExists(logName));

        bool removed = SearchEvents.RemoveLog(logName);
        Assert.True(removed);
        Assert.False(SearchEvents.LogExists(logName));
    }
}

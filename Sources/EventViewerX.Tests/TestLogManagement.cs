using System.Diagnostics;
using Xunit;

namespace EventViewerX.Tests;

public class TestLogManagement {
    [Fact]
    public void ClassicLogOverflowActions_TryNormalize_UsesCanonicalNames() {
        var success = ClassicLogOverflowActions.TryNormalize("Overwrite_Older", out var normalized, out var error);

        Assert.True(success);
        Assert.Equal("overwrite_older", normalized);
        Assert.Null(error);
    }

    [Fact]
    public void ClassicLogOverflowActions_TryNormalize_RejectsUnknownValues() {
        var success = ClassicLogOverflowActions.TryNormalize("archive_forever", out var normalized, out var error);

        Assert.False(success);
        Assert.Null(normalized);
        Assert.Equal("overflow_action must be one of: overwrite_as_needed, overwrite_older, do_not_overwrite.", error);
    }

    [Fact]
    public void CreateAndRemoveLog() {
        if (!OperatingSystem.IsWindows()) return;
        if (!TestEnv.IsAdmin()) return;

        const string logName = "EVXTestCustomLog";
        if (SearchEvents.LogExists(logName)) {
            SearchEvents.RemoveLog(logName);
        }

        bool created = SearchEvents.CreateLog(logName, logName, null, 256, OverflowAction.OverwriteAsNeeded, 1);
        if (!created) return; // environments without rights skip
        Assert.True(SearchEvents.LogExists(logName));

        bool removed = SearchEvents.RemoveLog(logName);
        Assert.True(removed);
        Assert.False(SearchEvents.LogExists(logName));
    }
}

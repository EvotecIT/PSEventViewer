using EventViewerX.Native;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestWindowsEventReaderSetup {
    [Fact]
    public void QueryOpenedIsReportedOnlyAfterTheNativeCursorExists() {
        if (!OperatingSystem.IsWindows()) {
            return;
        }

        string path = Path.GetFullPath(Path.Combine(
            "..",
            "..",
            "..",
            "..",
            "..",
            "Tests",
            "Logs",
            "NamedFilterExamples.evtx"));
        var query = new NativeEventQuery(
            IntPtr.Zero,
            path,
            "*",
            WindowsEventNativeMethods.QueryFlags.FilePath |
            WindowsEventNativeMethods.QueryFlags.ForwardDirection,
            path);
        bool queryOpened = false;

        using IEnumerator<EventObject> events =
            WindowsEventReader.Read(
                    query,
                    EventReadMode.Metadata,
                    Environment.MachineName,
                    path,
                    CancellationToken.None,
                    () => queryOpened = true)
                .GetEnumerator();

        Assert.False(queryOpened);
        Assert.True(events.MoveNext());
        Assert.True(queryOpened);
    }
}

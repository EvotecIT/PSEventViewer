using EventViewerX.Native;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestWindowsEventArchiveCancellation {
    [Fact]
    public async Task CancellationLeavesOriginalUntouchedUntilNativeWorkerFinishes() {
        string root = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(
            root,
            "source.evtx");
        byte[] original = { 1, 2, 3, 4 };
        File.WriteAllBytes(
            path,
            original);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        try {
            Task operation = Task.Run(() =>
                WindowsEventArchive.ArchiveFileResources(
                    path,
                    0,
                    cancellation.Token,
                    (temporaryPath, _) => {
                        entered.Set();
                        release.Wait();
                        File.WriteAllBytes(
                            temporaryPath,
                            new byte[] { 9, 8, 7 });
                    }));
            Assert.True(
                entered.Wait(TimeSpan.FromSeconds(5)));

            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation);
            Assert.Equal(
                original,
                File.ReadAllBytes(path));

            release.Set();
            Assert.True(
                SpinWait.SpinUntil(
                    () => Directory
                        .EnumerateFiles(
                            root,
                            "*.archive.evtx")
                        .Count() == 0,
                    TimeSpan.FromSeconds(5)));
            Assert.Equal(
                original,
                File.ReadAllBytes(path));
        } finally {
            release.Set();
            if (Directory.Exists(root)) {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }
}

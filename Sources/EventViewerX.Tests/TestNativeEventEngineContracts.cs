using System.ComponentModel;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestNativeEventEngineContracts {
    [Fact]
    public void RemoteChannelDefaultsToBoundedConnectionAndUnboundedRead() {
        var query = new EventLogChannelQuery("System");

        Assert.Equal(5000, query.RemoteConnectionTimeoutMilliseconds);
        Assert.Equal(0, query.RemoteReadTimeoutMilliseconds);
    }

    [Fact]
    public void FileQueryIsSnapshottedBeforeEnumerationStarts() {
        if (!OperatingSystem.IsWindows()) return;
        string path = GetFixturePath();
        var query = new EventLogFileQuery(path) {
            Oldest = true,
            MaxEvents = 2,
            ReadMode = EventReadMode.Metadata
        };

        IEnumerable<EventObject> events = EventLogEngine.ReadFile(query);
        query.Oldest = false;
        query.MaxEvents = 1;
        query.ReadMode = EventReadMode.Full;
        query.XPath = "*[System[EventID=999999]]";

        EventObject[] actual = events.ToArray();
        Assert.Equal(2, actual.Length);
        Assert.All(actual, static item => Assert.Equal(EventReadMode.Metadata, item.ReadMode));
        Assert.True(actual[0].RecordId < actual[1].RecordId);
    }

    [Fact]
    public void LocalChannelQueryIsSnapshottedBeforeEnumerationStarts() {
        if (!OperatingSystem.IsWindows()) return;
        var query = new EventLogChannelQuery("System") {
            MaxEvents = 2,
            ReadMode = EventReadMode.Metadata
        };

        IEnumerable<EventObject> events = EventLogEngine.ReadChannel(query);
        query.MaxEvents = 1;
        query.ReadMode = EventReadMode.Full;
        query.XPath = "*[System[EventID=999999]]";

        EventObject[] actual = events.ToArray();
        Assert.Equal(2, actual.Length);
        Assert.All(actual, static item => Assert.Equal(EventReadMode.Metadata, item.ReadMode));
    }

    [Fact]
    public void CancellationStopsAFileEnumerationBetweenRecords() {
        if (!OperatingSystem.IsWindows()) return;
        var query = new EventLogFileQuery(GetFixturePath()) {
            Oldest = true,
            ReadMode = EventReadMode.Metadata
        };
        using var cancellation = new CancellationTokenSource();
        int count = 0;

        Assert.Throws<OperationCanceledException>(() => {
            foreach (EventObject _ in EventLogEngine.ReadFile(query, cancellation.Token)) {
                count++;
                if (count == 3) {
                    cancellation.Cancel();
                }
            }
        });
        Assert.Equal(3, count);
    }

    [Fact]
    public void CorruptEventLogFailsLoudlyInsteadOfReturningAnEmptySuccess() {
        if (!OperatingSystem.IsWindows()) return;
        string directory = CreateTemporaryDirectory();
        try {
            string corruptPath = Path.Combine(directory, "corrupt.evtx");
            File.Copy(GetFixturePath(), corruptPath);
            using (FileStream stream = File.Open(corruptPath, FileMode.Open, FileAccess.Write, FileShare.None)) {
                stream.SetLength(4096);
            }
            var query = new EventLogFileQuery(corruptPath) {
                Oldest = true,
                ReadMode = EventReadMode.Metadata
            };

            Exception exception = Assert.ThrowsAny<Exception>(() =>
                EventLogEngine.ReadFile(query).ToArray());

            Assert.True(
                exception is Win32Exception ||
                exception is IOException ||
                exception is InvalidOperationException,
                $"Unexpected corruption failure: {exception.GetType().FullName}");
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CorruptEventLogDoesNotReplaceAnExistingExport() {
        if (!OperatingSystem.IsWindows()) return;
        string directory = CreateTemporaryDirectory();
        try {
            string corruptPath = Path.Combine(directory, "corrupt.evtx");
            File.Copy(GetFixturePath(), corruptPath);
            using (FileStream stream = File.Open(corruptPath, FileMode.Open, FileAccess.Write, FileShare.None)) {
                stream.SetLength(4096);
            }
            string outputPath = Path.Combine(directory, "events.jsonl");
            File.WriteAllText(outputPath, "preserve-me");
            var query = new EventLogFileQuery(corruptPath) {
                Oldest = true,
                ReadMode = EventReadMode.Full
            };

            Assert.ThrowsAny<Exception>(() =>
                EventLogExporter.ExportFile(
                    query,
                    outputPath,
                    EventExportFormat.JsonLines,
                    overwrite: true));

            Assert.Equal("preserve-me", File.ReadAllText(outputPath));
            Assert.Empty(Directory.GetFiles(directory, ".events.jsonl.*.tmp"));
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void XmlExportUsesTheSameRawContractForEveryReadMode() {
        if (!OperatingSystem.IsWindows()) return;
        string directory = CreateTemporaryDirectory();
        try {
            var metadataQuery = new EventLogFileQuery(GetFixturePath()) {
                Oldest = true,
                MaxEvents = 8,
                ReadMode = EventReadMode.Metadata
            };
            var fullQuery = new EventLogFileQuery(GetFixturePath()) {
                Oldest = true,
                MaxEvents = 8,
                ReadMode = EventReadMode.Full
            };

            EventExportResult metadata = EventLogExporter.ExportFile(
                metadataQuery,
                Path.Combine(directory, "metadata.xml"),
                EventExportFormat.Xml);
            EventExportResult full = EventLogExporter.ExportFile(
                fullQuery,
                Path.Combine(directory, "full.xml"),
                EventExportFormat.Xml);

            Assert.Equal(8, metadata.EventCount);
            Assert.Equal(metadata.Bytes, full.Bytes);
            Assert.Equal(metadata.Sha256, full.Sha256);
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LocalChannelExportsJsonLinesWithoutAConsumerPipeline() {
        if (!OperatingSystem.IsWindows()) return;
        string directory = CreateTemporaryDirectory();
        try {
            string outputPath = Path.Combine(directory, "system.jsonl");
            var query = new EventLogChannelQuery("System") {
                MaxEvents = 3,
                ReadMode = EventReadMode.Message,
                MessageCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US")
            };

            EventExportResult result = EventLogExporter.ExportChannel(
                query,
                outputPath,
                EventExportFormat.JsonLines);

            Assert.Equal(3, result.EventCount);
            string[] lines = File.ReadAllLines(outputPath);
            Assert.Equal(3, lines.Length);
            Assert.All(lines, static line => {
                using JsonDocument document = JsonDocument.Parse(line);
                Assert.True(document.RootElement.GetProperty("recordId").GetInt64() > 0);
                Assert.Equal(
                    "en-US",
                    document.RootElement.GetProperty("messageCulture").GetString());
            });
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LocalChannelXmlExportDoesNotMutateTheRequestedReadMode() {
        if (!OperatingSystem.IsWindows()) return;
        string directory = CreateTemporaryDirectory();
        try {
            string outputPath = Path.Combine(directory, "system.xml");
            var query = new EventLogChannelQuery("System") {
                MaxEvents = 3,
                ReadMode = EventReadMode.Metadata
            };

            EventExportResult result = EventLogExporter.ExportChannel(
                query,
                outputPath,
                EventExportFormat.Xml);

            XDocument document = XDocument.Load(outputPath);
            XNamespace eventNamespace =
                "http://schemas.microsoft.com/win/2004/08/events/event";
            Assert.Equal(3, result.EventCount);
            Assert.Equal(3, document.Root!.Elements(eventNamespace + "Event").Count());
            Assert.Equal(EventReadMode.Metadata, query.ReadMode);
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string GetFixturePath() {
        return Path.GetFullPath(Path.Combine(
            "..",
            "..",
            "..",
            "..",
            "..",
            "Tests",
            "Logs",
            "NamedFilterExamples.evtx"));
    }

    private static string CreateTemporaryDirectory() {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"EventViewerX-Native-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}

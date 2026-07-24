using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventLogExporter {
    [Fact]
    public void JsonLinesExportIsDeterministicAndMachineReadable() {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new ExportFixture();
        var query = fixture.CreateQuery(EventReadMode.Full, maxEvents: 5);
        string firstPath = fixture.GetPath("events-first.jsonl");
        string secondPath = fixture.GetPath("events-second.jsonl");

        EventExportResult first = EventLogExporter.ExportFile(
            query,
            firstPath,
            EventExportFormat.JsonLines);
        EventExportResult second = EventLogExporter.ExportFile(
            query,
            secondPath,
            EventExportFormat.JsonLines);

        Assert.Equal(5, first.EventCount);
        Assert.Equal(first.EventCount, second.EventCount);
        Assert.Equal(first.Bytes, second.Bytes);
        Assert.Equal(first.Sha256, second.Sha256);
        string[] lines = File.ReadAllLines(firstPath);
        Assert.Equal(5, lines.Length);
        foreach (string line in lines) {
            using JsonDocument document = JsonDocument.Parse(line);
            Assert.True(document.RootElement.GetProperty("recordId").GetInt64() > 0);
            Assert.False(string.IsNullOrEmpty(
                document.RootElement.GetProperty("providerName").GetString()));
            Assert.Equal(JsonValueKind.Array,
                document.RootElement.GetProperty("properties").ValueKind);
            Assert.Equal(JsonValueKind.String,
                document.RootElement.GetProperty("messageRenderStatus").ValueKind);
            Assert.Equal(JsonValueKind.String,
                document.RootElement.GetProperty("xml").ValueKind);
        }
    }

    [Fact]
    public void CsvExportUsesStableSchemaAndOneRecordPerRowForMetadata() {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new ExportFixture();
        var query = fixture.CreateQuery(EventReadMode.Metadata, maxEvents: 7);
        string outputPath = fixture.GetPath("events.csv");

        EventExportResult result = EventLogExporter.ExportFile(
            query,
            outputPath,
            EventExportFormat.Csv);

        string[] lines = File.ReadAllLines(outputPath);
        Assert.Equal(7, result.EventCount);
        Assert.Equal(8, lines.Length);
        Assert.Equal(
            "TimeCreated,RecordId,Id,ProviderName,MachineName,LogName,Level,LevelDisplayName,Task,TaskDisplayName,Opcode,OpcodeDisplayName,Keywords,KeywordDisplayNames,ProcessId,ThreadId,UserId,MessageCulture,MessageRenderStatus,MessageRenderErrorCode,Message,Properties,Data,Attachments,Xml",
            lines[0]);
        Assert.All(lines.Skip(1), static line => Assert.False(string.IsNullOrWhiteSpace(line)));
    }

    [Fact]
    public void XmlExportProducesOneWellFormedEventElementPerRecord() {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new ExportFixture();
        var query = fixture.CreateQuery(EventReadMode.StructuredData, maxEvents: 6);
        string outputPath = fixture.GetPath("events.xml");

        EventExportResult result = EventLogExporter.ExportFile(
            query,
            outputPath,
            EventExportFormat.Xml);

        XDocument document = XDocument.Load(outputPath, LoadOptions.PreserveWhitespace);
        XNamespace eventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";
        Assert.Equal(6, result.EventCount);
        Assert.Equal(6, document.Root!.Elements(eventNamespace + "Event").Count());
    }

    [Fact]
    public void CancellationPreservesExistingDestinationAndRemovesTemporaryOutput() {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new ExportFixture();
        var query = fixture.CreateQuery(EventReadMode.Full);
        string outputPath = fixture.GetPath("existing.jsonl");
        File.WriteAllText(outputPath, "preserve-me");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            EventLogExporter.ExportFile(
                query,
                outputPath,
                EventExportFormat.JsonLines,
                overwrite: true,
                cancellation.Token));

        Assert.Equal("preserve-me", File.ReadAllText(outputPath));
        Assert.Empty(Directory.GetFiles(
            fixture.DirectoryPath,
            ".existing.jsonl.*.tmp"));
    }

    [Fact]
    public void ExportNeverOverwritesItsSourceEventLog() {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new ExportFixture();
        var query = fixture.CreateQuery(EventReadMode.Full);

        IOException exception = Assert.Throws<IOException>(() =>
            EventLogExporter.ExportFile(
                query,
                fixture.SourcePath,
                EventExportFormat.JsonLines,
                overwrite: true));

        Assert.Contains("cannot overwrite the source", exception.Message);
    }

    [Fact]
    public void PromotionWithoutOverwritePreservesAConcurrentDestination() {
        using var fixture = new ExportFixture();
        string temporaryPath = fixture.GetPath("events.tmp");
        string destinationPath = fixture.GetPath("events.jsonl");
        File.WriteAllText(temporaryPath, "new-output");
        File.WriteAllText(destinationPath, "concurrent-output");

        Assert.Throws<IOException>(() =>
            EventLogExporter.PromoteTemporaryFile(
                temporaryPath,
                destinationPath,
                overwrite: false));

        Assert.Equal("concurrent-output", File.ReadAllText(destinationPath));
        Assert.Equal("new-output", File.ReadAllText(temporaryPath));
    }

    [Fact]
    public void ExportCanSkipTheFinalHashPass() {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new ExportFixture();
        var query = fixture.CreateQuery(EventReadMode.Metadata, maxEvents: 3);
        string outputPath = fixture.GetPath("events-no-hash.jsonl");

        EventExportResult result = EventLogExporter.ExportFile(
            query,
            outputPath,
            EventExportFormat.JsonLines,
            computeSha256: false);

        Assert.Equal(3, result.EventCount);
        Assert.Null(result.Sha256);
        Assert.True(result.Bytes > 0);
        Assert.True(File.Exists(outputPath));
    }

    private sealed class ExportFixture : IDisposable {
        internal ExportFixture() {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"EventViewerX-Export-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            string relativePath = Path.Combine(
                "..",
                "..",
                "..",
                "..",
                "..",
                "Tests",
                "Logs",
                "NamedFilterExamples.evtx");
            SourcePath = Path.GetFullPath(relativePath);
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal EventLogFileQuery CreateQuery(
            EventReadMode readMode,
            int maxEvents = 0) {

            return new EventLogFileQuery(SourcePath) {
                Oldest = true,
                ReadMode = readMode,
                MaxEvents = maxEvents
            };
        }

        internal string GetPath(string fileName) {
            return Path.Combine(DirectoryPath, fileName);
        }

        public void Dispose() {
            if (Directory.Exists(DirectoryPath)) {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}

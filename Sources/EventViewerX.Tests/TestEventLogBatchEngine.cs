using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventLogBatchEngine {
    [Fact]
    public void QueryFactoryPartitionsAndConsolidatesLargeFilters() {
        EventLogBatchQuery query =
            EventLogQueryFactory.ForChannels(
                new[] { "System", "Application" },
                new string?[] { null },
                new EventFilter {
                    EventIds = Enumerable.Range(1, 50)
                        .ToArray()
                },
                new EventLogQueryOptions {
                    MaxEvents = 7,
                    MaxConcurrency = 3,
                    ReadMode = EventReadMode.Metadata
                });

        EventLogStructuredQuery structured =
            Assert.Single(
                query.StructuredQueries);
        Assert.Equal(7, query.MaxEvents);
        Assert.Equal(3, query.MaxConcurrency);
        Assert.Contains("System", structured.QueryXml);
        Assert.Contains("Application", structured.QueryXml);
        Assert.True(
            structured.QueryXml
                .Split("<Select", StringSplitOptions.None)
                .Length > 3);
    }

    [Fact]
    public async Task AsyncBatchMatchesSynchronousNativeSelection() {
        if (!OperatingSystem.IsWindows()) return;
        string path = GetFixturePath();

        var syncQuery = EventLogBatchQuery.ForFiles(new[] {
            new EventLogFileQuery(path) {
                ReadMode = EventReadMode.Metadata,
                MaxEvents = 4
            }
        });
        syncQuery.MaxEvents = 4;
        long[] expected = EventLogBatchEngine.Read(syncQuery)
            .Select(static eventObject =>
                eventObject.RecordId ?? 0)
            .ToArray();

        var asyncQuery = EventLogBatchQuery.ForFiles(new[] {
            new EventLogFileQuery(path) {
                ReadMode = EventReadMode.Metadata,
                MaxEvents = 4
            }
        });
        asyncQuery.MaxEvents = 4;
        asyncQuery.MaxConcurrency = 2;
        var actual = new List<long>();
        await foreach (EventObject eventObject in
                       EventLogBatchEngine.ReadAsync(asyncQuery)) {
            actual.Add(eventObject.RecordId ?? 0);
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConsolidatedFileBatchDeduplicatesOverlappingNativeSelects() {
        if (!OperatingSystem.IsWindows()) return;
        string path = GetFixturePath();
        int eventId = EventLogEngine.ReadFile(
                new EventLogFileQuery(path) {
                    MaxEvents = 1,
                    ReadMode = EventReadMode.Metadata
                })
            .Single()
            .Id;

        EventLogFileQuery[] sources = Enumerable.Range(0, 2)
            .Select(_ => new EventLogFileQuery(path) {
                XPath = $"*[System[EventID={eventId}]]",
                ReadMode = EventReadMode.Metadata,
                MaxEvents = 16
            })
            .ToArray();
        var query = EventLogBatchQuery.ForFiles(sources);
        query.MaxEvents = 20;
        EventLogBatchQuery consolidated =
            EventLogBatchConsolidator.Consolidate(query);
        EventLogStructuredQuery structured =
            Assert.Single(consolidated.StructuredQueries);
        Assert.Single(
            System.Xml.Linq.XDocument
                .Parse(structured.QueryXml)
                .Descendants("Select"));

        EventObject[] actual =
            EventLogBatchEngine.Read(consolidated).ToArray();

        Assert.NotEmpty(actual);
        Assert.True(actual.Length <= 16);
        Assert.Equal(
            actual.Length,
            actual
                .Select(static item => item.RecordId)
                .Distinct()
                .Count());
    }

    [Fact]
    public void ConsolidationKeepsChannelAndFileQueriesOnSeparateNativeHandles() {
        if (!OperatingSystem.IsWindows()) return;
        string path = GetFixturePath();
        var channel = new EventLogChannelQuery("System") {
            XPath = "*[System[EventID=7040]]",
            ReadMode = EventReadMode.Metadata,
            MaxEvents = 3
        };
        var file = new EventLogFileQuery(path) {
            XPath = "*[System[EventID=7040]]",
            ReadMode = EventReadMode.Metadata,
            MaxEvents = 3
        };
        EventLogBatchQuery combined = EventLogBatchQuery.Combine(new[] {
            EventLogBatchQuery.ForChannels(new[] { channel }),
            EventLogBatchQuery.ForFiles(new[] { file })
        });

        EventLogBatchQuery consolidated =
            EventLogBatchConsolidator.Consolidate(combined);

        Assert.Equal(2, consolidated.StructuredQueries.Count);
        Assert.Contains(
            consolidated.StructuredQueries,
            static query =>
                query.SourceKind == EventLogQuerySourceKind.Channel);
        Assert.Contains(
            consolidated.StructuredQueries,
            static query =>
                query.SourceKind == EventLogQuerySourceKind.File);
        EventObject[] events =
            EventLogBatchEngine.Read(consolidated).ToArray();
        Assert.Contains(
            events,
            item => string.Equals(
                item.GatheredFrom,
                path,
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            events,
            item => string.Equals(
                item.ContainerLog,
                "System",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConsolidationPreservesFileSuppressExpressions() {
        if (!OperatingSystem.IsWindows()) return;
        string path = GetFixturePath();
        var select = new EventFilter {
            EventIds = new[] { 7040 }
        };
        var suppress = new EventFilter {
            ProviderNames = new[] { "Service Control Manager" }
        };
        var structured = new EventLogStructuredQuery(
            EventFilterCompiler.BuildFileQueryXml(
                new[] { path },
                select,
                suppress)) {
            SourceKind = EventLogQuerySourceKind.File,
            ReadMode = EventReadMode.Metadata
        };

        EventLogBatchQuery consolidated =
            EventLogBatchConsolidator.Consolidate(
                EventLogBatchQuery.ForStructured(
                    new[] { structured }));

        Assert.Empty(EventLogBatchEngine.Read(consolidated));
    }

    [Fact]
    public void ContinueOnErrorReportsOneSourceAndKeepsValidEvents() {
        if (!OperatingSystem.IsWindows()) return;
        string path = GetFixturePath();
        string missing = Path.Combine(
            Path.GetTempPath(),
            $"missing-{Guid.NewGuid():N}.evtx");
        var failures = new List<EventLogQueryFailure>();
        var query = EventLogBatchQuery.ForFiles(new[] {
            new EventLogFileQuery(path) {
                MaxEvents = 2,
                ReadMode = EventReadMode.Metadata
            },
            new EventLogFileQuery(missing) {
                MaxEvents = 2,
                ReadMode = EventReadMode.Metadata
            }
        });
        query.ContinueOnError = true;
        query.FailureHandler = failures.Add;

        EventObject[] actual = EventLogBatchEngine.Read(query).ToArray();

        Assert.Equal(2, actual.Length);
        EventLogQueryFailure failure = Assert.Single(failures);
        Assert.Equal(missing, failure.Source);
        Assert.IsType<FileNotFoundException>(failure.Exception);
    }

    [Fact]
    public void CancellationStopsTheMergedBatchEnumeration() {
        if (!OperatingSystem.IsWindows()) return;
        var query = EventLogBatchQuery.ForFiles(new[] {
            new EventLogFileQuery(GetFixturePath()) {
                Oldest = true,
                ReadMode = EventReadMode.Metadata
            }
        });
        using var cancellation = new CancellationTokenSource();
        int count = 0;

        Assert.Throws<OperationCanceledException>(() => {
            foreach (EventObject _ in EventLogBatchEngine.Read(
                         query,
                         cancellation.Token)) {
                count++;
                if (count == 3) {
                    cancellation.Cancel();
                }
            }
        });

        Assert.Equal(3, count);
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
}

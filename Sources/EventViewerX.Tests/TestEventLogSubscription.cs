using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventLogSubscription {
    [Fact]
    public void OldestSubscriptionDeliversAnExistingExactRecord() {
        if (!OperatingSystem.IsWindows()) return;
        var currentQuery = new EventLogChannelQuery("System") {
            ReadMode = EventReadMode.Metadata,
            MaxEvents = 1
        };
        EventObject current = EventLogEngine.ReadChannel(currentQuery).Single();
        Assert.True(current.RecordId.HasValue);

        using var delivered = new ManualResetEventSlim();
        EventObject? received = null;
        EventLogSubscriptionFailure? failure = null;
        var query = new EventLogSubscriptionQuery("System") {
            XPath = $"*[System[EventRecordID={current.RecordId.Value}]]",
            Start = EventLogSubscriptionStart.Oldest,
            ReadMode = EventReadMode.Metadata
        };
        using var subscription = new EventLogSubscription(
            query,
            eventObject => {
                received = eventObject;
                delivered.Set();
            },
            subscriptionFailure => {
                failure = subscriptionFailure;
                delivered.Set();
            });

        Assert.True(delivered.Wait(TimeSpan.FromSeconds(10)));
        Assert.Null(failure);
        Assert.NotNull(received);
        Assert.Equal(current.RecordId, received!.RecordId);
        Assert.Equal(1, subscription.EventsDelivered);
    }

    [Fact]
    public void OldestSubscriptionAcceptsStructuredNamedDataSuppression() {
        if (!OperatingSystem.IsWindows()) return;
        EventObject current = EventLogEngine.ReadChannel(
            new EventLogChannelQuery("System") {
                ReadMode = EventReadMode.Metadata,
                MaxEvents = 1
            }).Single();
        Assert.True(current.RecordId.HasValue);
        string queryXml =
            EventFilterCompiler.BuildChannelQueryXml(
                new[] { "System" },
                new EventFilter {
                    RecordIds = new[] {
                        current.RecordId.Value
                    },
                    ExcludedNamedData =
                        new Dictionary<string, IReadOnlyList<string>> {
                            ["FieldThatDoesNotExist"] =
                                new[] { "ExcludedValue" }
                        }
                });
        using var delivered = new ManualResetEventSlim();
        EventObject? received = null;
        EventLogSubscriptionFailure? failure = null;
        using var subscription = new EventLogSubscription(
            new EventLogSubscriptionQuery("System") {
                XPath = queryXml,
                Start = EventLogSubscriptionStart.Oldest,
                ReadMode = EventReadMode.Metadata
            },
            eventObject => {
                received = eventObject;
                delivered.Set();
            },
            subscriptionFailure => {
                failure = subscriptionFailure;
                delivered.Set();
            });

        Assert.True(delivered.Wait(TimeSpan.FromSeconds(10)));
        Assert.Null(failure);
        Assert.NotNull(received);
        Assert.Equal(current.RecordId, received!.RecordId);
    }

    [Fact]
    public void SubscriptionRejectsBookmarkWithoutBookmarkStart() {
        var query = new EventLogSubscriptionQuery("System") {
            BookmarkXml = "<BookmarkList />"
        };

        Assert.Throws<ArgumentException>(() =>
            new EventLogSubscription(query, _ => { }));
    }

    [Fact]
    public void FutureSubscriptionIsSignaledAndDeliversANewEvent() {
        if (!OperatingSystem.IsWindows()) return;
        if (!TestEnv.IsAdmin()) return;
        string suffix = Guid.NewGuid().ToString("N");
        string logName = $"EVXSubscription{suffix}";
        string sourceName = $"EVXSubscriptionSource{suffix}";
        try {
            ClassicEventLogManager.EnsureLog(
                new ClassicEventLogConfiguration {
                    LogName = logName,
                    SourceName = sourceName,
                    MaximumKilobytes = 256,
                    OverflowAction =
                        System.Diagnostics.OverflowAction
                            .OverwriteAsNeeded
                });
            using var delivered =
                new ManualResetEventSlim();
            EventObject? received = null;
            using var subscription =
                new EventLogSubscription(
                    new EventLogSubscriptionQuery(
                        logName) {
                        Start =
                            EventLogSubscriptionStart.Future,
                        XPath = "*[System[EventID=7001]]",
                        ReadMode =
                            EventReadMode.StructuredData,
                        BufferCapacity = 1
                    },
                    eventObject => {
                        received = eventObject;
                        delivered.Set();
                    });

            ClassicEventLogManager.Write(
                new ClassicEventWriteRequest {
                    LogName = logName,
                    SourceName = sourceName,
                    EventId = 7001,
                    Message = "future-subscription"
                });

            Assert.True(
                delivered.Wait(TimeSpan.FromSeconds(10)));
            Assert.NotNull(received);
            Assert.Equal(7001, received!.Id);
            Assert.Equal(logName, received.LogName);
        } finally {
            if (ClassicEventLogManager.LogExists(logName)) {
                ClassicEventLogManager.RemoveLog(logName);
            }
        }
    }

    [Fact]
    public void WatcherManagerAcceptsTheCompleteSubscriptionContract() {
        if (!OperatingSystem.IsWindows()) return;
        EventObject current = EventLogEngine.ReadChannel(
            new EventLogChannelQuery("System") {
                MaxEvents = 1,
                ReadMode = EventReadMode.Metadata
            }).Single();
        using var delivered = new ManualResetEventSlim();
        var query = new EventLogSubscriptionQuery("System") {
            XPath =
                $"*[System[EventRecordID={current.RecordId!.Value}]]",
            Start = EventLogSubscriptionStart.Oldest,
            ReadMode = EventReadMode.Metadata,
            MessageCulture =
                System.Globalization.CultureInfo.GetCultureInfo(
                    "en-US"),
            BufferCapacity = 8
        };

        WatcherInfo watcher = WatcherManager.StartWatcher(
            name: null,
            query,
            _ => delivered.Set(),
            stopAfter: 1);
        try {
            Assert.True(delivered.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(
                SpinWait.SpinUntil(
                    () => watcher.EndTime.HasValue,
                    TimeSpan.FromSeconds(10)));
            Assert.Equal(1, watcher.EventsFound);
            Assert.Equal(query.XPath, watcher.SubscriptionQuery.XPath);
            Assert.Equal(
                EventLogSubscriptionStart.Oldest,
                watcher.SubscriptionQuery.Start);
        } finally {
            WatcherManager.StopWatcher(watcher.Id);
        }
    }

    [Fact]
    public void WatcherManagerOwnsPartitionedSubscriptionsAsOneWatcher() {
        if (!OperatingSystem.IsWindows()) return;
        var queries = new[] {
            new EventLogSubscriptionQuery("System") {
                XPath = "*[System[EventID=1]]",
                ReadMode = EventReadMode.Metadata
            },
            new EventLogSubscriptionQuery("System") {
                XPath = "*[System[EventID=2]]",
                ReadMode = EventReadMode.Metadata
            }
        };

        WatcherInfo watcher = WatcherManager.StartWatcher(
            name: null,
            queries,
            _ => { });
        try {
            Assert.Equal(2, watcher.SubscriptionQueries.Count);
            Assert.Equal(
                queries.Select(static query => query.XPath),
                watcher.SubscriptionQueries.Select(
                    static query => query.XPath));
        } finally {
            WatcherManager.StopWatcher(watcher.Id);
        }
    }
}

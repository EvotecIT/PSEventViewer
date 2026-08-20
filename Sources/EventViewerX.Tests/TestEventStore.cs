using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
using DBAClientX;
using EventViewerX.Reporting;
using EventViewerX.Storage;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventStore {
    [Fact]
    public async Task WriteIsIdempotentAndCommitsCheckpointWithRows() {
        string path = CreateStorePath();
        try {
            EventReport report = CreateReport(
                (new DateTime(2026, 7, 31, 23, 0, 0, DateTimeKind.Utc), 41, "alice"),
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "bob"));
            var store = new EventStore(path);
            var checkpoint = new EventStoreCheckpoint {
                Consumer = "weekly-report",
                Computer = "WEC01",
                Container = "ForwardedEvents",
                RecordId = 42,
                BookmarkXml = "<Bookmark/>"
            };

            EventStoreWriteResult first = await store.WriteAsync(report, checkpoint);
            EventStoreWriteResult second = await store.WriteAsync(report, checkpoint);
            EventStoreCheckpoint saved = Assert.IsType<EventStoreCheckpoint>(
                await store.GetCheckpointAsync("weekly-report", "WEC01", "ForwardedEvents"));

            Assert.Equal(2, first.Inserted);
            Assert.Equal(0, second.Inserted);
            Assert.Equal(2, second.Duplicates);
            Assert.True(first.CheckpointCommitted);
            Assert.Equal(42, saved.RecordId);
            Assert.Equal("<Bookmark/>", saved.BookmarkXml);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task OriginalEventIdentityDeduplicatesDirectAndCollectorTransport() {
        string path = CreateStorePath();
        try {
            DateTime time = new(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc);
            var store = new EventStore(path);
            EventStoreWriteResult forwarded = await store.WriteAsync(
                CreateReportFromTransport(time, 42, "alice", "WEC01", "ForwardedEvents"));
            EventStoreWriteResult direct = await store.WriteAsync(
                CreateReportFromTransport(time, 42, "alice", "source.ad.evotec.xyz", "Security"));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery());

            Assert.Equal(1, forwarded.Inserted);
            Assert.Equal(0, direct.Inserted);
            Assert.Equal(1, direct.Duplicates);
            Assert.Single(report.Rows);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task ReadUsesTypedPredicatesAndRetainsHomogeneousSchema() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice"),
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob")));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery {
                DefinitionNames = new[] { "StoredLogon" },
                Predicate = EventPredicate.Compare("User", EventPredicateOperator.Equal, "bob")
            });

            EventReportRow row = Assert.Single(report.Rows);
            Assert.Equal("bob", row.Values["User"]);
            EventReportSection section = Assert.Single(report.Sections);
            Assert.Equal(EventReportSectionKind.Custom, section.Kind);
            Assert.Equal(new[] { "User", "Computer" }, section.Columns.Select(static column => column.Name));
            Assert.DoesNotContain(section.Columns, static column => column.Name == nameof(EventReportRow.EventId));
            Assert.Equal(2, report.EventsScanned);
            Assert.False(report.ScanLimitReached);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task StoredPredicatesUseLiveCommonFieldNamesAndNumericLevel() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice")));
            EventPredicate predicate = EventPredicate.AllOf(
                EventPredicate.Compare("ProviderName", EventPredicateOperator.Equal,
                    "Microsoft-Windows-Security-Auditing"),
                EventPredicate.Compare("SourceLogName", EventPredicateOperator.Equal, "Security"),
                EventPredicate.Compare("MachineName", EventPredicateOperator.Equal, "source.ad.evotec.xyz"),
                EventPredicate.Compare("TypeName", EventPredicateOperator.Equal, "StoredLogon"),
                EventPredicate.Compare("Level", EventPredicateOperator.Equal, 0));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery {
                Predicate = predicate
            });

            EventReportRow row = Assert.Single(report.Rows);
            Assert.Equal((byte)0, row.LevelValue);
            Assert.Equal("Information", row.Level);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task StoredCustomFieldsShadowNativeSqlColumnsWithoutLosingMatches() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateShadowedProviderReport(
                new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc),
                42));

            EventReport customMatch = await store.ReadReportAsync(new EventStoreQuery {
                DefinitionNames = new[] { "StoredShadowedProvider" },
                Predicate = EventPredicate.Compare(
                    "ProviderName",
                    EventPredicateOperator.Equal,
                    "custom-provider")
            });
            EventReport nativeValueDoesNotMatch = await store.ReadReportAsync(new EventStoreQuery {
                DefinitionNames = new[] { "StoredShadowedProvider" },
                Predicate = EventPredicate.Compare(
                    "ProviderName",
                    EventPredicateOperator.Equal,
                    "Microsoft-Windows-Security-Auditing")
            });
            EventStoreSummaryResult summary = await store.SummarizeAsync(
                new EventStoreQuery {
                    DefinitionNames = new[] { "StoredShadowedProvider" },
                    Predicate = EventPredicate.Compare(
                        "ProviderName",
                        EventPredicateOperator.Equal,
                        "custom-provider")
                },
                EventStoreSummaryPeriod.Day);
            await store.WriteAsync(CreateGenericReport(
                new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc),
                43,
                "ProviderName",
                "generic-provider"));
            EventReport genericMatch = await store.ReadReportAsync(new EventStoreQuery {
                DefinitionNames = new[] { "Generic" },
                Predicate = EventPredicate.Compare(
                    "ProviderName",
                    EventPredicateOperator.Equal,
                    "generic-provider")
            });

            EventReportRow row = Assert.Single(customMatch.Rows);
            Assert.Equal("custom-provider", row.Values["ProviderName"]);
            Assert.Empty(nativeValueDoesNotMatch.Rows);
            Assert.Equal(1, Assert.Single(summary.Rows).Count);
            Assert.Equal("generic-provider", Assert.Single(genericMatch.Rows).Values["ProviderName"]);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task UnlimitedCandidateScanAndContradictoryPredicateStayExact() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice"),
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob")));

            EventReport unlimited = await store.ReadReportAsync(new EventStoreQuery {
                Predicate = EventPredicate.Compare("User", EventPredicateOperator.IsNotNull),
                MaxCandidates = 0,
                Oldest = true
            });
            EventReport contradictory = await store.ReadReportAsync(new EventStoreQuery {
                Predicate = EventPredicate.AllOf(
                    EventPredicate.Compare("EventId", EventPredicateOperator.Equal, 4624),
                    EventPredicate.Compare("EventId", EventPredicateOperator.Equal, 4625))
            });

            Assert.Equal(2, unlimited.Rows.Count);
            Assert.Equal(2, unlimited.EventsScanned);
            Assert.False(unlimited.ScanLimitReached);
            Assert.Empty(contradictory.Rows);
            Assert.Equal(2, contradictory.EventsScanned);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public void StoredSchemaTypeNamesAreRuntimeNeutralAndResolvable() {
        Type[] values = {
            typeof(string),
            typeof(int?),
            typeof(DateTime[]),
            typeof(Dictionary<string, List<int?[]>>)
        };

        foreach (Type value in values) {
            string stableName = EventReportColumnSchema.GetStableTypeName(value);
            Assert.DoesNotContain("mscorlib", stableName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("System.Private.CoreLib", stableName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Version=", stableName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(value, EventReportColumnSchema.ResolveValueTypeName(stableName));
        }

        const string privateCoreString =
            "System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
        const string mscorlibNullable =
            "System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        Assert.Equal(typeof(string), EventReportColumnSchema.ResolveValueTypeName(privateCoreString));
        Assert.Equal(typeof(int?), EventReportColumnSchema.ResolveValueTypeName(mscorlibNullable));
        Assert.Equal("System.String", EventReportColumnSchema.NormalizeValueTypeName(privateCoreString));
    }

    [Fact]
    public async Task LegacyRuntimeSpecificSchemaIsMigratedWithoutPruningRows() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice")));
            const string privateCoreString =
                "System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
            using (var sqlite = new SQLite { BusyTimeoutMs = 10000 }) {
                using SQLiteSession session = sqlite.OpenSession(path);
                string schemaJson = Convert.ToString(session.ExecuteScalar(
                    "SELECT schema_json FROM evx_definitions WHERE definition_name = 'StoredLogon';"),
                    System.Globalization.CultureInfo.InvariantCulture)!;
                string legacyJson = schemaJson.Replace("System.String", privateCoreString);
                session.ExecuteNonQuery(
                    "UPDATE evx_definitions SET schema_hash = $hash, schema_json = $schema WHERE definition_name = 'StoredLogon';",
                    new Dictionary<string, object?> {
                        ["$hash"] = "legacy-runtime-specific-hash",
                        ["$schema"] = legacyJson
                    });
            }

            EventStoreWriteResult migrated = await new EventStore(path).WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob")));
            EventReport report = await new EventStore(path).ReadReportAsync(new EventStoreQuery { Oldest = true });
            string storedSchema;
            using (var sqlite = new SQLite { BusyTimeoutMs = 10000 }) {
                using SQLiteSession session = sqlite.OpenSession(path);
                storedSchema = Convert.ToString(session.ExecuteScalar(
                    "SELECT schema_json FROM evx_definitions WHERE definition_name = 'StoredLogon';"),
                    System.Globalization.CultureInfo.InvariantCulture)!;
            }

            Assert.Equal(1, migrated.Inserted);
            Assert.Equal(2, report.Rows.Count);
            Assert.DoesNotContain("System.Private.CoreLib", storedSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Version=", storedSchema, StringComparison.OrdinalIgnoreCase);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task CompositeTypeQueriesExpandToStoredLeafDefinitions() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReportForDefinition(
                "ADUserLogonFailed",
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice")));
            await store.WriteAsync(CreateReportForDefinition(
                "ADUserLogon",
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob")));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery {
                Types = new[] { EventType.ActiveDirectoryAuthentication },
                Oldest = true
            });

            Assert.Equal(2, report.Rows.Count);
            Assert.Equal(
                new[] { "ADUserLogonFailed", "ADUserLogon" },
                report.Rows.Select(static row => row.Type));
            Assert.Equal(2, report.Sections.Count);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task CandidateLimitIsExplicitAndNeverCheckpointedByRead() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice"),
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob")));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery {
                Predicate = EventPredicate.Compare("User", EventPredicateOperator.Equal, "nobody"),
                MaxCandidates = 1,
                Oldest = true
            });

            Assert.Empty(report.Rows);
            Assert.Equal(1, report.EventsScanned);
            Assert.True(report.ScanLimitReached);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task CalendarSummariesUseSqlFastPathAndPruneSelectively() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 7, 31, 23, 0, 0, DateTimeKind.Utc), 41, "alice"),
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "bob"),
                (new DateTime(2026, 8, 3, 1, 0, 0, DateTimeKind.Utc), 43, "carol")));

            EventStoreSummaryResult monthly = await store.SummarizeAsync(
                new EventStoreQuery(),
                EventStoreSummaryPeriod.Month);
            Assert.Equal(2, monthly.Rows.Count);
            Assert.Equal(3, monthly.EventsScanned);
            Assert.Equal(new[] { 1L, 2L }, monthly.Rows.Select(static row => row.Count));

            int deleted = await store.PruneBeforeAsync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            EventReport remaining = await store.ReadReportAsync(new EventStoreQuery());
            Assert.Equal(1, deleted);
            Assert.Equal(2, remaining.Rows.Count);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task CalendarSummariesRejectPartialResultLimitsAndReturnUtcBuckets() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 3, 1, 0, 0, DateTimeKind.Utc), 42, "alice")));

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => store.SummarizeAsync(
                new EventStoreQuery { MaxEvents = 1 },
                EventStoreSummaryPeriod.Day));
            EventStoreSummaryResult weekly = await store.SummarizeAsync(
                new EventStoreQuery {
                    Predicate = EventPredicate.Compare("User", EventPredicateOperator.Equal, "alice")
                },
                EventStoreSummaryPeriod.Week);

            Assert.Contains("exhaustive", exception.Message, StringComparison.OrdinalIgnoreCase);
            EventStoreSummaryRow row = Assert.Single(weekly.Rows);
            Assert.Equal(DateTimeKind.Utc, row.PeriodStartUtc.Kind);
            Assert.Equal(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc), row.PeriodStartUtc);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task GenericSchemasCanEvolveWithObservedEventData() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateGenericReport(
                new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc),
                42,
                "FirstField",
                "one"));
            await store.WriteAsync(CreateGenericReport(
                new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc),
                43,
                "SecondField",
                "two"));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery { Oldest = true });

            Assert.Equal(2, report.Rows.Count);
            EventReportSection section = Assert.Single(report.Sections);
            Assert.Equal(EventReportSectionKind.Generic, section.Kind);
            Assert.Contains(section.Columns, static column => column.Name == "FirstField");
            Assert.Contains(section.Columns, static column => column.Name == "SecondField");
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task IncompatibleSchemaRollsBackRowsAndCheckpoint() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice")));
            EventReport incompatible = CreateReportWithChangedSchema(
                new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc),
                43);
            var checkpoint = new EventStoreCheckpoint {
                Consumer = "schema-race",
                Computer = "WEC01",
                Container = "ForwardedEvents",
                RecordId = 43
            };

            await Assert.ThrowsAsync<InvalidDataException>(() => store.WriteAsync(incompatible, checkpoint));
            EventReport remaining = await store.ReadReportAsync(new EventStoreQuery());
            EventStoreCheckpoint? saved = await store.GetCheckpointAsync(
                "schema-race", "WEC01", "ForwardedEvents");

            Assert.Single(remaining.Rows);
            Assert.Null(saved);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task ConcurrentWritersRemainIdempotent() {
        string path = CreateStorePath();
        try {
            EventReport report = CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice"),
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob"));
            EventStoreWriteResult[] results = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(_ => new EventStore(path).WriteAsync(report)));

            EventReport stored = await new EventStore(path).ReadReportAsync(new EventStoreQuery());

            Assert.Equal(2, results.Sum(static result => result.Inserted));
            Assert.Equal(2, stored.Rows.Count);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public void StoredPlanDistinguishesSqlPrefilterAndManagedVerification() {
        EventStoreQueryPlan plan = EventStore.Plan(new EventStoreQuery {
            EventIds = new[] { 4624 },
            Predicate = EventPredicate.AllOf(
                EventPredicate.Compare("Provider", EventPredicateOperator.Equal, "Security"),
                EventPredicate.Compare("User", EventPredicateOperator.Equal, "alice"))
        });

        Assert.True(plan.HasSqlPredicatePrefilter);
        Assert.True(plan.HasManagedVerification);
        Assert.Contains(plan.Steps, static step =>
            step.Stage == EventStoreQueryPlanStage.Sql && step.Expression.Contains("Provider", StringComparison.Ordinal));
        Assert.Contains(plan.Steps, static step =>
            step.Stage == EventStoreQueryPlanStage.Managed && step.Expression.Contains("User", StringComparison.Ordinal));
    }

    private static EventReport CreateReport(params (DateTime Time, long RecordId, string User)[] events) {
        return CreateReportFromTransport(events, "WEC01", "ForwardedEvents");
    }

    private static EventReport CreateReportForDefinition(
        string definitionName,
        params (DateTime Time, long RecordId, string User)[] events) {

        return CreateReportFromTransport(events, "WEC01", "ForwardedEvents", definitionName);
    }

    private static EventReport CreateReportFromTransport(
        DateTime time,
        long recordId,
        string user,
        string queriedMachine,
        string container) => CreateReportFromTransport(
            new[] { (time, recordId, user) },
            queriedMachine,
            container);

    private static EventReport CreateReportFromTransport(
        IReadOnlyList<(DateTime Time, long RecordId, string User)> events,
        string queriedMachine,
        string container,
        string definitionName = "StoredLogon") {

        EventDefinition definition = new() {
            Name = definitionName,
            DisplayName = "Stored logons",
            Description = "Stored logon test events.",
            Sources = new[] {
                new EventDefinitionSource { LogName = "Security", EventIds = new[] { 4624 } }
            },
            Fields = new[] {
                new EventDefinitionField { Name = "User", Source = EventFieldSource.Data, SourceName = "TargetUserName" },
                new EventDefinitionField { Name = "Computer", Source = EventFieldSource.Metadata, SourceName = "SourceComputer" }
            }
        };
        object[] records = events.Select(item => {
            var source = new EventObject(
                new SyntheticEventRecord(item.Time, item.RecordId),
                queriedMachine,
                EventReadMode.StructuredDataAndMessage) {
                ContainerLog = container,
                GatheredLogName = container
            };
            source.Data["TargetUserName"] = item.User;
            return (object)EventDefinitionEngine.CreateRecord(definition, source);
        }).ToArray();
        return EventReportEngine.Create(records, "Stored logons");
    }

    private static EventReport CreateReportWithChangedSchema(DateTime time, long recordId) {
        EventDefinition definition = new() {
            Name = "StoredLogon",
            Sources = new[] {
                new EventDefinitionSource { LogName = "Security", EventIds = new[] { 4624 } }
            },
            Fields = new[] {
                new EventDefinitionField { Name = "DifferentField", Source = EventFieldSource.Data, SourceName = "TargetUserName" }
            }
        };
        var source = new EventObject(
            new SyntheticEventRecord(time, recordId),
            "WEC01",
            EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };
        source.Data["TargetUserName"] = "bob";
        return EventReportEngine.Create(new object[] {
            EventDefinitionEngine.CreateRecord(definition, source)
        });
    }

    private static EventReport CreateShadowedProviderReport(DateTime time, long recordId) {
        EventDefinition definition = new() {
            Name = "StoredShadowedProvider",
            DisplayName = "Stored provider shadow",
            Sources = new[] {
                new EventDefinitionSource { LogName = "Security", EventIds = new[] { 4624 } }
            },
            Fields = new[] {
                new EventDefinitionField {
                    Name = "ProviderName",
                    Source = EventFieldSource.Data,
                    SourceName = "CustomProvider"
                }
            }
        };
        var source = new EventObject(
            new SyntheticEventRecord(time, recordId),
            "WEC01",
            EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };
        source.Data["CustomProvider"] = "custom-provider";
        return EventReportEngine.Create(new object[] {
            EventDefinitionEngine.CreateRecord(definition, source)
        });
    }

    private static EventReport CreateGenericReport(
        DateTime time,
        long recordId,
        string field,
        string value) {

        var source = new EventObject(
            new SyntheticEventRecord(time, recordId),
            "WEC01",
            EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };
        source.Data[field] = value;
        return EventReportEngine.Create(new object[] { source }, "Generic events");
    }

    private static string CreateStorePath() => Path.Combine(
        Path.GetTempPath(),
        $"eventviewerx-store-{Guid.NewGuid():N}.db");

    private static void DeleteStore(string path) {
        foreach (string candidate in new[] { path, path + "-wal", path + "-shm" }) {
            if (File.Exists(candidate)) {
                File.Delete(candidate);
            }
        }
    }

    private sealed class SyntheticEventRecord : EventRecord {
        private readonly DateTime _time;
        private readonly long _recordId;

        internal SyntheticEventRecord(DateTime time, long recordId) {
            _time = time;
            _recordId = recordId;
        }

        public override string ProviderName => "Microsoft-Windows-Security-Auditing";
        public override string LogName => "Security";
        public override string MachineName => "source.ad.evotec.xyz";
        public override int Id => 4624;
        public override byte? Level => 0;
        public override int? Task => 12544;
        public override long? Keywords => 0;
        public override IEnumerable<string> KeywordsDisplayNames => Array.Empty<string>();
        public override short? Opcode => 0;
        public override string OpcodeDisplayName => string.Empty;
        public override string TaskDisplayName => string.Empty;
        public override Guid? ProviderId => null;
        public override Guid? ActivityId => null;
        public override Guid? RelatedActivityId => null;
        public override int? ProcessId => 1;
        public override int? ThreadId => 1;
        public override string LevelDisplayName => "Information";
        public override IList<EventProperty> Properties => Array.Empty<EventProperty>();
        public override DateTime? TimeCreated => _time;
        public override int? Qualifiers => null;
        public override long? RecordId => _recordId;
        public override byte? Version => 0;
        public override SecurityIdentifier UserId => null!;
        public override EventBookmark Bookmark => null!;
        public override string FormatDescription() => "An account was logged on.";
        public override string FormatDescription(IEnumerable<object> values) => FormatDescription();
        public override string ToXml() => "<Event><EventData /></Event>";
    }
}

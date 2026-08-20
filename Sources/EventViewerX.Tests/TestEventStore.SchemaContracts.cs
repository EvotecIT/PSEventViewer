using DBAClientX;
using EventViewerX.Reporting;
using EventViewerX.Storage;
using Xunit;

namespace EventViewerX.Tests;

public sealed partial class TestEventStore {
    [Fact]
    public async Task GenericSchemaEvolutionPreservesStringValuesAndObservedColumns() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateGenericReport(
                new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc),
                42,
                "IsoText",
                "2026-08-20T10:00:00Z"));
            await store.WriteAsync(CreateGenericReport(
                new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc),
                43,
                "SecondField",
                "two"));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery { Oldest = true });
            string schemaJson;
            using (var sqlite = new SQLite { BusyTimeoutMs = 10000 }) {
                using SQLiteSession session = sqlite.OpenSession(path);
                schemaJson = Convert.ToString(session.ExecuteScalar(
                    "SELECT schema_json FROM evx_definitions WHERE definition_name = 'Generic';"),
                    System.Globalization.CultureInfo.InvariantCulture)!;
            }

            EventReportRow first = report.Rows[0];
            Assert.IsType<string>(first.Values["IsoText"]);
            Assert.Equal("2026-08-20T10:00:00Z", first.Values["IsoText"]);
            Assert.Contains("IsoText", schemaJson, StringComparison.Ordinal);
            Assert.Contains("SecondField", schemaJson, StringComparison.Ordinal);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task ExistingStoresAcquireIndexedTransportIdentityWithoutLosingIdempotence() {
        string path = CreateStorePath();
        try {
            EventReport report = CreateReport(
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice"));
            await new EventStore(path).WriteAsync(report);
            using (var sqlite = new SQLite { BusyTimeoutMs = 10000 }) {
                using SQLiteSession session = sqlite.OpenSession(path);
                session.ExecuteNonQuery("DROP INDEX ix_evx_events_original_transport;");
                session.ExecuteNonQuery("ALTER TABLE evx_events DROP COLUMN original_event_key;");
                session.ExecuteNonQuery("ALTER TABLE evx_events DROP COLUMN transport_kind;");
            }

            var migratedStore = new EventStore(path);
            EventStoreWriteResult duplicate = await migratedStore.WriteAsync(report);
            EventReport stored = await migratedStore.ReadReportAsync(new EventStoreQuery());

            Assert.Equal(0, duplicate.Inserted);
            Assert.Single(stored.Rows);
            using var verificationClient = new SQLite { BusyTimeoutMs = 10000 };
            using SQLiteSession verification = verificationClient.OpenSession(path);
            IReadOnlyList<string> columns = verification.QueryAsList(
                "PRAGMA table_info(evx_events);",
                static record => record.GetString(1));
            Assert.Contains("original_event_key", columns);
            Assert.Contains("transport_kind", columns);
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task GenericEventDataCannotDuplicateCommonReportColumns() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateGenericReport(
                new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc),
                42,
                nameof(EventReportRow.EventId),
                "provider-event-id"));

            EventReport report = await store.ReadReportAsync(new EventStoreQuery());
            EventReport metadataMatch = await store.ReadReportAsync(new EventStoreQuery {
                Predicate = EventPredicate.Compare(
                    nameof(EventReportRow.EventId),
                    EventPredicateOperator.Equal,
                    4624)
            });
            EventReportRow row = Assert.Single(report.Rows);
            EventReportSection section = Assert.Single(report.Sections);

            Assert.Equal(4624, row.EventId);
            Assert.Equal("provider-event-id", row.Values[nameof(EventReportRow.EventId)]);
            Assert.Equal((byte)0, row.ToDictionary()[nameof(EventReportRow.LevelValue)]);
            Assert.Single(metadataMatch.Rows);
            Assert.Single(section.Columns, static column =>
                string.Equals(column.Name, nameof(EventReportRow.EventId), StringComparison.OrdinalIgnoreCase));
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public void StoredReportsRejectAmbiguousDefinitionAndColumnSchemas() {
        var row = new EventReportRow {
            TimeCreated = new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc),
            Type = "Collision",
            Values = new Dictionary<string, object?> { ["First"] = "one" }
        };
        EventReportSectionSchema first = CreateSchema("Collision", "First");
        EventReportSectionSchema second = CreateSchema("collision", "Second");
        EventReportSectionSchema duplicateColumns = new() {
            Name = "DuplicateColumns",
            Kind = EventReportSectionKind.Custom,
            Columns = new[] {
                CreateColumn("Value", typeof(string)),
                CreateColumn("value", typeof(int))
            }
        };

        ArgumentException definition = Assert.Throws<ArgumentException>(() =>
            EventReportEngine.CreateStored(new[] { row }, new[] { first, second }));
        ArgumentException columns = Assert.Throws<ArgumentException>(() =>
            EventReportEngine.CreateStored(
                new[] { new EventReportRow { Type = "DuplicateColumns" } },
                new[] { duplicateColumns }));

        Assert.Contains("duplicate", definition.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("duplicate", columns.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StoredReportsRejectUndefinedSectionKinds() {
        var row = new EventReportRow {
            Type = "InvalidKind"
        };
        EventReportSectionSchema schema = new() {
            Name = "InvalidKind",
            Kind = (EventReportSectionKind)999,
            Columns = Array.Empty<EventReportColumnSchema>()
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            EventReportEngine.CreateStored(new[] { row }, new[] { schema }));

        Assert.Contains("undefined", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StoredReportsNormalizeCompatibleValuesAndRejectIncompatibleSchemaValues() {
        EventReportSectionSchema schema = new() {
            Name = "TypedValues",
            Kind = EventReportSectionKind.Custom,
            Columns = new[] { CreateColumn("AttemptCount", typeof(int)) }
        };
        var compatible = new EventReportRow {
            Type = "TypedValues",
            Values = new Dictionary<string, object?> { ["AttemptCount"] = "7" }
        };
        var incompatible = new EventReportRow {
            Type = "TypedValues",
            Values = new Dictionary<string, object?> { ["AttemptCount"] = "not-an-integer" }
        };

        EventReport normalized = EventReportEngine.CreateStored(new[] { compatible }, new[] { schema });
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            EventReportEngine.CreateStored(new[] { incompatible }, new[] { schema }));

        Assert.Equal(7, Assert.IsType<int>(Assert.Single(normalized.Rows).Values["AttemptCount"]));
        Assert.Contains("AttemptCount", exception.Message, StringComparison.Ordinal);
        Assert.Contains("System.Int32", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StoreRevalidatesAndNormalizesMutableRowsAtThePersistenceBoundary() {
        string path = CreateStorePath();
        try {
            EventReportSectionSchema schema = new() {
                Name = "MutableValues",
                Kind = EventReportSectionKind.Custom,
                Columns = new[] { CreateColumn("AttemptCount", typeof(int)) }
            };
            EventReport report = EventReportEngine.CreateStored(
                new[] {
                    new EventReportRow {
                        Type = "MutableValues",
                        Values = new Dictionary<string, object?> { ["AttemptCount"] = "7" }
                    }
                },
                new[] { schema });
            report.Rows[0].Values = new Dictionary<string, object?> {
                ["AttemptCount"] = "not-an-integer"
            };
            var store = new EventStore(path);

            ArgumentException invalid = await Assert.ThrowsAsync<ArgumentException>(() =>
                store.WriteAsync(report));

            Assert.Contains("AttemptCount", invalid.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(path));

            report.Rows[0].Values = new Dictionary<string, object?> { ["AttemptCount"] = "8" };
            EventStoreWriteResult written = await store.WriteAsync(report);
            EventReport stored = await store.ReadReportAsync(new EventStoreQuery());

            Assert.Equal(1, written.Inserted);
            Assert.Equal(8, Assert.IsType<int>(Assert.Single(stored.Rows).Values["AttemptCount"]));
        } finally {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task DerivedSummaryReportsCannotBeWrittenBackIntoEventHistory() {
        string path = CreateStorePath();
        try {
            var store = new EventStore(path);
            await store.WriteAsync(CreateReportForDefinition(
                "FirstDefinition",
                (new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), 42, "alice")));
            await store.WriteAsync(CreateReportForDefinition(
                "SecondDefinition",
                (new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc), 43, "bob")));
            EventReport summary = await store.CreateSummaryReportAsync(
                new EventStoreQuery(),
                EventStoreSummaryPeriod.Day);

            InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                store.WriteAsync(summary));

            Assert.Contains("summary", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, (await store.ReadReportAsync(new EventStoreQuery())).Rows.Count);
        } finally {
            DeleteStore(path);
        }
    }

    private static EventReportSectionSchema CreateSchema(string name, string column) => new() {
        Name = name,
        DisplayName = name,
        Kind = EventReportSectionKind.Custom,
        Columns = new[] { CreateColumn(column, typeof(string)) }
    };

    private static EventReportColumnSchema CreateColumn(string name, Type type) => new() {
        Name = name,
        DisplayName = name,
        ValueTypeName = EventReportColumnSchema.GetStableTypeName(type)
    };
}

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

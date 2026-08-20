using System.Text.Json;
using System.Text.Json.Serialization;
using DBAClientX;

namespace EventViewerX.Storage;

/// <summary>Optional local SQLite history store backed by the shared DbaClientX provider layer.</summary>
public sealed partial class EventStore {
    private const int SchemaVersion = 1;
    private readonly object _initializationLock = new();
    private bool _initialized;

    /// <summary>Creates a local event store for one SQLite database path.</summary>
    public EventStore(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Store path cannot be empty.", nameof(path));
        }
        Path = System.IO.Path.GetFullPath(path);
    }

    /// <summary>Absolute SQLite database path.</summary>
    public string Path { get; }

    /// <summary>Creates or validates the current storage schema.</summary>
    public void Initialize() {
        lock (_initializationLock) {
            if (_initialized) {
                return;
            }
            string? directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory!);
            }
            using var sqlite = new SQLite { BusyTimeoutMs = 10000 };
            using SQLiteSession session = sqlite.OpenSession(Path);
            session.ExecuteNonQuery("PRAGMA journal_mode=WAL;");
            session.ExecuteNonQuery("PRAGMA synchronous=NORMAL;");
            session.ExecuteNonQuery(SchemaSql);
            object? version = session.ExecuteScalar(
                "SELECT schema_version FROM evx_store_metadata WHERE singleton_id = 1;");
            if (Convert.ToInt32(version, CultureInfo.InvariantCulture) != SchemaVersion) {
                throw new InvalidDataException(
                    $"Event store schema version '{version}' is not supported by this EventViewerX build.");
            }
            _initialized = true;
        }
    }

    private void EnsureInitialized() {
        if (!_initialized) {
            Initialize();
        }
    }

    private static JsonSerializerOptions CreateJsonOptions() {
        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS evx_store_metadata (
    singleton_id INTEGER NOT NULL PRIMARY KEY CHECK (singleton_id = 1),
    schema_version INTEGER NOT NULL,
    created_utc TEXT NOT NULL
);
INSERT OR IGNORE INTO evx_store_metadata (singleton_id, schema_version, created_utc)
VALUES (1, 1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'));

CREATE TABLE IF NOT EXISTS evx_definitions (
    definition_name TEXT NOT NULL PRIMARY KEY COLLATE NOCASE,
    display_name TEXT NOT NULL,
    description TEXT NOT NULL,
    kind INTEGER NOT NULL,
    schema_hash TEXT NOT NULL,
    schema_json TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS evx_events (
    event_key TEXT NOT NULL PRIMARY KEY,
    definition_name TEXT NOT NULL COLLATE NOCASE,
    event_time_utc TEXT NOT NULL,
    event_id INTEGER NOT NULL,
    record_id INTEGER NULL,
    provider TEXT NOT NULL,
    source_log TEXT NOT NULL,
    container_log TEXT NOT NULL,
    source_computer TEXT NOT NULL,
    collector_computer TEXT NOT NULL,
    level TEXT NOT NULL,
    level_value INTEGER NULL,
    message TEXT NOT NULL,
    values_json TEXT NOT NULL,
    inserted_utc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_evx_events_time ON evx_events (event_time_utc);
CREATE INDEX IF NOT EXISTS ix_evx_events_definition_time ON evx_events (definition_name, event_time_utc);
CREATE INDEX IF NOT EXISTS ix_evx_events_source_time ON evx_events (source_computer, source_log, event_time_utc);
CREATE INDEX IF NOT EXISTS ix_evx_events_event_id_time ON evx_events (event_id, event_time_utc);

CREATE TABLE IF NOT EXISTS evx_checkpoints (
    consumer TEXT NOT NULL COLLATE NOCASE,
    computer TEXT NOT NULL COLLATE NOCASE,
    container TEXT NOT NULL COLLATE NOCASE,
    record_id INTEGER NULL,
    bookmark_xml TEXT NULL,
    updated_utc TEXT NOT NULL,
    PRIMARY KEY (consumer, computer, container)
);";
}

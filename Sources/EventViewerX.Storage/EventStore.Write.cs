using System.Security.Cryptography;
using System.Text.Json;
using DBAClientX;
using EventViewerX.Reporting;

namespace EventViewerX.Storage;

public sealed partial class EventStore {
    /// <summary>Stores one normalized report and optional checkpoint in a single transaction.</summary>
    public async Task<EventStoreWriteResult> WriteAsync(
        EventReport report,
        EventStoreCheckpoint? checkpoint = null,
        CancellationToken cancellationToken = default) {

        if (report == null) {
            throw new ArgumentNullException(nameof(report));
        }
        ValidateCheckpoint(checkpoint);
        EnsureInitialized();
        EventReportRow[] rows = report.Rows.ToArray();
        EventReportSectionSchema[] schemas = report.Sections
            .Select(EventReportSectionSchema.FromSection)
            .ToArray();
        var schemaNames = new HashSet<string>(schemas.Select(static schema => schema.Name),
            StringComparer.OrdinalIgnoreCase);
        if (rows.Any(row => !schemaNames.Contains(row.Type) &&
                            !string.Equals(row.Type, "Generic", StringComparison.OrdinalIgnoreCase))) {
            throw new InvalidDataException("Every stored typed row must have a matching homogeneous report schema.");
        }

        using var sqlite = new SQLite { BusyTimeoutMs = 10000 };
        await using SQLiteAsyncSession session = await sqlite
            .OpenSessionAsync(Path, cancellationToken)
            .ConfigureAwait(false);
        return await session.RunInTransactionAsync(async (transaction, token) => {
            string updatedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            foreach (EventReportSectionSchema schema in schemas) {
                string schemaHash = CreateSchemaHash(schema);
                IReadOnlyList<StoredDefinitionSchema> existingDefinitions = await transaction.QueryAsListAsync(
                    "SELECT schema_hash, schema_json FROM evx_definitions WHERE definition_name = $name;",
                    static record => new StoredDefinitionSchema(record.GetString(0), record.GetString(1)),
                    new Dictionary<string, object?> { ["$name"] = schema.Name },
                    cancellationToken: token).ConfigureAwait(false);
                StoredDefinitionSchema? existingDefinition = existingDefinitions.Count == 0
                    ? null
                    : existingDefinitions[0];
                if (existingDefinition != null &&
                    !string.Equals(existingDefinition.Hash, schemaHash, StringComparison.Ordinal) &&
                    !HasEquivalentSchema(existingDefinition.Json, schemaHash)) {
                    object? existingRows = await transaction.ExecuteScalarAsync(
                        "SELECT COUNT(*) FROM evx_events WHERE definition_name = $name;",
                        new Dictionary<string, object?> { ["$name"] = schema.Name },
                        token).ConfigureAwait(false);
                    if (Convert.ToInt64(existingRows, CultureInfo.InvariantCulture) > 0) {
                        throw new InvalidDataException(
                            $"Stored definition '{schema.Name}' has a different column schema. " +
                            "Prune its existing rows or use a new definition name before writing the revised schema.");
                    }
                }
                await transaction.ExecuteNonQueryAsync(
                    UpsertDefinitionSql,
                    new Dictionary<string, object?> {
                        ["$name"] = schema.Name,
                        ["$display"] = schema.DisplayName,
                        ["$description"] = schema.Description,
                        ["$kind"] = (int)schema.Kind,
                        ["$schemaHash"] = schemaHash,
                        ["$schema"] = JsonSerializer.Serialize(schema, JsonOptions),
                        ["$updated"] = updatedAt
                    },
                    token).ConfigureAwait(false);
            }
            int inserted = 0;
            foreach (EventReportRow row in rows) {
                token.ThrowIfCancellationRequested();
                inserted += await transaction.ExecuteNonQueryAsync(
                    InsertEventSql,
                    CreateEventParameters(row, updatedAt),
                    token).ConfigureAwait(false);
            }
            if (checkpoint != null) {
                await transaction.ExecuteNonQueryAsync(
                    UpsertCheckpointSql,
                    new Dictionary<string, object?> {
                        ["$consumer"] = checkpoint.Consumer.Trim(),
                        ["$computer"] = checkpoint.Computer.Trim(),
                        ["$container"] = checkpoint.Container.Trim(),
                        ["$recordId"] = checkpoint.RecordId,
                        ["$bookmark"] = checkpoint.BookmarkXml,
                        ["$updated"] = updatedAt
                    },
                    token).ConfigureAwait(false);
            }
            return new EventStoreWriteResult(rows.Length, inserted, checkpoint != null);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets a detached durable checkpoint for one consumer and source.</summary>
    public async Task<EventStoreCheckpoint?> GetCheckpointAsync(
        string consumer,
        string computer,
        string container,
        CancellationToken cancellationToken = default) {

        if (string.IsNullOrWhiteSpace(consumer) || string.IsNullOrWhiteSpace(computer) ||
            string.IsNullOrWhiteSpace(container)) {
            throw new ArgumentException("Consumer, computer, and container are required.");
        }
        EnsureInitialized();
        using var sqlite = new SQLite { BusyTimeoutMs = 10000 };
        await using SQLiteAsyncSession session = await sqlite
            .OpenSessionAsync(Path, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<EventStoreCheckpoint> rows = await session.QueryAsListAsync(
            @"SELECT consumer, computer, container, record_id, bookmark_xml, updated_utc
              FROM evx_checkpoints
              WHERE consumer = $consumer AND computer = $computer AND container = $container;",
            static record => new EventStoreCheckpoint {
                Consumer = record.GetString(0),
                Computer = record.GetString(1),
                Container = record.GetString(2),
                RecordId = record.IsDBNull(3) ? null : record.GetInt64(3),
                BookmarkXml = record.IsDBNull(4) ? null : record.GetString(4),
                UpdatedAtUtc = DateTime.Parse(
                    record.GetString(5),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind)
            },
            new Dictionary<string, object?> {
                ["$consumer"] = consumer.Trim(),
                ["$computer"] = computer.Trim(),
                ["$container"] = container.Trim()
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    private static Dictionary<string, object?> CreateEventParameters(
        EventReportRow row,
        string insertedAt) => new() {
            ["$key"] = CreateEventKey(row),
            ["$definition"] = row.Type,
            ["$time"] = row.TimeCreated.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ["$eventId"] = row.EventId,
            ["$recordId"] = row.RecordId,
            ["$provider"] = row.Provider ?? string.Empty,
            ["$sourceLog"] = row.SourceLog ?? string.Empty,
            ["$containerLog"] = row.ContainerLog ?? string.Empty,
            ["$sourceComputer"] = row.SourceComputer ?? string.Empty,
            ["$collectorComputer"] = row.CollectorComputer ?? string.Empty,
            ["$level"] = row.Level ?? string.Empty,
            ["$levelValue"] = row.LevelValue,
            ["$message"] = row.Message ?? string.Empty,
            ["$values"] = JsonSerializer.Serialize(row.Values, JsonOptions),
            ["$inserted"] = insertedAt
        };

    private static string CreateEventKey(EventReportRow row) {
        string identity = string.Join("\0", new[] {
            NormalizeSqliteNoCaseIdentity(row.SourceComputer),
            NormalizeSqliteNoCaseIdentity(row.SourceLog),
            row.RecordId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            row.EventId.ToString(CultureInfo.InvariantCulture),
            NormalizeSqliteNoCaseIdentity(row.Provider),
            row.TimeCreated.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            NormalizeSqliteNoCaseIdentity(row.Type)
        });
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
        return string.Concat(hash.Select(static value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static string NormalizeSqliteNoCaseIdentity(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }
        char[] characters = value!.ToCharArray();
        for (int index = 0; index < characters.Length; index++) {
            if (characters[index] is >= 'a' and <= 'z') {
                characters[index] = (char)(characters[index] - ('a' - 'A'));
            }
        }
        return new string(characters);
    }

    private static string CreateSchemaHash(EventReportSectionSchema schema) {
        if (schema.Kind == EventReportSectionKind.Generic) {
            return "generic-dynamic-v1";
        }
        string identity = string.Join("\0", new[] {
            ((int)schema.Kind).ToString(CultureInfo.InvariantCulture)
        }.Concat(schema.Columns.SelectMany(static column => new[] {
            column.Name,
            EventReportColumnSchema.NormalizeValueTypeName(column.ValueTypeName)
        })));
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
        return string.Concat(hash.Select(static value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static bool HasEquivalentSchema(string json, string expectedHash) {
        try {
            EventReportSectionSchema? schema = JsonSerializer.Deserialize<EventReportSectionSchema>(json, JsonOptions);
            return schema != null &&
                   string.Equals(CreateSchemaHash(schema), expectedHash, StringComparison.Ordinal);
        } catch (JsonException) {
            return false;
        }
    }

    private static void ValidateCheckpoint(EventStoreCheckpoint? checkpoint) {
        if (checkpoint == null) {
            return;
        }
        if (string.IsNullOrWhiteSpace(checkpoint.Consumer) ||
            string.IsNullOrWhiteSpace(checkpoint.Computer) ||
            string.IsNullOrWhiteSpace(checkpoint.Container)) {
            throw new ArgumentException("Checkpoint consumer, computer, and container are required.", nameof(checkpoint));
        }
    }

    private const string UpsertDefinitionSql = @"
INSERT INTO evx_definitions
    (definition_name, display_name, description, kind, schema_hash, schema_json, updated_utc)
VALUES ($name, $display, $description, $kind, $schemaHash, $schema, $updated)
ON CONFLICT(definition_name) DO UPDATE SET
    display_name = excluded.display_name,
    description = excluded.description,
    kind = excluded.kind,
    schema_hash = excluded.schema_hash,
    schema_json = excluded.schema_json,
    updated_utc = excluded.updated_utc;";

    private const string InsertEventSql = @"
INSERT OR IGNORE INTO evx_events
    (event_key, definition_name, event_time_utc, event_id, record_id, provider,
     source_log, container_log, source_computer, collector_computer, level,
     level_value, message, values_json, inserted_utc)
VALUES
    ($key, $definition, $time, $eventId, $recordId, $provider,
     $sourceLog, $containerLog, $sourceComputer, $collectorComputer, $level,
     $levelValue, $message, $values, $inserted);";

    private const string UpsertCheckpointSql = @"
INSERT INTO evx_checkpoints
    (consumer, computer, container, record_id, bookmark_xml, updated_utc)
VALUES ($consumer, $computer, $container, $recordId, $bookmark, $updated)
ON CONFLICT(consumer, computer, container) DO UPDATE SET
    record_id = excluded.record_id,
    bookmark_xml = excluded.bookmark_xml,
    updated_utc = excluded.updated_utc;";

    private sealed class StoredDefinitionSchema {
        internal StoredDefinitionSchema(string hash, string json) {
            Hash = hash;
            Json = json;
        }

        internal string Hash { get; }
        internal string Json { get; }
    }
}

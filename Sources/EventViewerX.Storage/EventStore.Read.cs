using System.Text.Json;
using DBAClientX;
using EventViewerX.Reporting;

namespace EventViewerX.Storage;

public sealed partial class EventStore {
    /// <summary>Explains indexed SQLite prefiltering and managed typed verification without reading rows.</summary>
    public static EventStoreQueryPlan Plan(EventStoreQuery query) {
        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        EventStoreQuery snapshot = query.Snapshot();
        var steps = new List<EventStoreQueryPlanStep>();
        AddDirectPlanStep(steps, "Definition", snapshot.ResolveDefinitionNames());
        AddDirectPlanStep(steps, "EventId", snapshot.EventIds);
        AddDirectPlanStep(steps, "RecordId", snapshot.RecordIds);
        AddDirectPlanStep(steps, "SourceComputer", snapshot.SourceComputers);
        AddDirectPlanStep(steps, "SourceLog", snapshot.SourceLogs);
        AddDirectPlanStep(steps, "Provider", snapshot.Providers);
        if (snapshot.StartTime.HasValue || snapshot.EndTime.HasValue) {
            steps.Add(new EventStoreQueryPlanStep(
                "TimeCreated boundary",
                EventStoreQueryPlanStage.Sql,
                "UTC time boundaries use the indexed event_time_utc column."));
        }
        if (snapshot.Predicate != null) {
            EventPredicatePlan predicatePlan = EventPredicatePlanner.Plan(snapshot.Predicate);
            steps.AddRange(predicatePlan.Steps
                .Where(static step => !string.Equals(
                    step.Expression,
                    "Exact predicate verification",
                    StringComparison.Ordinal))
                .Select(static step => new EventStoreQueryPlanStep(
                step.Expression,
                step.Stage == EventPredicatePlanStage.Native
                    ? EventStoreQueryPlanStage.Sql
                    : EventStoreQueryPlanStage.Managed,
                step.Stage == EventPredicatePlanStage.Native
                    ? "The common event dimension is eligible for indexed SQLite pushdown when selected stored schemas do not shadow that field."
                    : step.Reason)));
            steps.Add(new EventStoreQueryPlanStep(
                "Exact predicate verification",
                EventStoreQueryPlanStage.Managed,
                "The complete predicate is verified against normalized typed values after SQL prefiltering."));
        }
        return new EventStoreQueryPlan(steps, snapshot.MaxCandidates);
    }

    /// <summary>Reads normalized stored rows and recreates homogeneous report sections.</summary>
    public async Task<EventReport> ReadReportAsync(
        EventStoreQuery query,
        string? title = null,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        EnsureInitialized();
        EventStoreQuery snapshot = query.Snapshot();
        using var sqlite = new SQLite { BusyTimeoutMs = 10000 };
        await using SQLiteAsyncSession session = await sqlite
            .OpenSessionAsync(Path, cancellationToken)
            .ConfigureAwait(false);
        return await session.RunInTransactionAsync(async (transaction, token) => {
            PredicatePushdownPolicy pushdown = await ReadPredicatePushdownPolicyAsync(
                transaction,
                snapshot.ResolveDefinitionNames(),
                token).ConfigureAwait(false);
            QueryCommand command = BuildReadCommand(snapshot, pushdown);
            IReadOnlyList<EventReportRow> candidates = await transaction.QueryAsListAsync(
                command.Sql,
                MapEventRow,
                command.Parameters,
                cancellationToken: token).ConfigureAwait(false);

            bool scanLimitReached = command.CandidateLimit > 0 && candidates.Count > command.CandidateLimit;
            IEnumerable<EventReportRow> boundedCandidates = scanLimitReached
                ? candidates.Take(command.CandidateLimit)
                : candidates;
            var rows = new List<EventReportRow>();
            long scanned = 0;
            foreach (EventReportRow row in boundedCandidates) {
                token.ThrowIfCancellationRequested();
                scanned++;
                if (snapshot.Predicate != null &&
                    !EventPredicateEvaluator.Matches(snapshot.Predicate, row.ToPredicateDictionary())) {
                    continue;
                }
                rows.Add(row);
                if (snapshot.MaxEvents > 0 && rows.Count >= snapshot.MaxEvents) {
                    break;
                }
            }

            string[] definitionNames = rows.Select(static row => row.Type)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            IReadOnlyList<EventReportSectionSchema> schemas = await ReadSchemasAsync(
                transaction,
                definitionNames,
                token).ConfigureAwait(false);
            EventReportCoverage[] coverage = rows
                .GroupBy(static row => row.CollectorComputer + "\0" + row.SourceLog, StringComparer.OrdinalIgnoreCase)
                .Select(static group => {
                    EventReportRow first = group.First();
                    return new EventReportCoverage {
                        MachineName = first.CollectorComputer,
                        LogName = first.SourceLog,
                        Succeeded = true,
                        Status = "Stored",
                        Detail = string.Empty
                    };
                }).ToArray();
            return EventReportEngine.CreateStored(
                rows,
                schemas,
                title,
                coverage,
                eventsScanned: scanned,
                scanLimitReached: scanLimitReached);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static QueryCommand BuildReadCommand(
        EventStoreQuery query,
        PredicatePushdownPolicy pushdown) {

        WhereCommand filter = BuildWhere(query, includePredicateNative: true, pushdown: pushdown);

        int candidateLimit = query.Predicate != null && query.MaxCandidates > 0
            ? checked((int)Math.Min(query.MaxCandidates, int.MaxValue - 1))
            : 0;
        long directLimit = query.Predicate == null ? query.MaxEvents : 0;
        long sqlLimit = candidateLimit > 0 ? candidateLimit + 1L : directLimit;
        string sql = @"SELECT definition_name, event_time_utc, event_id, record_id, provider,
source_log, container_log, source_computer, collector_computer, level, level_value, message, values_json
FROM evx_events";
        if (filter.Clauses.Count > 0) {
            sql += " WHERE " + string.Join(" AND ", filter.Clauses);
        }
        sql += query.Oldest ? " ORDER BY event_time_utc ASC, rowid ASC" : " ORDER BY event_time_utc DESC, rowid DESC";
        if (sqlLimit > 0) {
            sql += " LIMIT $limit";
            filter.Parameters["$limit"] = sqlLimit;
        }
        sql += ";";
        return new QueryCommand(sql, filter.Parameters, candidateLimit);
    }

    private static WhereCommand BuildWhere(
        EventStoreQuery query,
        bool includePredicateNative,
        PredicatePushdownPolicy? pushdown = null) {

        var where = new List<string>();
        var parameters = new Dictionary<string, object?>();
        AddIn(where, parameters, "definition_name", "definition", query.ResolveDefinitionNames());
        AddBoundary(where, parameters, "event_time_utc", "$start", ">=", ToUtcText(query.StartTime));
        AddBoundary(where, parameters, "event_time_utc", "$end", "<=", ToUtcText(query.EndTime));
        AddIn(where, parameters, "event_id", "eventId", query.EventIds);
        AddIn(where, parameters, "record_id", "recordId", query.RecordIds);
        AddIn(where, parameters, "source_computer", "sourceComputer", query.SourceComputers);
        AddIn(where, parameters, "source_log", "sourceLog", query.SourceLogs);
        AddIn(where, parameters, "provider", "provider", query.Providers);
        if (includePredicateNative && query.Predicate != null) {
            EventFilter? native = EventPredicatePlanner.Plan(query.Predicate).NativeFilter;
            if (native != null) {
                pushdown ??= PredicatePushdownPolicy.AllowAll;
                if (pushdown.CanPush(query.Predicate, "EventId", "Id")) {
                    AddIn(where, parameters, "event_id", "predicateEventId", native.EventIds);
                }
                if (pushdown.CanPush(query.Predicate, "RecordId", "EventRecordId")) {
                    AddIn(where, parameters, "record_id", "predicateRecordId", native.RecordIds);
                }
                if (pushdown.CanPush(query.Predicate, "ProviderName", "Provider")) {
                    AddIn(where, parameters, "provider", "predicateProvider", native.ProviderNames);
                }
                if (pushdown.CanPush(query.Predicate, "Level")) {
                    AddIn(where, parameters, "level_value", "predicateLevel", native.Levels);
                }
                if (pushdown.CanPush(query.Predicate, "TimeCreated", "When")) {
                    AddBoundary(where, parameters, "event_time_utc", "$predicateStart", ">=", ToUtcText(native.StartTime));
                    AddBoundary(where, parameters, "event_time_utc", "$predicateEnd", "<=", ToUtcText(native.EndTime));
                }
            }
        }
        return new WhereCommand(where, parameters);
    }

    private static async Task<PredicatePushdownPolicy> ReadPredicatePushdownPolicyAsync(
        SQLiteAsyncSession session,
        IReadOnlyList<string> definitionNames,
        CancellationToken cancellationToken) {

        var where = new List<string>();
        var parameters = new Dictionary<string, object?>();
        AddIn(where, parameters, "definition_name", "pushdownSchema", definitionNames);
        string sql = "SELECT schema_json FROM evx_definitions";
        if (where.Count > 0) {
            sql += " WHERE " + where[0];
        }
        sql += ";";
        IReadOnlyList<EventReportSectionSchema> schemas = await session.QueryAsListAsync(
            sql,
            static record => JsonSerializer.Deserialize<EventReportSectionSchema>(record.GetString(0), JsonOptions)
                ?? throw new InvalidDataException("A stored report schema is invalid."),
            parameters,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (schemas.Any(static schema => schema.Kind == EventReportSectionKind.Generic)) {
            return PredicatePushdownPolicy.DisableAll;
        }
        return new PredicatePushdownPolicy(schemas
            .SelectMany(static schema => schema.Columns)
            .Select(static column => column.Name));
    }

    private static EventReportRow MapEventRow(IDataRecord record) => new() {
        Type = record.GetString(0),
        TimeCreated = ParseUtc(record.GetString(1)),
        EventId = record.GetInt32(2),
        RecordId = record.IsDBNull(3) ? null : record.GetInt64(3),
        Provider = record.GetString(4),
        SourceLog = record.GetString(5),
        ContainerLog = record.GetString(6),
        SourceComputer = record.GetString(7),
        CollectorComputer = record.GetString(8),
        Level = record.GetString(9),
        LevelValue = record.IsDBNull(10) ? null : record.GetByte(10),
        Message = record.GetString(11),
        Values = DeserializeValues(record.GetString(12))
    };

    private static async Task<IReadOnlyList<EventReportSectionSchema>> ReadSchemasAsync(
        SQLiteAsyncSession session,
        IReadOnlyList<string> names,
        CancellationToken cancellationToken) {

        if (names.Count == 0) {
            return Array.Empty<EventReportSectionSchema>();
        }
        var where = new List<string>();
        var parameters = new Dictionary<string, object?>();
        AddIn(where, parameters, "definition_name", "schema", names);
        return await session.QueryAsListAsync(
            "SELECT schema_json FROM evx_definitions WHERE " + where[0] + ";",
            static record => JsonSerializer.Deserialize<EventReportSectionSchema>(record.GetString(0), JsonOptions)
                ?? throw new InvalidDataException("A stored report schema is invalid."),
            parameters,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(string json) {
        Dictionary<string, JsonElement>? values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
        return values?.ToDictionary(
            static item => item.Key,
            static item => ConvertJson(item.Value),
            StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, object?>();
    }

    private static object? ConvertJson(JsonElement value) => value.ValueKind switch {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String when value.TryGetDateTime(out DateTime date) => date,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => value.EnumerateArray().Select(ConvertJson).ToArray(),
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(
            static property => property.Name,
            static property => ConvertJson(property.Value),
            StringComparer.OrdinalIgnoreCase),
        _ => value.GetRawText()
    };

    private static DateTime ParseUtc(string value) => DateTime.Parse(
        value,
        CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind).ToUniversalTime();

    private static string? ToUtcText(DateTime? value) => value?.ToUniversalTime()
        .ToString("O", CultureInfo.InvariantCulture);

    private static void AddBoundary(
        ICollection<string> where,
        IDictionary<string, object?> parameters,
        string column,
        string parameter,
        string operation,
        string? value) {

        if (value == null) {
            return;
        }
        where.Add($"{column} {operation} {parameter}");
        parameters[parameter] = value;
    }

    private static void AddIn<T>(
        ICollection<string> where,
        IDictionary<string, object?> parameters,
        string column,
        string prefix,
        IReadOnlyList<T>? values) {

        if (values == null || values.Count == 0) {
            return;
        }
        var names = new string[values.Count];
        for (int index = 0; index < values.Count; index++) {
            string name = $"${prefix}{index}";
            names[index] = name;
            parameters[name] = values[index];
        }
        where.Add($"{column} IN ({string.Join(", ", names)})");
    }

    private static void AddDirectPlanStep<T>(
        ICollection<EventStoreQueryPlanStep> steps,
        string name,
        IReadOnlyList<T>? values) {

        if (values == null || values.Count == 0) {
            return;
        }
        steps.Add(new EventStoreQueryPlanStep(
            $"{name} ({values.Count})",
            EventStoreQueryPlanStage.Sql,
            $"{name} selection uses an indexed normalized SQLite column."));
    }

    private sealed class QueryCommand {
        internal QueryCommand(string sql, Dictionary<string, object?> parameters, int candidateLimit) {
            Sql = sql;
            Parameters = parameters;
            CandidateLimit = candidateLimit;
        }

        internal string Sql { get; }
        internal Dictionary<string, object?> Parameters { get; }
        internal int CandidateLimit { get; }
    }

    private sealed class WhereCommand {
        internal WhereCommand(List<string> clauses, Dictionary<string, object?> parameters) {
            Clauses = clauses;
            Parameters = parameters;
        }

        internal List<string> Clauses { get; }
        internal Dictionary<string, object?> Parameters { get; }
    }

    private sealed class PredicatePushdownPolicy {
        internal static readonly PredicatePushdownPolicy AllowAll = new(Array.Empty<string>());
        internal static readonly PredicatePushdownPolicy DisableAll = new(Array.Empty<string>(), disableAll: true);
        private readonly HashSet<string> _shadowedFields;
        private readonly bool _disableAll;

        internal PredicatePushdownPolicy(IEnumerable<string> shadowedFields, bool disableAll = false) {
            _shadowedFields = new HashSet<string>(shadowedFields, StringComparer.OrdinalIgnoreCase);
            _disableAll = disableAll;
        }

        internal bool CanPush(EventPredicate predicate, params string[] nativeAliases) =>
            !_disableAll && !UsesShadowedField(predicate, nativeAliases);

        private bool UsesShadowedField(EventPredicate predicate, IReadOnlyList<string> nativeAliases) {
            if (predicate.Kind == EventPredicateKind.Comparison) {
                return nativeAliases.Any(alias => string.Equals(alias, predicate.Field, StringComparison.OrdinalIgnoreCase)) &&
                       _shadowedFields.Contains(predicate.Field!);
            }
            return predicate.Children.Any(child => UsesShadowedField(child, nativeAliases));
        }
    }
}

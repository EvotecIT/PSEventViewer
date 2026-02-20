using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Diagnostics;
using EventViewerX.Reports.QueryHelpers;

namespace EventViewerX.Reports.Correlation;

/// <summary>
/// Builds timeline and correlation projections for named-event detections.
/// </summary>
public static class NamedEventsTimelineQueryExecutor {
    private const int MaxCorrelationKeys = 8;
    private const int MaxPayloadKeys = 64;
    private const int MaxGroupsCap = 2000;
    private const int MaxBucketMinutes = 1440;
    private const int MaxThreadsCap = 8;
    private const int CorrelationIdHashBytes = 8;
    private static readonly string[] AllowedCorrelationKeysValue = {
        "who",
        "object_affected",
        "computer",
        "action",
        "named_event",
        "event_id",
        "gathered_from",
        "gathered_log_name"
    };

    private static readonly string[] DefaultCorrelationKeysValue = {
        "who",
        "object_affected",
        "computer"
    };

    /// <summary>
    /// Allowed correlation dimensions accepted by <see cref="TryBuildAsync"/>.
    /// </summary>
    public static IReadOnlyList<string> AllowedCorrelationKeys => AllowedCorrelationKeysValue;

    /// <summary>
    /// Default correlation dimensions used when no explicit dimensions are provided.
    /// </summary>
    public static IReadOnlyList<string> DefaultCorrelationKeys => DefaultCorrelationKeysValue;

    /// <summary>
    /// Builds timeline and correlation projections from a named-events query request.
    /// </summary>
    /// <param name="request">Query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple containing either a successful <see cref="NamedEventsTimelineQueryResult"/> or a
    /// <see cref="NamedEventsTimelineQueryFailure"/>.
    /// </returns>
    public static async Task<(NamedEventsTimelineQueryResult? Result, NamedEventsTimelineQueryFailure? Failure)> TryBuildAsync(
        NamedEventsTimelineQueryRequest request,
        CancellationToken cancellationToken = default) {
        if (request is null) {
            return (null, new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "request is required."
            });
        }

        if (!TryValidateRequest(
                request,
                out var normalizedNamedEvents,
                out var normalizedMachines,
                out var normalizedCorrelationKeys,
                out var normalizedPayloadKeys,
                out var normalizedEventIds,
                out var failure)) {
            return (null, failure);
        }

        var maxEvents = request.MaxEvents <= 0 ? 1 : request.MaxEvents;
        var maxThreads = Math.Max(1, Math.Min(request.MaxThreads <= 0 ? 4 : request.MaxThreads, MaxThreadsCap));
        var maxGroups = Math.Max(1, Math.Min(request.MaxGroups <= 0 ? 250 : request.MaxGroups, MaxGroupsCap));
        var bucketMinutes = Math.Max(1, Math.Min(request.BucketMinutes <= 0 ? 15 : request.BucketMinutes, MaxBucketMinutes));
        var maxEventsPerNamedEvent = request.MaxEventsPerNamedEvent.HasValue && request.MaxEventsPerNamedEvent.Value > 0
            ? request.MaxEventsPerNamedEvent
            : null;
        var effectiveNamedEvents = normalizedNamedEvents ?? new List<NamedEvents>();
        var includeUncorrelated = request.IncludeUncorrelated;
        var includePayload = request.IncludePayload;
        string? logName = null;
        var requestLogName = request.LogName;
        var trimmedLogName = requestLogName is null ? string.Empty : requestLogName.Trim();
        if (trimmedLogName.Length > 0) {
            logName = trimmedLogName;
        }

        var rows = new List<EventRowAccumulator>(Math.Min(maxEvents, 256));
        var perNamedEventCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var filteredOut = 0;
        var filteredUncorrelated = 0;
        var truncated = false;

        try {
            await foreach (var item in SearchEvents.FindEventsByNamedEvents(
                               typeEventsList: effectiveNamedEvents,
                               machineNames: normalizedMachines.Count > 0 ? normalizedMachines.Cast<string?>().ToList() : null,
                               startTime: request.StartTimeUtc,
                               endTime: request.EndTimeUtc,
                               timePeriod: request.TimePeriod,
                               maxThreads: maxThreads,
                               maxEvents: maxEvents,
                               cancellationToken: cancellationToken)) {
                cancellationToken.ThrowIfCancellationRequested();

                var namedEventName = ResolveNamedEventName(item);
                if (maxEventsPerNamedEvent.HasValue) {
                    var current = perNamedEventCount.TryGetValue(namedEventName, out var count) ? count : 0;
                    if (current >= maxEventsPerNamedEvent.Value) {
                        filteredOut++;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(logName) &&
                    !string.Equals(item.GatheredLogName, logName, StringComparison.OrdinalIgnoreCase)) {
                    filteredOut++;
                    continue;
                }

                if (normalizedEventIds is not null && !normalizedEventIds.Contains(item.EventID)) {
                    filteredOut++;
                    continue;
                }

                var row = ToAccumulator(item, namedEventName, includePayload, normalizedPayloadKeys);
                var correlation = BuildCorrelationValues(row, normalizedCorrelationKeys);
                row.Correlation = correlation;
                var hasCorrelation = correlation.Values.Any(static value => !string.IsNullOrWhiteSpace(value));
                if (!hasCorrelation && !includeUncorrelated) {
                    filteredUncorrelated++;
                    continue;
                }

                rows.Add(row);
                perNamedEventCount[namedEventName] = perNamedEventCount.TryGetValue(namedEventName, out var existingCount) ? existingCount + 1 : 1;
                if (rows.Count >= maxEvents) {
                    truncated = true;
                    break;
                }
            }
        } catch (ArgumentException ex) {
            return (null, new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = ex.Message
            });
        } catch (InvalidOperationException ex) {
            return (null, new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.QueryFailed,
                Message = ex.Message
            });
        } catch (Exception ex) {
            return (null, new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.Exception,
                Message = ex.Message
            });
        }

        var orderedRows = rows
            .OrderBy(static row => row.WhenUtcDate ?? DateTime.MaxValue)
            .ThenBy(static row => row.RecordId ?? long.MaxValue)
            .ThenBy(static row => row.NamedEvent, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var timelineRows = new List<NamedEventsTimelineEventRow>(orderedRows.Count);
        var groups = new Dictionary<string, GroupAccumulator>(StringComparer.OrdinalIgnoreCase);
        var buckets = new Dictionary<DateTime, BucketAccumulator>();
        var sequence = 0;

        for (var i = 0; i < orderedRows.Count; i++) {
            var row = orderedRows[i];
            var correlationToken = BuildCorrelationToken(row.Correlation);
            var correlationId = BuildCorrelationId(correlationToken);

            sequence++;
            timelineRows.Add(new NamedEventsTimelineEventRow {
                Sequence = sequence,
                CorrelationId = correlationId,
                Correlation = row.Correlation,
                NamedEvent = row.NamedEvent,
                RuleType = row.RuleType,
                EventId = row.EventId,
                RecordId = row.RecordId,
                GatheredFrom = row.GatheredFrom,
                GatheredLogName = row.GatheredLogName,
                WhenUtc = row.WhenUtc,
                Who = row.Who,
                ObjectAffected = row.ObjectAffected,
                Computer = row.Computer,
                Action = row.Action,
                Payload = row.Payload
            });

            if (!groups.TryGetValue(correlationId, out var group)) {
                group = new GroupAccumulator {
                    CorrelationId = correlationId,
                    Correlation = row.Correlation
                };
                groups[correlationId] = group;
            }

            group.EventCount++;
            group.NamedEvents.Add(row.NamedEvent);
            group.EventIds.Add(row.EventId);
            if (!string.IsNullOrWhiteSpace(row.GatheredFrom)) {
                group.Machines.Add(row.GatheredFrom);
            }

            if (row.WhenUtcDate.HasValue) {
                if (!group.FirstSeenUtc.HasValue || row.WhenUtcDate.Value < group.FirstSeenUtc.Value) {
                    group.FirstSeenUtc = row.WhenUtcDate.Value;
                }
                if (!group.LastSeenUtc.HasValue || row.WhenUtcDate.Value > group.LastSeenUtc.Value) {
                    group.LastSeenUtc = row.WhenUtcDate.Value;
                }

                var bucketStart = FloorToBucket(row.WhenUtcDate.Value, bucketMinutes);
                if (!buckets.TryGetValue(bucketStart, out var bucket)) {
                    bucket = new BucketAccumulator {
                        BucketStartUtc = bucketStart
                    };
                    buckets[bucketStart] = bucket;
                }

                bucket.EventCount++;
                bucket.CorrelationIds.Add(correlationId);
            }
        }

        var orderedGroups = groups.Values
            .OrderByDescending(static group => group.EventCount)
            .ThenBy(static group => group.FirstSeenUtc ?? DateTime.MaxValue)
            .ThenBy(static group => group.CorrelationId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var groupsTotal = orderedGroups.Count;

        var groupsTruncated = orderedGroups.Count > maxGroups;
        if (groupsTruncated) {
            orderedGroups = orderedGroups.Take(maxGroups).ToList();
        }

        var groupRows = orderedGroups
            .Select(group => new NamedEventsTimelineGroupRow {
                CorrelationId = group.CorrelationId,
                Correlation = group.Correlation,
                EventCount = group.EventCount,
                FirstSeenUtc = group.FirstSeenUtc?.ToString("O"),
                LastSeenUtc = group.LastSeenUtc?.ToString("O"),
                DurationMinutes = group.FirstSeenUtc.HasValue && group.LastSeenUtc.HasValue
                    ? Math.Round((group.LastSeenUtc.Value - group.FirstSeenUtc.Value).TotalMinutes, 3)
                    : null,
                NamedEvents = group.NamedEvents.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                EventIds = group.EventIds.OrderBy(static value => value).ToArray(),
                Machines = group.Machines.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray()
            })
            .ToArray();

        var bucketRows = buckets.Values
            .OrderBy(static bucket => bucket.BucketStartUtc)
            .Select(bucket => new NamedEventsTimelineBucketRow {
                BucketStartUtc = bucket.BucketStartUtc.ToString("O"),
                BucketEndUtc = bucket.BucketStartUtc.AddMinutes(bucketMinutes).ToString("O"),
                EventCount = bucket.EventCount,
                CorrelationCount = bucket.CorrelationIds.Count
            })
            .ToArray();

        return (new NamedEventsTimelineQueryResult {
            RequestedNamedEvents = effectiveNamedEvents
                .Select(static value => ToSnakeCase(value.ToString()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            EffectiveMachines = normalizedMachines.ToArray(),
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            MaxEvents = maxEvents,
            MaxThreads = maxThreads,
            CorrelationKeys = normalizedCorrelationKeys.ToArray(),
            IncludeUncorrelated = includeUncorrelated,
            BucketMinutes = bucketMinutes,
            Truncated = truncated,
            GroupsTruncated = groupsTruncated,
            GroupsTotal = groupsTotal,
            FilteredOut = filteredOut,
            FilteredUncorrelated = filteredUncorrelated,
            Timeline = timelineRows,
            CorrelationGroups = groupRows,
            Buckets = bucketRows
        }, null);
    }

    private static bool TryValidateRequest(
        NamedEventsTimelineQueryRequest request,
        out List<NamedEvents> normalizedNamedEvents,
        out List<string> normalizedMachines,
        out List<string> normalizedCorrelationKeys,
        out HashSet<string>? normalizedPayloadKeys,
        out HashSet<int>? normalizedEventIds,
        out NamedEventsTimelineQueryFailure? failure) {
        normalizedNamedEvents = new List<NamedEvents>();
        normalizedMachines = new List<string>();
        normalizedCorrelationKeys = new List<string>();
        normalizedPayloadKeys = null;
        normalizedEventIds = null;

        if (request is null) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "request is required."
            };
            return false;
        }

        if (request.NamedEvents is null || request.NamedEvents.Count == 0) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "namedEvents must contain at least one value."
            };
            return false;
        }

        if (QueryValidationHelpers.HasInvalidUtcRange(request.StartTimeUtc, request.EndTimeUtc)) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "startTimeUtc must be less than or equal to endTimeUtc."
            };
            return false;
        }

        if (request.TimePeriod.HasValue && (request.StartTimeUtc.HasValue || request.EndTimeUtc.HasValue)) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "timePeriod cannot be combined with startTimeUtc/endTimeUtc."
            };
            return false;
        }

        if (request.MaxEvents <= 0) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "maxEvents must be greater than 0."
            };
            return false;
        }

        if (request.MaxThreads <= 0) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "maxThreads must be greater than 0."
            };
            return false;
        }

        if (request.MaxEventsPerNamedEvent.HasValue && request.MaxEventsPerNamedEvent.Value <= 0) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "maxEventsPerNamedEvent must be greater than 0 when provided."
            };
            return false;
        }

        if (request.MaxGroups <= 0) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "maxGroups must be greater than 0."
            };
            return false;
        }

        if (request.BucketMinutes <= 0 || request.BucketMinutes > MaxBucketMinutes) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = $"bucketMinutes must be between 1 and {MaxBucketMinutes}."
            };
            return false;
        }

        if (request.EventIds is not null && QueryValidationHelpers.HasNonPositiveValues(request.EventIds)) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = "eventIds must contain only positive values."
            };
            return false;
        }

        normalizedNamedEvents = request.NamedEvents
            .Distinct(EqualityComparer<NamedEvents>.Default)
            .ToList();

        if (request.MachineNames is not null) {
            for (var i = 0; i < request.MachineNames.Count; i++) {
                var candidate = request.MachineNames[i];
                if (string.IsNullOrWhiteSpace(candidate)) {
                    continue;
                }

                var trimmed = candidate.Trim();
                if (!normalizedMachines.Contains(trimmed, StringComparer.OrdinalIgnoreCase)) {
                    normalizedMachines.Add(trimmed);
                }
            }
        }

        if (request.EventIds is not null && request.EventIds.Count > 0) {
            normalizedEventIds = request.EventIds
                .Distinct()
                .ToHashSet();
        }

        if (!TryNormalizeCorrelationKeys(request.CorrelationKeys, out normalizedCorrelationKeys, out var correlationError)) {
            failure = new NamedEventsTimelineQueryFailure {
                Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                Message = correlationError ?? "Invalid correlation keys."
            };
            return false;
        }

        if (request.PayloadKeys is not null && request.PayloadKeys.Count > 0) {
            if (request.PayloadKeys.Count > MaxPayloadKeys) {
                failure = new NamedEventsTimelineQueryFailure {
                    Kind = NamedEventsTimelineQueryFailureKind.InvalidArgument,
                    Message = $"payloadKeys supports at most {MaxPayloadKeys} values."
                };
                return false;
            }

            normalizedPayloadKeys = request.PayloadKeys
                .Select(ToSnakeCase)
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        failure = null;
        return true;
    }

    private static bool TryNormalizeCorrelationKeys(
        IReadOnlyList<string>? requested,
        out List<string> normalized,
        out string? error) {
        normalized = new List<string>();
        error = null;

        if (requested is null || requested.Count == 0) {
            normalized.AddRange(DefaultCorrelationKeysValue);
            return true;
        }

        if (requested.Count > MaxCorrelationKeys) {
            error = $"correlationKeys supports at most {MaxCorrelationKeys} values.";
            return false;
        }

        var allowed = new HashSet<string>(AllowedCorrelationKeysValue, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < requested.Count; i++) {
            var raw = requested[i];
            var key = ToSnakeCase(raw);
            if (string.IsNullOrWhiteSpace(key) || !allowed.Contains(key)) {
                error = $"correlationKeys[{i}] ('{raw}') is not recognized. Allowed values: {string.Join(", ", AllowedCorrelationKeysValue)}.";
                return false;
            }

            if (seen.Add(key)) {
                normalized.Add(key);
            }
        }

        if (normalized.Count == 0) {
            error = "correlationKeys must contain at least one valid value.";
            return false;
        }

        return true;
    }

    private static EventRowAccumulator ToAccumulator(
        EventObjectSlim item,
        string namedEvent,
        bool includePayload,
        HashSet<string>? payloadKeySet) {
        if (item is null) {
            throw new ArgumentNullException(nameof(item));
        }

        var fullPayload = ExtractPayload(item);
        var payload = includePayload
            ? ProjectPayload(fullPayload, payloadKeySet)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var whenUtc = ReadPayloadUtc(fullPayload, "when");

        return new EventRowAccumulator(
            namedEvent,
            item.GetType().Name,
            item.EventID,
            item.RecordID,
            item.GatheredFrom,
            item.GatheredLogName,
            whenUtc,
            ParseUtc(whenUtc),
            ReadPayloadString(fullPayload, "who"),
            ReadPayloadString(fullPayload, "object_affected"),
            ReadPayloadString(fullPayload, "computer"),
            ReadPayloadString(fullPayload, "action"),
            payload);
    }

    private static string ResolveNamedEventName(EventObjectSlim item) {
        return Enum.TryParse<NamedEvents>(item.Type, out var parsedNamedEvent)
            ? ToSnakeCase(parsedNamedEvent.ToString())
            : ToSnakeCase(item.Type);
    }

    private static IReadOnlyDictionary<string, string> BuildCorrelationValues(
        EventRowAccumulator row,
        IReadOnlyList<string> correlationKeys) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < correlationKeys.Count; i++) {
            var key = correlationKeys[i];
            values[key] = ResolveCorrelationValue(row, key);
        }

        return values;
    }

    private static string ResolveCorrelationValue(EventRowAccumulator row, string correlationKey) {
        return correlationKey switch {
            "who" => NormalizeCorrelationValue(row.Who),
            "object_affected" => NormalizeCorrelationValue(row.ObjectAffected),
            "computer" => NormalizeCorrelationValue(row.Computer),
            "action" => NormalizeCorrelationValue(row.Action),
            "named_event" => NormalizeCorrelationValue(row.NamedEvent),
            "event_id" => row.EventId.ToString(CultureInfo.InvariantCulture),
            "gathered_from" => NormalizeCorrelationValue(row.GatheredFrom),
            "gathered_log_name" => NormalizeCorrelationValue(row.GatheredLogName),
            _ => string.Empty
        };
    }

    private static string NormalizeCorrelationValue(string? value) {
        if (value is null) {
            return string.Empty;
        }

        string nonNullValue = value;
        var trimmed = nonNullValue.Trim();
        return trimmed.Length == 0 ? string.Empty : trimmed;
    }

    private static string BuildCorrelationToken(IReadOnlyDictionary<string, string> correlation) {
        if (correlation.Count == 0) {
            return "uncorrelated";
        }

        var estimatedChars = 16;
        foreach (var key in correlation.Keys) {
            var value = correlation[key];
            estimatedChars += key.Length + (string.IsNullOrWhiteSpace(value) ? 8 : value.Length) + 2;
        }

        var sb = new StringBuilder(estimatedChars);
        foreach (var key in correlation.Keys.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)) {
            var value = correlation[key];
            if (sb.Length > 0) {
                sb.Append('|');
            }

            sb.Append(key);
            sb.Append('=');
            sb.Append(string.IsNullOrWhiteSpace(value) ? "<empty>" : value);
        }

        return sb.ToString();
    }

    private static string BuildCorrelationId(string token) {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return BitConverter.ToString(hash, 0, CorrelationIdHashBytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static DateTime? ParseUtc(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var offset)) {
            return offset.UtcDateTime;
        }

        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)) {
            return null;
        }

        return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
    }

    private static DateTime FloorToBucket(DateTime valueUtc, int bucketMinutes) {
        var utc = valueUtc.Kind == DateTimeKind.Utc ? valueUtc : valueUtc.ToUniversalTime();
        var bucketTicks = TimeSpan.FromMinutes(bucketMinutes).Ticks;
        if (bucketTicks <= 0) {
            return utc;
        }

        var flooredTicks = utc.Ticks - (utc.Ticks % bucketTicks);
        return new DateTime(flooredTicks, DateTimeKind.Utc);
    }

    private static Dictionary<string, object?> ExtractPayload(EventObjectSlim item) {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var type = item.GetType();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if (!ShouldIncludeField(field)) {
                continue;
            }

            var value = field.GetValue(item);
            payload[ToSnakeCase(field.Name)] = NormalizeValue(value);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!ShouldIncludeProperty(property)) {
                continue;
            }

            var key = ToSnakeCase(property.Name);
            if (payload.ContainsKey(key)) {
                continue;
            }

            object? value;
            try {
                value = property.GetValue(item);
            } catch (Exception ex) {
                Debug.WriteLine($"[NamedEventsTimelineQueryExecutor] Failed to read payload property '{property.Name}': {ex.Message}");
                continue;
            }

            payload[key] = NormalizeValue(value);
        }

        return payload;
    }

    private static Dictionary<string, object?> ProjectPayload(
        Dictionary<string, object?> payload,
        HashSet<string>? payloadKeySet) {
        if (payloadKeySet is null || payloadKeySet.Count == 0) {
            return payload;
        }

        var projected = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in payloadKeySet) {
            if (payload.TryGetValue(key, out var value)) {
                projected[key] = value;
            }
        }

        return projected;
    }

    private static string? ReadPayloadString(IReadOnlyDictionary<string, object?> payload, string key) {
        if (!payload.TryGetValue(key, out var value) || value is null) {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string? ReadPayloadUtc(IReadOnlyDictionary<string, object?> payload, string key) {
        if (!payload.TryGetValue(key, out var value) || value is null) {
            return null;
        }

        if (value is string text && DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedOffset)) {
            return parsedOffset.UtcDateTime.ToString("O");
        }

        if (value is DateTime dateTime) {
            return dateTime.ToUniversalTime().ToString("O");
        }

        return value.ToString();
    }

    private static bool ShouldIncludeField(FieldInfo field) {
        if (field.Name.StartsWith("_", StringComparison.Ordinal)) {
            return false;
        }

        if (string.Equals(field.Name, nameof(EventObjectSlim.EventID), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.Name, nameof(EventObjectSlim.RecordID), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.Name, nameof(EventObjectSlim.GatheredFrom), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.Name, nameof(EventObjectSlim.GatheredLogName), StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (string.Equals(field.FieldType.Name, "EventObject", StringComparison.Ordinal)) {
            return false;
        }

        return true;
    }

    private static bool ShouldIncludeProperty(PropertyInfo property) {
        if (!property.CanRead || property.GetMethod is null || !property.GetMethod.IsPublic) {
            return false;
        }

        return property.GetIndexParameters().Length == 0;
    }

    private static object? NormalizeValue(object? value) {
        if (value is null) {
            return null;
        }

        if (value is DateTime dateTime) {
            return dateTime.ToUniversalTime().ToString("O");
        }

        if (value is DateTimeOffset dateTimeOffset) {
            return dateTimeOffset.ToUniversalTime().ToString("O");
        }

        if (value is Enum enumValue) {
            return enumValue.ToString();
        }

        return value;
    }

    private static string ToSnakeCase(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (!char.IsLetterOrDigit(c)) {
                if (sb.Length > 0 && sb[sb.Length - 1] != '_') {
                    sb.Append('_');
                }
                continue;
            }

            if (i > 0) {
                var prev = value[i - 1];
                var next = i + 1 < value.Length ? value[i + 1] : '\0';

                var shouldSplitUpper =
                    char.IsUpper(c) &&
                    (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && next != '\0' && char.IsLower(next)));
                var shouldSplitDigit = char.IsDigit(c) && !char.IsDigit(prev);
                var shouldSplitLetter = char.IsLetter(c) && char.IsDigit(prev);

                if ((shouldSplitUpper || shouldSplitDigit || shouldSplitLetter) && sb.Length > 0 && sb[sb.Length - 1] != '_') {
                    sb.Append('_');
                }
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Trim('_');
    }

    private sealed class EventRowAccumulator {
        public EventRowAccumulator(
            string namedEvent,
            string ruleType,
            int eventId,
            long? recordId,
            string gatheredFrom,
            string gatheredLogName,
            string? whenUtc,
            DateTime? whenUtcDate,
            string? who,
            string? objectAffected,
            string? computer,
            string? action,
            Dictionary<string, object?> payload) {
            NamedEvent = namedEvent;
            RuleType = ruleType;
            EventId = eventId;
            RecordId = recordId;
            GatheredFrom = gatheredFrom;
            GatheredLogName = gatheredLogName;
            WhenUtc = whenUtc;
            WhenUtcDate = whenUtcDate;
            Who = who;
            ObjectAffected = objectAffected;
            Computer = computer;
            Action = action;
            Payload = payload;
        }

        public string NamedEvent { get; }
        public string RuleType { get; }
        public int EventId { get; }
        public long? RecordId { get; }
        public string GatheredFrom { get; }
        public string GatheredLogName { get; }
        public IReadOnlyDictionary<string, string> Correlation { get; set; } = new Dictionary<string, string>();
        public string? WhenUtc { get; }
        public DateTime? WhenUtcDate { get; }
        public string? Who { get; }
        public string? ObjectAffected { get; }
        public string? Computer { get; }
        public string? Action { get; }
        public Dictionary<string, object?> Payload { get; }
    }

    private sealed class GroupAccumulator {
        public string CorrelationId { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> Correlation { get; set; } = new Dictionary<string, string>();
        public int EventCount { get; set; }
        public DateTime? FirstSeenUtc { get; set; }
        public DateTime? LastSeenUtc { get; set; }
        public HashSet<string> NamedEvents { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<int> EventIds { get; } = new();
        public HashSet<string> Machines { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class BucketAccumulator {
        public DateTime BucketStartUtc { get; set; }
        public int EventCount { get; set; }
        public HashSet<string> CorrelationIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

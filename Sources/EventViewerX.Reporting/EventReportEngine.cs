using System.Collections.Concurrent;
using System.Reflection;

namespace EventViewerX.Reporting;

/// <summary>Runs one optimized query and produces a reusable report snapshot.</summary>
public static class EventReportEngine {
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TypedProperties = new();

    /// <summary>Queries and materializes an event report.</summary>
    public static async Task<EventReport> QueryAsync(EventReportRequest request, CancellationToken cancellationToken = default) {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }
        request.Validate();
        var stopwatch = Stopwatch.StartNew();
        List<EventReportRow> rows;
        List<EventReportCoverage> coverage;
        long scanned = 0;
        bool scanLimitReached = false;

        if (request.Types != null && request.Types.Count > 0) {
            var info = new EventTypeQueryExecutionInfo();
            EventTypeQuery query = CreateTypedQuery(request);
            rows = new List<EventReportRow>();
            await foreach (EventTypeRecord record in EventTypeEngine.ReadAsync(query, info, cancellationToken)) {
                rows.Add(Project(record));
            }
            scanned = info.EventsScanned;
            scanLimitReached = info.ScanLimitReached;
            coverage = BuildTypedCoverage(request, info);
        } else if (request.Definition != null) {
            rows = new List<EventReportRow>();
            var info = new EventDefinitionQueryExecutionInfo();
            var query = new EventDefinitionQuery(request.Definition) {
                Paths = request.Paths,
                MachineNames = request.Collectors != null && request.Collectors.Count > 0 ? request.Collectors : request.MachineNames,
                CollectorLogName = request.Collectors != null && request.Collectors.Count > 0 ? request.CollectorLogName : null,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                TimePeriod = request.TimePeriod,
                RecordIds = request.RecordIds,
                MaxEvents = request.MaxEvents,
                MaxCandidates = request.MaxCandidates,
                MaxConcurrency = request.MaxConcurrency,
                Oldest = request.Oldest,
                Credential = request.Credential,
                Authentication = request.Authentication,
                ContinueOnRemoteFailure = request.ContinueOnRemoteFailure
            };
            await foreach (CustomEventRecord record in EventDefinitionEngine.ReadAsync(query, info, cancellationToken)) {
                rows.Add(Project(record));
            }
            scanned = info.EventsScanned;
            scanLimitReached = info.ScanLimitReached;
            coverage = BuildCustomCoverage(request, info);
        } else {
            (rows, coverage) = await QueryGenericAsync(request, cancellationToken);
            scanned = rows.Count;
        }
        stopwatch.Stop();
        string title = string.IsNullOrWhiteSpace(request.Title)
            ? request.Types != null && request.Types.Count > 0
                ? string.Join(", ", request.Types.Select(static type => type.ToString()))
                : request.Definition != null
                    ? string.IsNullOrWhiteSpace(request.Definition.DisplayName) ? request.Definition.Name : request.Definition.DisplayName
                : request.Paths != null && request.Paths.Count > 0
                    ? $"{request.Paths.Count} offline event log{(request.Paths.Count == 1 ? string.Empty : "s")}"
                    : $"{request.LogName} events"
            : request.Title!.Trim();
        return new EventReport(title, DateTime.UtcNow, stopwatch.Elapsed, rows, coverage, scanned, scanLimitReached);
    }

    /// <summary>Creates a report snapshot from previously queried EventViewerX objects without reading logs again.</summary>
    public static EventReport Create(IEnumerable<object> input, string? title = null) {
        if (input == null) {
            throw new ArgumentNullException(nameof(input));
        }
        var rows = new List<EventReportRow>();
        foreach (object item in input) {
            rows.Add(CreateRow(item));
        }
        List<EventReportCoverage> coverage = rows
            .GroupBy(static row => row.CollectorComputer + "\0" + row.SourceLog, StringComparer.OrdinalIgnoreCase)
            .Select(static group => {
                EventReportRow first = group.First();
                return new EventReportCoverage {
                    MachineName = first.CollectorComputer,
                    LogName = first.SourceLog,
                    Succeeded = true,
                    Status = "Supplied",
                    Detail = string.Empty
                };
            }).ToList();
        return new EventReport(string.IsNullOrWhiteSpace(title) ? "EventViewerX events" : title!.Trim(),
            DateTime.UtcNow, TimeSpan.Zero, rows, coverage, rows.Count, scanLimitReached: false);
    }

    /// <summary>Normalizes one generic, built-in typed, or custom event without querying the event log.</summary>
    public static EventReportRow CreateRow(object input) {
        return input switch {
            EventTypeRecord typed => Project(typed),
            EventObject source => Project(source),
            CustomEventRecord custom => Project(custom),
            null => throw new ArgumentNullException(nameof(input)),
            _ => throw new ArgumentException(
                $"Unsupported report input type '{input.GetType().FullName}'. Expected EventObject, EventTypeRecord, or CustomEventRecord.",
                nameof(input))
        };
    }

    private static EventTypeQuery CreateTypedQuery(EventReportRequest request) {
        bool collectors = request.Collectors != null && request.Collectors.Count > 0;
        return new EventTypeQuery(request.Types!) {
            Paths = request.Paths,
            MachineNames = collectors ? request.Collectors : request.MachineNames,
            CollectorLogName = collectors ? request.CollectorLogName : null,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TimePeriod = request.TimePeriod,
            SourceRecordIds = request.RecordIds,
            MaxEvents = request.MaxEvents,
            MaxCandidates = request.MaxCandidates,
            MaxConcurrency = request.MaxConcurrency,
            Oldest = request.Oldest,
            ReadMode = EventReadMode.StructuredDataAndMessage,
            Credential = request.Credential,
            Authentication = request.Authentication,
            ContinueOnRemoteFailure = request.ContinueOnRemoteFailure,
            Enrichment = request.ResolveDns ? new EventEnrichmentOptions { ResolveDns = true } : null
        };
    }

    private static async Task<(List<EventReportRow> Rows, List<EventReportCoverage> Coverage)> QueryGenericAsync(
        EventReportRequest request, CancellationToken cancellationToken) {
        (DateTime? startTime, DateTime? endTime) = EventTimeRange.Resolve(request.StartTime, request.EndTime, request.TimePeriod);
        EventFilter filter = new() {
            EventIds = request.EventIds?.ToArray(),
            RecordIds = request.RecordIds?.ToArray(),
            StartTime = startTime,
            EndTime = endTime
        };
        if (request.Paths != null && request.Paths.Count > 0) {
            EventLogFileQuery[] files = request.Paths.Select(path => new EventLogFileQuery(Path.GetFullPath(path)) {
                XPath = EventFilterCompiler.BuildXPath(filter),
                Oldest = request.Oldest,
                ReadMode = EventReadMode.StructuredDataAndMessage
            }).ToArray();
            EventLogBatchQuery fileBatch = EventLogBatchQuery.ForFiles(files);
            fileBatch.MaxEvents = request.MaxEvents;
            fileBatch.MaxConcurrency = request.MaxConcurrency;
            var fileRows = new List<EventReportRow>();
            await foreach (EventObject record in EventLogEngine.ReadBatchAsync(fileBatch, cancellationToken)) {
                fileRows.Add(Project(record));
            }
            List<EventReportCoverage> fileCoverage = files.Select(static file => new EventReportCoverage {
                MachineName = "Offline",
                LogName = file.Path,
                Succeeded = true,
                Status = "Succeeded",
                Detail = string.Empty
            }).ToList();
            return (fileRows, fileCoverage);
        }
        string?[] targets = request.MachineNames == null || request.MachineNames.Count == 0
            ? new string?[] { null }
            : request.MachineNames.ToArray();
        var failures = new List<EventLogQueryFailure>();
        EventLogChannelQuery[] channels = targets.Select(target => new EventLogChannelQuery(request.LogName!) {
            MachineName = target,
            Credential = string.IsNullOrWhiteSpace(target) ? null : request.Credential,
            Authentication = request.Authentication,
            XPath = EventFilterCompiler.BuildXPath(filter),
            Oldest = request.Oldest,
            ReadMode = EventReadMode.StructuredDataAndMessage
        }).ToArray();
        EventLogBatchQuery batch = EventLogBatchQuery.ForChannels(channels);
        batch.MaxEvents = request.MaxEvents;
        batch.MaxConcurrency = request.MaxConcurrency;
        batch.ContinueOnError = request.ContinueOnRemoteFailure;
        batch.FailureHandler = failure => {
            if (EventLogRemoteQueryFailureClassifier.TryClassify(failure.MachineName, failure.Exception,
                    out EventLogRemoteQueryFailureKind kind)) {
                failures.Add(failure);
                return;
            }
            throw failure.Exception;
        };
        var rows = new List<EventReportRow>();
        await foreach (EventObject record in EventLogEngine.ReadBatchAsync(batch, cancellationToken)) {
            rows.Add(Project(record));
        }
        var coverage = targets.Select(target => {
            string machine = string.IsNullOrWhiteSpace(target) ? Environment.MachineName : target!;
            EventLogQueryFailure? failure = failures.FirstOrDefault(item =>
                string.Equals(item.MachineName, machine, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Source, request.LogName, StringComparison.OrdinalIgnoreCase));
            EventLogRemoteQueryFailureKind failureKind = EventLogRemoteQueryFailureKind.None;
            if (failure != null) {
                EventLogRemoteQueryFailureClassifier.TryClassify(
                    failure.MachineName, failure.Exception, out failureKind);
            }
            return new EventReportCoverage {
                MachineName = machine,
                LogName = request.LogName!,
                Succeeded = failure == null,
                Status = failure == null ? "Succeeded" : failureKind.ToString(),
                Detail = failure?.Exception.Message ?? string.Empty
            };
        }).ToList();
        return (rows, coverage);
    }

    private static EventReportRow Project(EventTypeRecord record) {
        PropertyInfo[] properties = TypedProperties.GetOrAdd(record.GetType(), static type => type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0 &&
                property.DeclaringType != typeof(EventTypeRecord) && property.Name != nameof(EventTypeRecord.SourceEvent))
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .ToArray());
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo property in properties) {
            values[property.Name] = property.GetValue(record);
        }
        EventObject source = record.SourceEvent;
        return new EventReportRow {
            TimeCreated = source.TimeCreated,
            Type = record.TypeName,
            EventId = source.Id,
            RecordId = source.RecordId,
            Provider = source.ProviderName,
            SourceLog = source.OriginalLogName,
            ContainerLog = source.ContainerLogName,
            SourceComputer = source.SourceComputer,
            CollectorComputer = source.CollectorComputer,
            Level = source.LevelDisplayName,
            Message = source.Message,
            Values = values
        };
    }

    private static EventReportRow Project(EventObject source) => new() {
        TimeCreated = source.TimeCreated,
        Type = "Generic",
        EventId = source.Id,
        RecordId = source.RecordId,
        Provider = source.ProviderName,
        SourceLog = source.OriginalLogName,
        ContainerLog = source.ContainerLogName,
        SourceComputer = source.SourceComputer,
        CollectorComputer = source.CollectorComputer,
        Level = source.LevelDisplayName,
        Message = source.Message,
        Values = source.Data.ToDictionary(static item => item.Key, static item => (object?)item.Value, StringComparer.OrdinalIgnoreCase)
    };

    private static EventReportRow Project(CustomEventRecord record) {
        EventObject source = record.SourceEvent;
        return new EventReportRow {
            TimeCreated = source.TimeCreated,
            Type = record.TypeName,
            EventId = source.Id,
            RecordId = source.RecordId,
            Provider = source.ProviderName,
            SourceLog = source.OriginalLogName,
            ContainerLog = source.ContainerLogName,
            SourceComputer = source.SourceComputer,
            CollectorComputer = source.CollectorComputer,
            Level = source.LevelDisplayName,
            Message = source.Message,
            Values = record.Values
        };
    }

    private static List<EventReportCoverage> BuildCustomCoverage(
        EventReportRequest request,
        EventDefinitionQueryExecutionInfo info) {
        if (request.Paths != null && request.Paths.Count > 0) {
            return request.Paths.Select(static path => new EventReportCoverage {
                MachineName = "Offline",
                LogName = Path.GetFullPath(path),
                Succeeded = true,
                Status = "Succeeded",
                Detail = string.Empty
            }).ToList();
        }
        IReadOnlyList<string?> targets = request.Collectors ?? request.MachineNames ?? new string?[] { null };
        return (from target in targets
                from source in request.Definition!.Sources
                let machine = string.IsNullOrWhiteSpace(target) ? Environment.MachineName : target!
                let queriedLog = request.Collectors != null ? request.CollectorLogName : source.LogName
                let failure = info.TargetFailures.FirstOrDefault(item =>
                    string.Equals(item.MachineName, machine, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.LogName, queriedLog, StringComparison.OrdinalIgnoreCase))
                select new EventReportCoverage {
                    MachineName = machine,
                    LogName = source.LogName,
                    Succeeded = failure == null,
                    Status = failure?.Kind.ToString() ?? "Succeeded",
                    Detail = failure?.Message ?? string.Empty
                }).ToList();
    }

    private static List<EventReportCoverage> BuildTypedCoverage(EventReportRequest request, EventTypeQueryExecutionInfo info) {
        if (request.Paths != null && request.Paths.Count > 0) {
            return request.Paths.Select(static path => new EventReportCoverage {
                MachineName = "Offline",
                LogName = Path.GetFullPath(path),
                Succeeded = true,
                Status = "Succeeded",
                Detail = string.Empty
            }).ToList();
        }
        IReadOnlyList<string?> targets = request.Collectors ?? request.MachineNames ?? new string?[] { null };
        IReadOnlyList<EventSourceDefinition> sources = EventTypeCatalog.GetSources(request.Types!);
        var failures = info.TargetFailures;
        var result = new List<EventReportCoverage>();
        foreach (string? target in targets) {
            string machine = string.IsNullOrWhiteSpace(target) ? Environment.MachineName : target!;
            foreach (EventSourceDefinition source in sources) {
                EventLogQueryTargetFailure? failure = failures.FirstOrDefault(item =>
                    string.Equals(item.MachineName, machine, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.LogName, request.Collectors != null ? request.CollectorLogName : source.LogName, StringComparison.OrdinalIgnoreCase));
                result.Add(new EventReportCoverage {
                    MachineName = machine,
                    LogName = source.LogName,
                    Succeeded = failure == null,
                    Status = failure?.Kind.ToString() ?? "Succeeded",
                    Detail = failure?.Message ?? string.Empty
                });
            }
        }
        return result;
    }
}

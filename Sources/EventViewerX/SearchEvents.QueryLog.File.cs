using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    /// <summary>
    /// Reads events from an EVTX file with optional filtering (IDs, provider, level, keywords, time, data).
    /// </summary>
    /// <param name="filePath">Path to the .evtx file (relative or absolute).</param>
    /// <param name="eventIds">Event IDs to include.</param>
    /// <param name="providerName">Provider name to include.</param>
    /// <param name="keywords">Keyword mask to include.</param>
    /// <param name="level">Event level to include.</param>
    /// <param name="startTime">Earliest event time.</param>
    /// <param name="endTime">Latest event time.</param>
    /// <param name="userId">User SID to include.</param>
    /// <param name="maxEvents">Maximum events to return (0 = all).</param>
    /// <param name="eventRecordId">Specific record IDs to include.</param>
    /// <param name="timePeriod">Relative time window (overrides start/end).</param>
    /// <param name="oldest">If true, read from oldest to newest.</param>
    /// <param name="namedDataFilter">Hashtable of EventData name/value filters to include.</param>
    /// <param name="namedDataExcludeFilter">Hashtable of EventData name/value filters to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="readMode">Amount of provider data to materialize for each event.</param>
    /// <param name="minimumEventRecordIdExclusive">Optional native record-ID lower bound.</param>
    /// <returns>Enumerable sequence of <see cref="EventObject"/> read from the file.</returns>
    /// <remarks>
    /// Unlimited queries that require multiple XPath chunks stream bounded chunk batches; ordering is preserved
    /// within each batch. A positive <paramref name="maxEvents"/> keeps a bounded global merge in the requested direction.
    /// </remarks>
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable? namedDataFilter = null, System.Collections.Hashtable? namedDataExcludeFilter = null, CancellationToken cancellationToken = default, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }
        if (maxEvents < 0) {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Maximum events must be greater than or equal to zero.");
        }
        // Sanitize and resolve path; allow UNC and relative.
        string sanitizedPath = filePath.Trim().Trim('"', '\'');
        string absolutePath = Path.GetFullPath(sanitizedPath);

        bool fileExists = File.Exists(absolutePath);
        if (!fileExists) {
            throw new FileNotFoundException($"The log file '{absolutePath}' does not exist.", absolutePath);
        }

        if (eventIds != null && eventIds.Any(id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }

        if (eventRecordId != null && eventRecordId.Any(id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordId));
        }

        if (timePeriod.HasValue) {
            (DateTime? periodStart, DateTime? periodEnd, TimeSpan? lastPeriod) = TimeHelper.GetTimePeriod(timePeriod.Value);
            startTime = lastPeriod.HasValue ? DateTime.Now.Subtract(lastPeriod.Value) : periodStart;
            endTime = periodEnd;
        }

        int fixedExpressionCount = CountFixedQueryExpressions(providerName, keywords, level, startTime, endTime, userId, timePeriod: null, namedDataFilter, namedDataExcludeFilter);
        Func<string?, long?>? minimumResolver = minimumEventRecordIdExclusive.HasValue ? _ => minimumEventRecordIdExclusive : null;
        IEnumerable<QueryWorkItem> workItems = BuildQueryWorkItems(
            new List<string?> { null },
            eventIds,
            eventRecordId,
            fixedExpressionCount,
            minimumResolver);

        if (oldest && minimumResolver != null) {
            List<Func<int, IReadOnlyList<EventObject>>> pageReaders = workItems
                .Select(workItem => CreateCheckpointPageReader(
                    workItem,
                    pageWorkItem => FilterQueryWorkItemResults(
                        pageWorkItem,
                        QueryLogFileChunk(
                            absolutePath,
                            pageWorkItem.EventIds,
                            providerName,
                            keywords,
                            level,
                            startTime,
                            endTime,
                            userId,
                            pageWorkItem.EventRecordIds,
                            true,
                            namedDataFilter,
                            namedDataExcludeFilter,
                            cancellationToken,
                            readMode,
                            pageWorkItem.MinimumEventRecordIdExclusive)).GetEnumerator()))
                .ToList();
            if (pageReaders.Count == 0) {
                yield break;
            }

            int returned = 0;
            foreach (EventObject eventObject in MergePagedSources(
                         pageReaders,
                         static (left, right) => CompareEvents(left, right, oldest: true),
                         GetCheckpointCandidatePageSize(pageReaders.Count),
                         cancellationToken)) {
                yield return eventObject;
                returned++;
                if (maxEvents > 0 && returned >= maxEvents) {
                    yield break;
                }
            }
            yield break;
        }

        foreach (EventObject eventObject in MergeQueryWorkItems(
                     workItems,
                     workItem => FilterQueryWorkItemResults(
                         workItem,
                         QueryLogFileChunk(
                             absolutePath,
                             workItem.EventIds,
                             providerName,
                             keywords,
                             level,
                             startTime,
                             endTime,
                             userId,
                             workItem.EventRecordIds,
                             oldest,
                             namedDataFilter,
                             namedDataExcludeFilter,
                             cancellationToken,
                             readMode,
                             workItem.MinimumEventRecordIdExclusive)).GetEnumerator(),
                     maxEvents,
                     oldest,
                     cancellationToken,
                     MaxSequentialOpenQueries)) {
            yield return eventObject;
        }
    }

    private static IEnumerable<EventObject> QueryLogFileChunk(
        string absolutePath,
        List<int>? eventIds,
        string? providerName,
        Keywords? keywords,
        Level? level,
        DateTime? startTime,
        DateTime? endTime,
        string? userId,
        List<long>? eventRecordId,
        bool oldest,
        System.Collections.Hashtable? namedDataFilter,
        System.Collections.Hashtable? namedDataExcludeFilter,
        CancellationToken cancellationToken,
        EventReadMode readMode,
        long? minimumEventRecordIdExclusive) {

        string xpath = BuildWinEventFilter(
            id: eventIds?.Select(static id => id.ToString()).ToArray(),
            eventRecordId: eventRecordId?.Select(static id => id.ToString()).ToArray(),
            startTime: startTime,
            endTime: endTime,
            providerName: !string.IsNullOrEmpty(providerName) ? new[] { providerName! } : null,
            keywords: keywords.HasValue ? new[] { (long)keywords.Value } : null,
            level: level.HasValue ? new[] { level.Value.ToString() } : null,
            userId: !string.IsNullOrEmpty(userId) ? new[] { userId! } : null,
            namedDataFilter: namedDataFilter != null ? new[] { namedDataFilter } : null,
            namedDataExcludeFilter: namedDataExcludeFilter != null ? new[] { namedDataExcludeFilter } : null,
            xpathOnly: true,
            minimumEventRecordIdExclusive: minimumEventRecordIdExclusive);

        _logger.WriteVerbose($"QueryLogFile: path '{absolutePath}', xpath '{xpath}'");
        var query = new EventLogQuery(absolutePath, PathType.FilePath, xpath) {
            ReverseDirection = !oldest,
            TolerateQueryErrors = false
        };
        var fallbackQuery = CreateFileFallbackQuery(absolutePath, xpath, oldest);
        EventLogReader? primaryReader;
        try {
            primaryReader = CreateEventLogReader(query, null);
        } catch (EventLogException) {
            primaryReader = null;
        }

        if (primaryReader == null) {
            using var fallbackReader = CreateEventLogReader(fallbackQuery, null);
            if (fallbackReader == null) {
                yield break;
            }
            using CancellationTokenRegistration fallbackCancellation = RegisterReaderCancellation(fallbackReader, cancellationToken);

            while (true) {
                EventRecord? record = ReadEventWithCancellation(
                    fallbackReader,
                    timeoutMs: 0,
                    $"Reading EVTX file '{absolutePath}'",
                    cancellationToken);
                if (record == null) {
                    break;
                }

                yield return new EventObject(record, absolutePath, readMode);
            }

            yield break;
        }

        using (primaryReader) {
            using CancellationTokenRegistration primaryCancellation = RegisterReaderCancellation(primaryReader, cancellationToken);
            // Some runtimes return a valid reader but yield no events for FilePath queries on specific EVTX files.
            // If this happens, retry with the QueryList fallback.
            EventRecord? record = ReadEventWithCancellation(
                primaryReader,
                timeoutMs: 0,
                $"Reading EVTX file '{absolutePath}'",
                cancellationToken);
            if (record == null) {
                using var fallbackReader = CreateEventLogReader(fallbackQuery, null);
                if (fallbackReader == null) {
                    yield break;
                }
                using CancellationTokenRegistration fallbackCancellation = RegisterReaderCancellation(fallbackReader, cancellationToken);

                while (true) {
                    record = ReadEventWithCancellation(
                        fallbackReader,
                        timeoutMs: 0,
                        $"Reading EVTX file '{absolutePath}'",
                        cancellationToken);
                    if (record == null) {
                        break;
                    }

                    yield return new EventObject(record, absolutePath, readMode);
                }

                yield break;
            }

            while (true) {
                yield return new EventObject(record, absolutePath, readMode);

                record = ReadEventWithCancellation(
                    primaryReader,
                    timeoutMs: 0,
                    $"Reading EVTX file '{absolutePath}'",
                    cancellationToken);
                if (record == null) {
                    break;
                }
            }
        }
    }

    private static EventLogQuery CreateFileFallbackQuery(string absolutePath, string xpath, bool oldest) {
        string escapedPath = EscapeXmlValue(absolutePath);
        string escapedXPath = EscapeXmlValue(xpath);
        string filePath = $"file://{escapedPath}";
        string queryString = $"<QueryList><Query Id='0' Path='{filePath}'><Select Path='{filePath}'>{escapedXPath}</Select></Query></QueryList>";
        return new EventLogQuery(null, PathType.LogName, queryString) {
            ReverseDirection = !oldest,
            TolerateQueryErrors = false
        };
    }
}

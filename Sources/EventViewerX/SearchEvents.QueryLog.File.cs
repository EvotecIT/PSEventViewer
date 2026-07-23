using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading;
using EventViewerX.Native;

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
    /// <param name="resultPredicate">Optional managed predicate applied before <paramref name="maxEvents"/>.</param>
    /// <param name="candidateObserver">Optional observer invoked for every native candidate before managed filtering.</param>
    /// <returns>Enumerable sequence of <see cref="EventObject"/> read from the file.</returns>
    /// <remarks>
    /// Unlimited queries that require multiple XPath chunks stream bounded chunk batches; ordering is preserved
    /// within each batch. A positive <paramref name="maxEvents"/> preserves monotonic native record order across
    /// XPath chunks. Provider timestamps are not used to reorder records within the EVTX source because they can move backwards.
    /// </remarks>
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable? namedDataFilter = null, System.Collections.Hashtable? namedDataExcludeFilter = null, CancellationToken cancellationToken = default, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null, Func<EventObject, bool>? resultPredicate = null, Action<EventObject>? candidateObserver = null) {
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
            minimumResolver,
            reserveRecordIdPagingBoundary: maxEvents > 0 && !(oldest && minimumResolver != null));
        Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator = workItem => {
            IEnumerator<EventObject> queryResults = FilterQueryWorkItemResults(
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
                    workItem.MinimumEventRecordIdExclusive,
                    workItem.MaximumEventRecordIdExclusive)).GetEnumerator();
            return resultPredicate == null && candidateObserver == null
                ? queryResults
                : ObserveAndFilterQueryResults(queryResults, resultPredicate, candidateObserver).GetEnumerator();
        };

        if (maxEvents > 0 || (oldest && minimumResolver != null)) {
            List<QueryWorkItem> pagedWorkItems = workItems.ToList();
            if (pagedWorkItems.Count == 1) {
                foreach (EventObject eventObject in MergeQueryWorkItems(
                             pagedWorkItems,
                             createEnumerator,
                             maxEvents,
                             oldest,
                             cancellationToken,
                             MaxSequentialOpenQueries)) {
                    yield return eventObject;
                }
                yield break;
            }

            int boundedPageSize = maxEvents > 0
                ? GetBoundedCandidatePageSize(1, maxEvents)
                : 0;
            List<Func<int, IReadOnlyList<EventObject>>> pageReaders = CreateRecordOrderedSourcePageReaders(
                pagedWorkItems,
                createEnumerator,
                oldest,
                cancellationToken,
                boundedPageSize);
            if (pageReaders.Count == 0) {
                yield break;
            }

            int pageSize = maxEvents > 0
                ? boundedPageSize
                : GetCheckpointCandidatePageSize(pageReaders.Count);
            int returned = 0;
            foreach (EventObject eventObject in MergePagedSources(
                         pageReaders,
                         (left, right) => CompareEvents(left, right, oldest),
                         pageSize,
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
                     createEnumerator,
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
        long? minimumEventRecordIdExclusive,
        long? maximumEventRecordIdExclusive) {

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
            minimumEventRecordIdExclusive: minimumEventRecordIdExclusive,
            maximumEventRecordIdExclusive: maximumEventRecordIdExclusive);

        _logger.WriteVerbose($"QueryLogFile: path '{absolutePath}', xpath '{xpath}'");
        if (readMode == EventReadMode.Metadata) {
            foreach (NativeEventMetadata metadata in WindowsEventFileReader.ReadMetadata(
                         absolutePath,
                         xpath,
                         oldest,
                         cancellationToken)) {
                yield return new EventObject(metadata, absolutePath, absolutePath);
            }
            yield break;
        }
        if (readMode == EventReadMode.Message) {
            foreach (NativeEventMessage message in WindowsEventFileReader.ReadMessages(
                         absolutePath,
                         xpath,
                         oldest,
                         cancellationToken)) {
                yield return new EventObject(message, absolutePath, absolutePath);
            }
            yield break;
        }
        if (readMode == EventReadMode.StructuredData) {
            foreach (NativeEventStructured structured in WindowsEventFileReader.ReadStructured(
                         absolutePath,
                         xpath,
                         oldest,
                         cancellationToken)) {
                yield return new EventObject(structured, absolutePath, absolutePath);
            }
            yield break;
        }
        if (readMode == EventReadMode.Full) {
            foreach (NativeEventFull full in WindowsEventFileReader.ReadFull(
                         absolutePath,
                         xpath,
                         oldest,
                         cancellationToken)) {
                yield return new EventObject(full, absolutePath, absolutePath);
            }
            yield break;
        }

        var query = new EventLogQuery(absolutePath, PathType.FilePath, xpath) {
            ReverseDirection = !oldest,
            TolerateQueryErrors = false
        };
        var fallbackQuery = CreateFileFallbackQuery(absolutePath, xpath, oldest);
        using EventLogPropertySelector? metadataSelector = readMode == EventReadMode.Metadata
            ? EventObject.CreateMetadataPropertySelector()
            : null;
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

                yield return metadataSelector != null
                    ? EventObject.CreateMetadata(record, metadataSelector, absolutePath, absolutePath)
                    : new EventObject(record, absolutePath, readMode);
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

                    yield return metadataSelector != null
                        ? EventObject.CreateMetadata(record, metadataSelector, absolutePath, absolutePath)
                        : new EventObject(record, absolutePath, readMode);
                }

                yield break;
            }

            while (true) {
                yield return metadataSelector != null
                    ? EventObject.CreateMetadata(record, metadataSelector, absolutePath, absolutePath)
                    : new EventObject(record, absolutePath, readMode);

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

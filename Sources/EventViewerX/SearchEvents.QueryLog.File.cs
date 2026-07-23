using System.IO;
using System.Globalization;
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
    /// <param name="messageCulture">Culture used to format provider messages and display names.</param>
    /// <returns>Enumerable sequence of <see cref="EventObject"/> read from the file.</returns>
    /// <remarks>
    /// Unlimited queries that require multiple XPath chunks stream bounded chunk batches; ordering is preserved
    /// within each batch. A positive <paramref name="maxEvents"/> preserves monotonic native record order across
    /// XPath chunks. Provider timestamps are not used to reorder records within the EVTX source because they can move backwards.
    /// </remarks>
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable? namedDataFilter = null, System.Collections.Hashtable? namedDataExcludeFilter = null, CancellationToken cancellationToken = default, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null, Func<EventObject, bool>? resultPredicate = null, Action<EventObject>? candidateObserver = null, CultureInfo? messageCulture = null) {
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
                    workItem.MaximumEventRecordIdExclusive,
                    messageCulture)).GetEnumerator();
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
        long? maximumEventRecordIdExclusive,
        CultureInfo? messageCulture) {

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
        WindowsEventNativeMethods.QueryFlags nativeFlags =
            WindowsEventNativeMethods.QueryFlags.FilePath |
            (oldest
                ? WindowsEventNativeMethods.QueryFlags.ForwardDirection
                : WindowsEventNativeMethods.QueryFlags.ReverseDirection);
        var nativeQuery = new NativeEventQuery(
            IntPtr.Zero,
            absolutePath,
            xpath,
            nativeFlags,
            absolutePath,
            absolutePath,
            messageCulture?.LCID ?? 0);
        foreach (EventObject eventObject in WindowsEventReader.Read(
                     nativeQuery,
                     readMode,
                     absolutePath,
                     absolutePath,
                     cancellationToken)) {
            yield return eventObject;
        }
    }
}

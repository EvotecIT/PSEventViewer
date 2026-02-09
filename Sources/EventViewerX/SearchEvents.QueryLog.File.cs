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
    /// <returns>Enumerable sequence of <see cref="EventObject"/> read from the file.</returns>
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable? namedDataFilter = null, System.Collections.Hashtable? namedDataExcludeFilter = null, CancellationToken cancellationToken = default) {
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

        // Check if we have any filters that require an XML query
        bool hasFilters = namedDataFilter != null || namedDataExcludeFilter != null || eventIds != null ||
                         !string.IsNullOrEmpty(providerName) || keywords != null || level != null || startTime != null ||
                         endTime != null || userId != null || eventRecordId != null;

        string xpath;
        EventLogQuery query;

        // Use XPath only; path is supplied via EventLogQuery FilePath
        if (hasFilters) {
            var namedDataFilterArray = namedDataFilter != null ? new[] { namedDataFilter! } : null;
            var namedDataExcludeFilterArray = namedDataExcludeFilter != null ? new[] { namedDataExcludeFilter! } : null;
            var idArray = eventIds?.Select(i => i.ToString()).ToArray();
            var eventRecordIdArray = eventRecordId?.Select(i => i.ToString()).ToArray();
            var providerNameArray = !string.IsNullOrEmpty(providerName) ? new[] { providerName! } : null;
            var keywordsArray = keywords != null ? new[] { (long)keywords.Value } : null;
            var levelArray = level != null ? new[] { level.Value.ToString() } : null;
            var userIdArray = !string.IsNullOrEmpty(userId) ? new[] { userId! } : null;

            xpath = BuildWinEventFilter(
                id: idArray,
                eventRecordId: eventRecordIdArray,
                startTime: startTime,
                endTime: endTime,
                providerName: providerNameArray,
                keywords: keywordsArray,
                level: levelArray,
                userId: userIdArray,
                namedDataFilter: namedDataFilterArray,
                namedDataExcludeFilter: namedDataExcludeFilterArray,
                xpathOnly: true
            );

            if (string.IsNullOrWhiteSpace(xpath)) {
                xpath = "*";
            }

            _logger.WriteVerbose($"QueryLogFile: path '{absolutePath}', exists: True, xpath '{xpath}', mode: complex");

            query = new EventLogQuery(absolutePath, PathType.FilePath, xpath) {
                ReverseDirection = !oldest,
                TolerateQueryErrors = true
            };
        } else {
            xpath = BuildWinEventFilter(xpathOnly: true);

            if (string.IsNullOrWhiteSpace(xpath)) {
                xpath = "*";
            }

            _logger.WriteVerbose($"QueryLogFile: path '{absolutePath}', exists: True, xpath '{xpath}', mode: simple");

            query = new EventLogQuery(absolutePath, PathType.FilePath, xpath) {
                ReverseDirection = !oldest,
                TolerateQueryErrors = true
            };
        }

        var fallbackQuery = CreateFileQueryWithFallback(absolutePath, xpath, oldest);
        var primaryReader = CreateEventLogReader(query, null);

        if (primaryReader == null) {
            using var fallbackReader = CreateEventLogReader(fallbackQuery, null);
            if (fallbackReader == null) {
                yield break;
            }

            int eventCount = 0;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                var record = fallbackReader.ReadEvent();
                if (record == null) {
                    break;
                }

                yield return new EventObject(record, filePath);
                eventCount++;
                if (maxEvents > 0 && eventCount >= maxEvents) {
                    break;
                }
            }

            yield break;
        }

        using (primaryReader) {
            int eventCount = 0;

            // Some runtimes return a valid reader but yield no events for FilePath queries on specific EVTX files.
            // If this happens, retry with the QueryList fallback.
            var record = primaryReader.ReadEvent();
            if (record == null) {
                using var fallbackReader = CreateEventLogReader(fallbackQuery, null);
                if (fallbackReader == null) {
                    yield break;
                }

                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();

                    record = fallbackReader.ReadEvent();
                    if (record == null) {
                        break;
                    }

                    yield return new EventObject(record, filePath);
                    eventCount++;
                    if (maxEvents > 0 && eventCount >= maxEvents) {
                        break;
                    }
                }

                yield break;
            }

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new EventObject(record, filePath);
                eventCount++;
                if (maxEvents > 0 && eventCount >= maxEvents) {
                    break;
                }

                record = primaryReader.ReadEvent();
                if (record == null) {
                    break;
                }
            }
        }
    }

    private static EventLogQuery CreateFileQueryWithFallback(string absolutePath, string xpath, bool oldest) {
        try {
            return new EventLogQuery(absolutePath, PathType.FilePath, xpath) {
                ReverseDirection = !oldest,
                TolerateQueryErrors = true
            };
        } catch (System.Diagnostics.Eventing.Reader.EventLogNotFoundException) {
            // Fall back to QueryList XML with embedded path (works on some systems/PS builds)
            string queryString = BuildWinEventFilter(path: absolutePath, xpathOnly: false);
            return new EventLogQuery(null, PathType.LogName, queryString) {
                ReverseDirection = !oldest,
                TolerateQueryErrors = true
            };
        }
    }
}

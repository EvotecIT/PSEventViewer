using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable namedDataFilter = null, System.Collections.Hashtable namedDataExcludeFilter = null, CancellationToken cancellationToken = default) {

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
            var namedDataFilterArray = namedDataFilter != null ? new[] { namedDataFilter } : null;
            var namedDataExcludeFilterArray = namedDataExcludeFilter != null ? new[] { namedDataExcludeFilter } : null;
            var idArray = eventIds?.Select(i => i.ToString()).ToArray();
            var eventRecordIdArray = eventRecordId?.Select(i => i.ToString()).ToArray();
            var providerNameArray = !string.IsNullOrEmpty(providerName) ? new[] { providerName } : null;
            var keywordsArray = keywords != null ? new[] { (long)keywords.Value } : null;
            var levelArray = level != null ? new[] { level.Value.ToString() } : null;
            var userIdArray = !string.IsNullOrEmpty(userId) ? new[] { userId } : null;

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

        using (EventLogReader reader = CreateEventLogReader(query, null) ??
                                       CreateEventLogReader(CreateFileQueryWithFallback(absolutePath, xpath, oldest), null)) {
            if (reader == null) {
                yield break;
            }

            int eventCount = 0;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                var record = reader.ReadEvent();
                if (record == null) {
                    break;
                }

                yield return new EventObject(record, filePath);
                eventCount++;
                if (maxEvents > 0 && eventCount >= maxEvents) {
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

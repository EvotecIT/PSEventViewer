using System.Diagnostics.Eventing.Reader;
using System.IO;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable namedDataFilter = null, System.Collections.Hashtable namedDataExcludeFilter = null, CancellationToken cancellationToken = default) {
        string sanitizedPath = filePath.Trim().Trim('"', '\'');
        string absolutePath = Path.GetFullPath(sanitizedPath);
        if (!File.Exists(absolutePath)) {
            throw new FileNotFoundException($"The log file '{absolutePath}' does not exist.", absolutePath);
        }

        if (eventIds != null && eventIds.Any(id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }

        if (eventRecordId != null && eventRecordId.Any(id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordId));
        }

        bool hasFilters = namedDataFilter != null || namedDataExcludeFilter != null || eventIds != null ||
                         providerName != null || keywords != null || level != null || startTime != null ||
                         endTime != null || userId != null || eventRecordId != null;

        EventLogQuery query;

        if (hasFilters) {
            var namedDataFilterArray = namedDataFilter != null ? new[] { namedDataFilter } : null;
            var namedDataExcludeFilterArray = namedDataExcludeFilter != null ? new[] { namedDataExcludeFilter } : null;
            var idArray = eventIds?.Select(i => i.ToString()).ToArray();
            var eventRecordIdArray = eventRecordId?.Select(i => i.ToString()).ToArray();
            var providerNameArray = !string.IsNullOrEmpty(providerName) ? new[] { providerName } : null;
            var keywordsArray = keywords != null ? new[] { (long)keywords.Value } : null;
            var levelArray = level != null ? new[] { level.ToString() } : null;
            var userIdArray = !string.IsNullOrEmpty(userId) ? new[] { userId } : null;

            string queryString = BuildWinEventFilter(
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
                path: absolutePath,
                xpathOnly: false
            );

            _logger.WriteVerbose($"Querying file '{filePath}' with complex query: {queryString}");

            query = new EventLogQuery(null, PathType.LogName, queryString) {
                ReverseDirection = !oldest
            };
        } else {
            string queryString = BuildWinEventFilter(
                path: absolutePath,
                xpathOnly: false
            );

            _logger.WriteVerbose($"Querying file '{filePath}' with simple query: {queryString}");

            query = new EventLogQuery(null, PathType.LogName, queryString) {
                ReverseDirection = !oldest
            };
        }

        EventRecord record;
        using (EventLogReader reader = CreateEventLogReader(query, filePath)) {
            if (reader != null) {
                int eventCount = 0;
                while (!cancellationToken.IsCancellationRequested && (record = reader.ReadEvent()) != null) {
                    EventObject eventObject = new EventObject(record, filePath);
                    yield return eventObject;
                    eventCount++;
                    if (maxEvents > 0 && eventCount >= maxEvents) {
                        break;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

namespace PSEventViewer {
    public class EventSearching : Settings {
        /// <summary>
        /// Initialize the EventSearching class with an internal logger
        /// </summary>
        /// <param name="internalLogger"></param>
        public EventSearching(InternalLogger internalLogger = null) {
            if (internalLogger != null) {
                _logger = internalLogger;
            }
        }

        /// <summary>
        /// Search for events in the event log
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="eventIds"></param>
        /// <param name="machineName"></param>
        /// <param name="providerName"></param>
        /// <param name="keywords"></param>
        /// <param name="level"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="userId"></param>
        /// <param name="maxEvents"></param>
        /// <returns></returns>
        public IEnumerable<EventObject> QueryLog(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0) {
            string queryString = BuildQueryString(eventIds, providerName, keywords, level, startTime, endTime, userId);

            _logger.WriteVerbose($"Querying log '{logName}' with query: {queryString}");

            EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
            if (machineName != null) {
                query.Session = new EventLogSession(machineName);
            }

            using (EventLogReader reader = new EventLogReader(query)) {
                EventRecord record;
                int eventCount = 0;
                while ((record = reader.ReadEvent()) != null) {
                    using (record) {
                        EventObject eventObject = new EventObject(record);
                        yield return eventObject;
                        eventCount++;
                        if (maxEvents > 0 && eventCount >= maxEvents) {
                            break;
                        }
                    }
                }
            }
        }
        private static string BuildQueryString(List<int> eventIds, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null) {
            StringBuilder queryString = new StringBuilder();

            // Add event IDs to the query
            queryString.AppendFormat("*[System[{0}]", string.Join(" or ", eventIds.Select(id => $"EventID={id}")));

            // Add provider name to the query
            if (!string.IsNullOrEmpty(providerName)) {
                queryString.AppendFormat(" and Provider[@Name='{0}']", providerName);
            }

            // Add keywords to the query
            if (keywords.HasValue) {
                queryString.AppendFormat(" and Keywords={0}", (long)keywords.Value);
            }

            // Add level to the query
            if (level.HasValue) {
                queryString.AppendFormat(" and Level={0}", (int)level.Value);
            }

            // Add time range to the query
            if (startTime.HasValue && endTime.HasValue) {
                queryString.AppendFormat(" and TimeCreated[@SystemTime>='{0:O}' and @SystemTime<='{1:O}']", startTime.Value, endTime.Value);
            }

            // Add user ID to the query
            if (!string.IsNullOrEmpty(userId)) {
                queryString.AppendFormat(" and Security[@UserID='{0}']", userId);
            }

            queryString.Append("]");

            return queryString.ToString();
        }
    }
}

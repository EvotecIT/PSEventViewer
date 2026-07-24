using System;
using System.Collections.Generic;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents {
    /// <summary>
    /// Streams PowerShell operational events through the owned native engine with only structured payload data.
    /// </summary>
    private static IEnumerable<EventObject> QueryPowerShellScriptEvents(
        string logName,
        IReadOnlyCollection<string> eventIds,
        string? machineName,
        string? eventLogPath,
        DateTime? dateFrom,
        DateTime? dateTo,
        int maxEventsScanned,
        CancellationToken cancellationToken) {

        string xpath = BuildWinEventFilter(
            id: eventIds is string[] eventIdArray
                ? eventIdArray
                : new List<string>(eventIds).ToArray(),
            startTime: dateFrom,
            endTime: dateTo,
            xpathOnly: true);

        if (!string.IsNullOrWhiteSpace(eventLogPath)) {
            var fileQuery = new EventLogFileQuery(eventLogPath!) {
                XPath = xpath,
                Oldest = false,
                ReadMode = EventReadMode.StructuredData,
                MaxEvents = maxEventsScanned
            };
            foreach (EventObject eventObject in EventLogEngine.ReadFile(fileQuery, cancellationToken)) {
                yield return eventObject;
            }
            yield break;
        }

        var channelQuery = new EventLogChannelQuery(logName) {
            XPath = xpath,
            MachineName = machineName,
            Oldest = false,
            ReadMode = EventReadMode.StructuredData,
            MaxEvents = maxEventsScanned,
            RemoteConnectionTimeoutMilliseconds = DefaultSessionTimeoutMs,
            RemoteReadTimeoutMilliseconds = DefaultSessionTimeoutMs,
            RpcEndpointPort = Settings.RpcProbePort
        };
        foreach (EventObject eventObject in EventLogEngine.ReadChannel(channelQuery, cancellationToken)) {
            yield return eventObject;
        }
    }
}

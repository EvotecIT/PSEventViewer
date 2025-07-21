using System.Diagnostics;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    public static bool CreateLog(string logName, string sourceName = null, string machineName = null, int maximumKilobytes = 0, OverflowAction overflowAction = OverflowAction.OverwriteAsNeeded, int retentionDays = 7) {
        if (string.IsNullOrEmpty(sourceName)) {
            sourceName = logName;
        }

        try {
            bool exists = string.IsNullOrEmpty(machineName)
                ? EventLog.Exists(logName)
                : EventLog.Exists(logName, machineName);
            if (!exists) {
                var data = new EventSourceCreationData(sourceName, logName);
                if (!string.IsNullOrEmpty(machineName)) {
                    data.MachineName = machineName;
                }
                EventLog.CreateEventSource(data);
            } else if (!EventLog.SourceExists(sourceName, machineName)) {
                var data = new EventSourceCreationData(sourceName, logName);
                if (!string.IsNullOrEmpty(machineName)) {
                    data.MachineName = machineName;
                }
                EventLog.CreateEventSource(data);
            }

            using EventLog log = string.IsNullOrEmpty(machineName)
                ? new EventLog(logName)
                : new EventLog(logName, machineName);
            if (maximumKilobytes > 0) {
                log.MaximumKilobytes = maximumKilobytes;
            }
            log.ModifyOverflowPolicy(overflowAction, retentionDays);
            return true;
        } catch {
            return false;
        }
    }
}

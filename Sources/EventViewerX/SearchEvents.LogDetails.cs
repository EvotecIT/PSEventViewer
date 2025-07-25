﻿using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;

namespace EventViewerX;

/// <summary>
/// Helper methods for retrieving event log configuration details.
/// </summary>
public partial class SearchEvents : Settings {

    public static IEnumerable<EventLogDetails> DisplayEventLogs(string[] listLog = null, string machineName = null) {
        EventLogSession session;
        if (!string.IsNullOrEmpty(machineName)) {
            session = new EventLogSession(machineName);
        } else {
            session = new EventLogSession();
            // let's provide the machine name for the sake of logging
            machineName = GetFQDN();
        }
        var logNames = session.GetLogNames();
        foreach (string logName in logNames) {
            if (listLog != null) {
                // Check if any log matches the pattern
                if (!listLog.Any(log => {
                    // Replace '*' with '.*' and '?' with '.' to convert the wildcard to a regex pattern
                    var regexPattern = "^" + Regex.Escape(log).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    return Regex.IsMatch(logName, regexPattern, RegexOptions.IgnoreCase);
                })) {
                    continue;
                }
            }
            EventLogConfiguration logConfig;
            try {
                logConfig = new EventLogConfiguration(logName, session);
                // Rest of your code...
            } catch (EventLogException ex) {
                logConfig = null;
                _logger.WriteWarning("Couldn't create EventLogConfiguration for " + logName + ". Error: " + ex.Message);
            }
            EventLogInformation logInfoObj;
            try {
                logInfoObj = session.GetLogInformation(logName, PathType.LogName);
            } catch (Exception ex) {
                logInfoObj = null;
                _logger.WriteWarning("Couldn't get log information for " + logName + ". Error: " + ex.Message);
            }

            if (logInfoObj == null) {
                _logger.WriteVerbose("Log information is not available for " + logName);
            }

            if (logConfig == null) {
                _logger.WriteWarning("LogConfig is null for " + logName);
                continue;
            }
            EventLogDetails logDetails = new EventLogDetails(_logger, machineName, logConfig, logInfoObj);
            yield return logDetails;

        }
    }

    public static IEnumerable<EventLogDetails> DisplayEventLogsParallel(string[] listLog = null, List<string> machineNames = null, int maxDegreeOfParallelism = 8) {
        if (machineNames == null || !machineNames.Any()) {
            machineNames = new List<string> { null };
        }

        BlockingCollection<EventLogDetails> logDetailsCollection = new BlockingCollection<EventLogDetails>();

        Task.Factory.StartNew(() => {
            Parallel.ForEach(machineNames, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, machineName => {
                try {
                    _logger.WriteVerbose("Querying event log settings on " + machineName);
                    foreach (var logDetails in DisplayEventLogs(listLog, machineName)) {
                        logDetailsCollection.Add(logDetails);
                    }
                } catch (Exception ex) {
                    _logger.WriteWarning("Error querying event log settings on " + machineName + ". Error: " + ex.Message);
                }
            });
            logDetailsCollection.CompleteAdding();
        });

        while (!logDetailsCollection.IsCompleted) {
            EventLogDetails logDetails;
            while (logDetailsCollection.TryTake(out logDetails)) {
                yield return logDetails;
            }
        }
    }
}
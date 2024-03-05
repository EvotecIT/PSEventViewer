using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventViewer {
    public partial class SearchEvents : Settings {

        public static IEnumerable<EventLogDetails> DisplayEventLogs(string[] listLog = null, string machineName = null) {
            EventLogSession session;
            if (machineName != null) {
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
    public class EventLogDetails {
        public string MachineName { get; set; }
        public string LogName { get; set; }
        public string LogType { get; set; }
        public EventLogIsolation LogIsolation { get; set; }
        public bool IsEnabled { get; set; }
        public bool? IsLogFull { get; set; }
        public long MaximumSizeInBytes { get; set; }
        public string LogFilePath { get; set; }
        public string LogMode { get; set; }
        public string OwningProviderName { get; set; }
        public List<string> ProviderNames { get; set; }
        public string ProviderLevel { get; set; }
        public string ProviderKeywords { get; set; }
        public int ProviderBufferSize { get; set; }
        public int ProviderMinimumNumberOfBuffers { get; set; }
        public int ProviderMaximumNumberOfBuffers { get; set; }
        public int ProviderLatency { get; set; }
        public string ProviderControlGuid { get; set; }
        public DateTime? CreationTime { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public long? FileSize { get; set; }
        public long? FileSizeMaximum;
        public double? FileSizeCurrentMB;
        public double? FileSizeMaximumMB;
        public long? RecordCount { get; set; }
        public long? OldestRecordNumber { get; set; }
        public string SecurityDescriptor { get; set; }
        public bool IsClassicLog { get; set; }

        public DateTime? NewestEvent;
        public DateTime? OldestEvent;
        public int? Attributes { get; set; }

        public EventLogDetails(InternalLogger internalLogger, string machineName, EventLogConfiguration logConfig, EventLogInformation logInfoObj) {
            LogName = logConfig.LogName;
            LogType = logConfig.LogType.ToString();
            IsEnabled = logConfig.IsEnabled;
            MaximumSizeInBytes = logConfig.MaximumSizeInBytes;
            LogFilePath = logConfig.LogFilePath;
            LogIsolation = logConfig.LogIsolation;
            LogMode = logConfig.LogMode.ToString();
            OwningProviderName = logConfig.OwningProviderName;
            try {
                ProviderNames = new List<string>(logConfig.ProviderNames);
            } catch (Exception ex) {
                internalLogger.WriteWarning("Couldn't get provider names for " + LogName + ". Error: " + ex.Message);
                ProviderNames = new List<string>();
            }
            ProviderBufferSize = logConfig.ProviderBufferSize.GetValueOrDefault();
            ProviderMinimumNumberOfBuffers = logConfig.ProviderMinimumNumberOfBuffers.GetValueOrDefault();
            ProviderMaximumNumberOfBuffers = logConfig.ProviderMaximumNumberOfBuffers.GetValueOrDefault();
            ProviderLatency = logConfig.ProviderLatency.GetValueOrDefault();
            ProviderControlGuid = logConfig.ProviderControlGuid.ToString();
            SecurityDescriptor = logConfig.SecurityDescriptor;
            ProviderLevel = logConfig.ProviderLevel.ToString();
            ProviderKeywords = logConfig.ProviderKeywords.ToString();
            IsClassicLog = logConfig.IsClassicLog;

            if (logInfoObj != null) {
                FileSize = logInfoObj.FileSize;
                RecordCount = logInfoObj.RecordCount;
                OldestRecordNumber = logInfoObj.OldestRecordNumber;
                LastAccessTime = logInfoObj.LastAccessTime;
                LastWriteTime = logInfoObj.LastWriteTime;
                CreationTime = logInfoObj.CreationTime;

                FileSizeCurrentMB = ConvertSize(FileSize, "B", "MB", 2);
                IsLogFull = logInfoObj.IsLogFull;
                Attributes = logInfoObj.Attributes;
            }

            FileSizeMaximum = logConfig.MaximumSizeInBytes;
            FileSizeMaximumMB = ConvertSize(FileSizeMaximum, "B", "MB", 2);
            MachineName = machineName;
        }

        private static double ConvertSize(double? value, string fromUnit, string toUnit, int precision) {
            if (!value.HasValue) {
                return 0;
            }

            double size = (double)value.Value;

            switch (fromUnit.ToUpper()) {
                case "B":
                    break;
                case "KB":
                    size *= 1024.0;
                    break;
                case "MB":
                    size *= 1024.0 * 1024.0;
                    break;
                case "GB":
                    size *= 1024.0 * 1024.0 * 1024.0;
                    break;
                case "TB":
                    size *= 1024.0 * 1024.0 * 1024.0 * 1024.0;
                    break;
            }

            switch (toUnit.ToUpper()) {
                case "B":
                    break;
                case "KB":
                    size /= 1024.0;
                    break;
                case "MB":
                    size /= 1024.0 * 1024.0;
                    break;
                case "GB":
                    size /= 1024.0 * 1024.0 * 1024.0;
                    break;
                case "TB":
                    size /= 1024.0 * 1024.0 * 1024.0 * 1024.0;
                    break;
            }

            return Math.Round(size, precision);
        }
    }
}

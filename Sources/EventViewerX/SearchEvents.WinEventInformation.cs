using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

namespace EventViewerX {
    /// <summary>
    /// Utility methods for retrieving basic Windows event log information.
    /// </summary>
    public partial class SearchEvents : Settings {
        public static IEnumerable<WinEventInformation> GetWinEventInformation(string[]? logNames, List<string>? machines, List<string>? filePaths, int maxDegreeOfParallelism = 50) {
            if (machines == null || machines.Count == 0) {
                machines = new List<string> { null };
            }

            if (logNames != null && logNames.Length > 0) {
                foreach (var log in DisplayEventLogsParallel(logNames, machines, maxDegreeOfParallelism)) {
                    yield return ConvertLogDetails(log);
                }
            }

            if (filePaths != null) {
                foreach (var path in filePaths) {
                    yield return ConvertFileDetails(path);
                }
            }
        }

        private static WinEventInformation ConvertLogDetails(EventLogDetails details) {
            var info = new WinEventInformation {
                MachineName = details.MachineName,
                LogName = details.LogName,
                LogType = details.LogType,
                LogIsolation = details.LogIsolation,
                IsEnabled = details.IsEnabled,
                IsLogFull = details.IsLogFull,
                MaximumSizeInBytes = details.MaximumSizeInBytes,
                LogFilePath = details.LogFilePath,
                LogMode = details.LogMode,
                OwningProviderName = details.OwningProviderName,
                ProviderNames = details.ProviderNames,
                ProviderLevel = details.ProviderLevel,
                ProviderKeywords = details.ProviderKeywords,
                ProviderBufferSize = details.ProviderBufferSize,
                ProviderMinimumNumberOfBuffers = details.ProviderMinimumNumberOfBuffers,
                ProviderMaximumNumberOfBuffers = details.ProviderMaximumNumberOfBuffers,
                ProviderLatency = details.ProviderLatency,
                ProviderControlGuid = details.ProviderControlGuid,
                CreationTime = details.CreationTime,
                LastAccessTime = details.LastAccessTime,
                LastWriteTime = details.LastWriteTime,
                FileSize = details.FileSize,
                FileSizeMaximum = details.FileSizeMaximum,
                FileSizeCurrentMB = details.FileSizeCurrentMB,
                FileSizeMaximumMB = details.FileSizeMaximumMB,
                RecordCount = details.RecordCount,
                OldestRecordNumber = details.OldestRecordNumber,
                SecurityDescriptor = details.SecurityDescriptor,
                IsClassicLog = details.IsClassicLog,
                Source = details.MachineName
            };

            // Map LogMode to human-readable EventAction
            info.EventAction = details.LogMode switch {
                "AutoBackup" => "ArchiveTheLogWhenFullDoNotOverwrite",
                "Circular" => "OverwriteEventsAsNeededOldestFirst",
                "Retain" => "DoNotOverwriteEventsClearLogManually",
                _ => "Unknown"
            };

            // Additional size properties for compatibility
            info.FileSizeMB = details.FileSizeCurrentMB;
            info.MaximumSizeMB = details.FileSizeMaximumMB;

            info.ProviderNamesExpanded = string.Join(", ", details.ProviderNames);
            if (!string.IsNullOrEmpty(details.SecurityDescriptor)) {
                try {
                    var sd = new CommonSecurityDescriptor(false, false, details.SecurityDescriptor);
                    info.SecurityDescriptorOwner = sd.Owner?.ToString();
                    info.SecurityDescriptorGroup = sd.Group?.ToString();
                    info.SecurityDescriptorDiscretionaryAcl = sd.DiscretionaryAcl?.ToString();
                    info.SecurityDescriptorSystemAcl = sd.SystemAcl?.ToString();
                } catch {
                }
            }
            info.EventOldest = GetEventTime(details.LogName, details.MachineName, false);
            info.EventNewest = GetEventTime(details.LogName, details.MachineName, true);
            return info;
        }

        private static WinEventInformation ConvertFileDetails(string path) {
            FileInfo fi = new(path);
            DateTime? newest = null;
            DateTime? oldest = null;
            long? newId = null;
            long? oldId = null;
            string machine = string.Empty;
            try {
                var queryOld = new EventLogQuery(path, PathType.FilePath);
                using (var reader = new EventLogReader(queryOld)) {
                    var rec = reader.ReadEvent();
                    if (rec != null) {
                        oldest = rec.TimeCreated;
                        oldId = rec.RecordId;
                        machine = rec.MachineName;
                    }
                }
                var queryNew = new EventLogQuery(path, PathType.FilePath) { ReverseDirection = true };
                using (var reader = new EventLogReader(queryNew)) {
                    var rec = reader.ReadEvent();
                    if (rec != null) {
                        newest = rec.TimeCreated;
                        newId = rec.RecordId;
                        if (!string.IsNullOrEmpty(machine)) {
                            if (rec.MachineName != machine) {
                                machine = string.Join(", ", new[] { machine, rec.MachineName }.Distinct());
                            }
                        } else {
                            machine = rec.MachineName;
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed reading event log file {path}: {ex.Message}");
            }
            long? recordCount = null;
            if (newId.HasValue && oldId.HasValue) {
                recordCount = newId.Value - oldId.Value;
            }
            var info = new WinEventInformation {
                EventNewest = newest,
                EventOldest = oldest,
                FileSize = fi.Length,
                FileSizeMaximum = null,
                FileSizeCurrentMB = ConvertSize(fi.Length),
                FileSizeMaximumMB = ConvertSize(fi.Length),
                IsClassicLog = false,
                IsEnabled = false,
                IsLogFull = false,
                LastAccessTime = fi.LastAccessTime,
                LastWriteTime = fi.LastWriteTime,
                LogFilePath = path,
                LogIsolation = 0,
                LogMode = "N/A",
                LogName = "N/A",
                LogType = "N/A",
                MaximumSizeInBytes = fi.Length,
                MachineName = machine,
                OldestRecordNumber = oldId,
                OwningProviderName = string.Empty,
                ProviderBufferSize = 0,
                ProviderControlGuid = string.Empty,
                ProviderKeywords = string.Empty,
                ProviderLatency = 1000,
                ProviderLevel = string.Empty,
                ProviderMaximumNumberOfBuffers = 16,
                ProviderMinimumNumberOfBuffers = 0,
                ProviderNames = new List<string>(),
                ProviderNamesExpanded = string.Empty,
                RecordCount = recordCount,
                SecurityDescriptor = null,
                Source = "File"
            };

            // Set EventAction for files
            info.EventAction = "N/A";

            // Additional size properties for compatibility
            info.FileSizeMB = ConvertSize(fi.Length);
            info.MaximumSizeMB = ConvertSize(fi.Length);

            return info;
        }

        private static DateTime? GetEventTime(string logName, string machine, bool newest) {
            try {
                var query = new EventLogQuery(logName, PathType.LogName) { ReverseDirection = newest };
                if (!string.IsNullOrEmpty(machine)) {
                    query.Session = new EventLogSession(machine);
                }
                using var reader = new EventLogReader(query);
                var rec = reader.ReadEvent();
                return rec?.TimeCreated;
            } catch {
                return null;
            }
        }

        private static double ConvertSize(long valueBytes) {
            return Math.Round(valueBytes / 1024d / 1024d, 2);
        }
    }
}
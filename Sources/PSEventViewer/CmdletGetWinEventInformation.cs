using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace PSEventViewer {
    [Cmdlet(VerbsCommon.Get, "WinEventInformation")]
    public sealed class CmdletGetWinEventInformation : AsyncPSCmdlet {
        [Alias("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")]
        [Parameter(Mandatory = false)]
        public List<string> Machine { get; set; } = new() { Environment.MachineName };

        [Parameter(Mandatory = false)]
        public List<string> FilePath { get; set; }

        [Alias("LogType", "Log")]
        [Parameter(Mandatory = false)]
        public List<string> LogName { get; set; } = new() { "Security" };

        [Parameter(Mandatory = false)]
        public int MaxRunspaces { get; set; } = 50;

        [Alias("AskDC", "QueryDomainControllers", "AskForest")]
        [Parameter(Mandatory = false)]
        public SwitchParameter RunAgainstDC { get; set; }

        private SearchEvents SearchEvents;
        private InternalLoggerPowerShell Logger;

        protected override Task BeginProcessingAsync() {
            var internalLogger = new InternalLogger(false);
            Logger = new InternalLoggerPowerShell(internalLogger, WriteVerbose, WriteWarning, WriteDebug, WriteError, WriteProgress, WriteInformation);
            SearchEvents = new SearchEvents(internalLogger);
            return Task.CompletedTask;
        }

        protected override Task ProcessRecordAsync() {
            var machines = Machine ?? new List<string>();
            if (RunAgainstDC.IsPresent) {
                try {
                    var forest = System.DirectoryServices.ActiveDirectory.Forest.GetCurrentForest();
                    machines = forest.Domains.Cast<System.DirectoryServices.ActiveDirectory.Domain>()
                        .SelectMany(d => d.DomainControllers.Cast<System.DirectoryServices.ActiveDirectory.DomainController>())
                        .Select(dc => dc.Name).Distinct().ToList();
                } catch {
                    // ignored
                }
            }

            if (machines.Count == 0) {
                machines.Add(null);
            }

            if (LogName != null && LogName.Count > 0) {
                foreach (var log in SearchEvents.DisplayEventLogsParallel(LogName.ToArray(), machines, MaxRunspaces)) {
                    var obj = ConvertLogDetails(log);
                    obj.Properties.Add(new PSNoteProperty("Source", log.MachineName));
                    WriteObject(obj);
                }
            }

            if (FilePath != null) {
                foreach (var path in FilePath) {
                    var obj = ConvertFileDetails(path);
                    WriteObject(obj);
                }
            }
            return Task.CompletedTask;
        }

        private PSObject ConvertLogDetails(EventLogDetails details) {
            PSObject ps = PSObject.AsPSObject(details);
            ps.Properties.Add(new PSNoteProperty("ProviderNamesExpanded", string.Join(", ", details.ProviderNames)));
            if (!string.IsNullOrEmpty(details.SecurityDescriptor)) {
                try {
                    var sd = new CommonSecurityDescriptor(false, false, details.SecurityDescriptor);
                    ps.Properties.Add(new PSNoteProperty("SecurityDescriptorOwner", sd.Owner?.ToString()));
                    ps.Properties.Add(new PSNoteProperty("SecurityDescriptorGroup", sd.Group?.ToString()));
                    ps.Properties.Add(new PSNoteProperty("SecurityDescriptorDiscretionaryAcl", sd.DiscretionaryAcl?.ToString()));
                    ps.Properties.Add(new PSNoteProperty("SecurityDescriptorSystemAcl", sd.SystemAcl?.ToString()));
                } catch { }
            }
            ps.Properties.Add(new PSNoteProperty("EventOldest", GetEventTime(details.LogName, details.MachineName, false)));
            ps.Properties.Add(new PSNoteProperty("EventNewest", GetEventTime(details.LogName, details.MachineName, true)));
            return ps;
        }

        private PSObject ConvertFileDetails(string path) {
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
                Logger.WriteWarning($"Failed reading event log file {path}: {ex.Message}");
            }
            long? recordCount = null;
            if (newId.HasValue && oldId.HasValue) {
                recordCount = newId.Value - oldId.Value;
            }
            PSObject ps = new();
            ps.Properties.Add(new PSNoteProperty("EventNewest", newest));
            ps.Properties.Add(new PSNoteProperty("EventOldest", oldest));
            ps.Properties.Add(new PSNoteProperty("FileSize", fi.Length));
            ps.Properties.Add(new PSNoteProperty("FileSizeMaximum", (long?)null));
            var sizeMb = ConvertSize(fi.Length);
            ps.Properties.Add(new PSNoteProperty("FileSizeCurrentMB", sizeMb));
            ps.Properties.Add(new PSNoteProperty("FileSizeMaximumMB", sizeMb));
            ps.Properties.Add(new PSNoteProperty("IsClassicLog", false));
            ps.Properties.Add(new PSNoteProperty("IsEnabled", false));
            ps.Properties.Add(new PSNoteProperty("IsLogFull", false));
            ps.Properties.Add(new PSNoteProperty("LastAccessTime", fi.LastAccessTime));
            ps.Properties.Add(new PSNoteProperty("LastWriteTime", fi.LastWriteTime));
            ps.Properties.Add(new PSNoteProperty("LogFilePath", path));
            ps.Properties.Add(new PSNoteProperty("LogIsolation", false));
            ps.Properties.Add(new PSNoteProperty("LogMode", "N/A"));
            ps.Properties.Add(new PSNoteProperty("LogName", "N/A"));
            ps.Properties.Add(new PSNoteProperty("LogType", "N/A"));
            ps.Properties.Add(new PSNoteProperty("MaximumSizeInBytes", fi.Length));
            ps.Properties.Add(new PSNoteProperty("MachineName", machine));
            ps.Properties.Add(new PSNoteProperty("OldestRecordNumber", oldId));
            ps.Properties.Add(new PSNoteProperty("OwningProviderName", ""));
            ps.Properties.Add(new PSNoteProperty("ProviderBufferSize", 0));
            ps.Properties.Add(new PSNoteProperty("ProviderControlGuid", ""));
            ps.Properties.Add(new PSNoteProperty("ProviderKeywords", ""));
            ps.Properties.Add(new PSNoteProperty("ProviderLatency", 1000));
            ps.Properties.Add(new PSNoteProperty("ProviderLevel", ""));
            ps.Properties.Add(new PSNoteProperty("ProviderMaximumNumberOfBuffers", 16));
            ps.Properties.Add(new PSNoteProperty("ProviderMinimumNumberOfBuffers", 0));
            ps.Properties.Add(new PSNoteProperty("ProviderNames", ""));
            ps.Properties.Add(new PSNoteProperty("ProviderNamesExpanded", ""));
            ps.Properties.Add(new PSNoteProperty("RecordCount", recordCount));
            ps.Properties.Add(new PSNoteProperty("SecurityDescriptor", null));
            ps.Properties.Add(new PSNoteProperty("SecurityDescriptorOwner", null));
            ps.Properties.Add(new PSNoteProperty("SecurityDescriptorGroup", null));
            ps.Properties.Add(new PSNoteProperty("SecurityDescriptorDiscretionaryAcl", null));
            ps.Properties.Add(new PSNoteProperty("SecurityDescriptorSystemAcl", null));
            ps.Properties.Add(new PSNoteProperty("Source", "File"));
            return ps;
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

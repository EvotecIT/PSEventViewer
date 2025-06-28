using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace EventViewerX {
    /// <summary>
    /// Represents detailed information about an event log or log file.
    /// </summary>
    public class WinEventInformation {
        public string Source { get; set; }
        public string MachineName { get; set; }
        public string LogName { get; set; }
        public string LogType { get; set; }
        public EventLogIsolation LogIsolation { get; set; }
        public bool IsEnabled { get; set; }
        public bool? IsLogFull { get; set; }
        public bool IsClassicLog { get; set; }
        public long MaximumSizeInBytes { get; set; }
        public string LogFilePath { get; set; }
        public string LogMode { get; set; }
        public string EventAction { get; set; }
        public string OwningProviderName { get; set; }
        public List<string> ProviderNames { get; set; }
        public string ProviderNamesExpanded { get; set; }
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
        public long? FileSizeMaximum { get; set; }
        public double? FileSizeCurrentMB { get; set; }
        public double? FileSizeMaximumMB { get; set; }
        public double? FileSizeMB { get; set; }
        public double? MaximumSizeMB { get; set; }
        public long? RecordCount { get; set; }
        public long? OldestRecordNumber { get; set; }
        public string SecurityDescriptor { get; set; }
        public string SecurityDescriptorOwner { get; set; }
        public string SecurityDescriptorGroup { get; set; }
        public string SecurityDescriptorDiscretionaryAcl { get; set; }
        public string SecurityDescriptorSystemAcl { get; set; }
        public DateTime? EventNewest { get; set; }
        public DateTime? EventOldest { get; set; }
    }
}
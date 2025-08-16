using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace EventViewerX {
    /// <summary>
    /// Represents detailed information about an event log or log file.
    /// </summary>
    public class WinEventInformation {
        /// <summary>Provider source of the log.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Name of the machine hosting the log.</summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>Name of the log.</summary>
        public string LogName { get; set; } = string.Empty;

        /// <summary>Type of the log such as Operational or Administrative.</summary>
        public string LogType { get; set; } = string.Empty;

        /// <summary>Isolation level for the log.</summary>
        public EventLogIsolation LogIsolation { get; set; }

        /// <summary>Indicates whether the log is enabled.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>True when the log has reached its maximum size.</summary>
        public bool? IsLogFull { get; set; }

        /// <summary>True if the log is a classic event log.</summary>
        public bool IsClassicLog { get; set; }

        /// <summary>Maximum size of the log in bytes.</summary>
        public long MaximumSizeInBytes { get; set; }

        /// <summary>Path to the log file.</summary>
        public string LogFilePath { get; set; } = string.Empty;

        /// <summary>Current log mode such as Circular or Retain.</summary>
        public string LogMode { get; set; } = string.Empty;

        /// <summary>Action performed on the log.</summary>
        public string EventAction { get; set; } = string.Empty;

        /// <summary>Name of the owning provider.</summary>
        public string OwningProviderName { get; set; } = string.Empty;

        /// <summary>List of provider names for this log.</summary>
        public List<string> ProviderNames { get; set; } = new();

        /// <summary>Expanded list of provider names as a string.</summary>
        public string ProviderNamesExpanded { get; set; } = string.Empty;

        /// <summary>Provider level information.</summary>
        public string ProviderLevel { get; set; } = string.Empty;

        /// <summary>Keywords configured on the provider.</summary>
        public string ProviderKeywords { get; set; } = string.Empty;

        /// <summary>Size of provider buffer.</summary>
        public int ProviderBufferSize { get; set; }

        /// <summary>Minimum number of buffers for the provider.</summary>
        public int ProviderMinimumNumberOfBuffers { get; set; }

        /// <summary>Maximum number of buffers for the provider.</summary>
        public int ProviderMaximumNumberOfBuffers { get; set; }

        /// <summary>Latency setting for the provider.</summary>
        public int ProviderLatency { get; set; }

        /// <summary>Control GUID for the provider.</summary>
        public string ProviderControlGuid { get; set; } = string.Empty;

        /// <summary>File creation time.</summary>
        public DateTime? CreationTime { get; set; }

        /// <summary>Last access time of the log.</summary>
        public DateTime? LastAccessTime { get; set; }

        /// <summary>Last write time of the log.</summary>
        public DateTime? LastWriteTime { get; set; }

        /// <summary>Current size of the file in bytes.</summary>
        public long? FileSize { get; set; }

        /// <summary>Maximum allowed file size in bytes.</summary>
        public long? FileSizeMaximum { get; set; }

        /// <summary>Current file size in megabytes.</summary>
        public double? FileSizeCurrentMB { get; set; }

        /// <summary>Maximum file size in megabytes.</summary>
        public double? FileSizeMaximumMB { get; set; }

        /// <summary>File size as megabytes.</summary>
        public double? FileSizeMB { get; set; }

        /// <summary>Maximum size allowed in megabytes.</summary>
        public double? MaximumSizeMB { get; set; }

        /// <summary>Number of records contained in the log.</summary>
        public long? RecordCount { get; set; }

        /// <summary>Oldest record number in the log.</summary>
        public long? OldestRecordNumber { get; set; }

        /// <summary>Security descriptor of the log.</summary>
        public string SecurityDescriptor { get; set; } = string.Empty;

        /// <summary>Owner from the security descriptor.</summary>
        public string SecurityDescriptorOwner { get; set; } = string.Empty;

        /// <summary>Group from the security descriptor.</summary>
        public string SecurityDescriptorGroup { get; set; } = string.Empty;

        /// <summary>DACL from the security descriptor.</summary>
        public string SecurityDescriptorDiscretionaryAcl { get; set; } = string.Empty;

        /// <summary>SACL from the security descriptor.</summary>
        public string SecurityDescriptorSystemAcl { get; set; } = string.Empty;

        /// <summary>Date of the newest event in the log.</summary>
        public DateTime? EventNewest { get; set; }

        /// <summary>Date of the oldest event in the log.</summary>
        public DateTime? EventOldest { get; set; }
    }
}
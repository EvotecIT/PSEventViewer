using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Threading;

namespace EventViewerX {
    /// <summary>
    /// Represents details of a PowerShell engine start event.
    /// </summary>
    public class PowerShellScriptExecutionInfo {
        private static int _executionCount;

        /// <summary>
        /// Gets the sequential index of this execution.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Resets internal state used to track executions.
        /// </summary>
        public static void ResetState() {
            Interlocked.Exchange(ref _executionCount, 0);
        }
        /// <summary>
        /// Underlying event record containing script execution details.
        /// </summary>
        public EventRecord EventRecord { get; }

        /// <summary>
        /// Parsed data values extracted from the event.
        /// </summary>
        public IDictionary<string, string?> Data { get; }

        internal PowerShellScriptExecutionInfo(EventRecord record, IDictionary<string, string?> data) {
            EventRecord = record;
            Data = data;
            Index = Interlocked.Increment(ref _executionCount);
        }
    }

    /// <summary>
    /// Represents a reconstructed PowerShell script from event logs.
    /// </summary>
    public class RestoredPowerShellScript {
        /// <summary>
        /// Identifier of the script block.
        /// </summary>
        public string ScriptBlockId { get; set; }

        /// <summary>
        /// Full script text reconstructed from events.
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Event records that compose the script.
        /// </summary>
        public IReadOnlyList<EventRecord> Events { get; set; }

        /// <summary>
        /// Primary event record for convenience access.
        /// </summary>
        public EventRecord EventRecord => Events?[0];

        /// <summary>
        /// Parsed data dictionary from the event.
        /// </summary>
        public IDictionary<string, string?> Data { get; set; }

        /// <summary>
        /// Saves the script to the specified directory.
        /// </summary>
        public string Save(string directory, bool addComment = true, bool unblock = false) {
            Directory.CreateDirectory(directory);
            string fileName = $"{EventRecord.MachineName}_{ScriptBlockId}.ps1";
            string filePath = Path.Combine(directory, fileName);
            if (addComment) {
                var header = string.Join(Environment.NewLine,
                    "<#",
                    $"RecordID = {EventRecord.RecordId}",
                    $"LogName = {EventRecord.LogName}",
                    $"MachineName = {EventRecord.MachineName}",
                    $"TimeCreated = {EventRecord.TimeCreated}",
                    "#>");
                File.WriteAllText(filePath, header + Environment.NewLine + Script);
            } else {
                File.WriteAllText(filePath, Script);
            }
            if (!unblock) {
                File.WriteAllText(filePath + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3");
            }
            return filePath;
        }
    }
}

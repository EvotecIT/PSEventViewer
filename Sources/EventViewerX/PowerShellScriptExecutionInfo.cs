using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;

namespace EventViewerX {
    /// <summary>
    /// Represents details of a PowerShell engine start event.
    /// </summary>
    public class PowerShellScriptExecutionInfo {
        public EventRecord EventRecord { get; }
        public IDictionary<string, string?> Data { get; }

        internal PowerShellScriptExecutionInfo(EventRecord record, IDictionary<string, string?> data) {
            EventRecord = record;
            Data = data;
        }
    }

    /// <summary>
    /// Represents a reconstructed PowerShell script from event logs.
    /// </summary>
    public class RestoredPowerShellScript {
        public string ScriptBlockId { get; set; }
        public string Script { get; set; }
        public IReadOnlyList<EventRecord> Events { get; set; }
        public EventRecord EventRecord => Events?[0];
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

using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

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
        public EventRecord EventRecord { get; set; }
        public IDictionary<string, string?> Data { get; set; }
    }
}

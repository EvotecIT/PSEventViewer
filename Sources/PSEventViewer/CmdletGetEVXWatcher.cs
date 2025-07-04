using System.Linq;
using System.Management.Automation;

namespace PSEventViewer {
    /// <summary>
    /// Returns information about registered or running watchers.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EVXWatcher")]
    [OutputType(typeof(WatcherSettings))]
    public sealed class CmdletGetEVXWatcher : PSCmdlet {
        [Parameter]
        public SwitchParameter Running { get; set; }

        protected override void ProcessRecord() {
            if (Running.IsPresent) {
                foreach (var kvp in WatcherRegistry.GetRunning().ToArray()) {
                    WriteObject(new {
                        Name = kvp.Key,
                        kvp.Value.Definition.MachineName,
                        kvp.Value.Definition.LogName,
                        kvp.Value.Definition.EventId,
                        kvp.Value.Started
                    });
                }
            } else {
                foreach (var def in WatcherRegistry.GetAllDefinitions()) {
                    WriteObject(def);
                }
            }
        }
    }
}

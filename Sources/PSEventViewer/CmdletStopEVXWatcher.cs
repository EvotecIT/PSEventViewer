using System;
using System.Management.Automation;
using System.Linq;
using EventViewerX;

namespace PSEventViewer {
    /// <summary>
    /// Stops running EVX watchers by identifier or name.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "EVXWatcher")]
    [Alias("Stop-EventViewerXWatcher")]
    public sealed class CmdletStopEVXWatcher : PSCmdlet {
        /// <summary>
        /// Identifiers of watchers to stop.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public Guid[] Id { get; set; }

        /// <summary>
        /// Name of the watcher to stop.
        /// </summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// When set, stops all running watchers.
        /// </summary>
        [Parameter]
        public SwitchParameter All { get; set; }

        /// <summary>
        /// Executes the stop operation based on provided parameters.
        /// </summary>
        protected override void ProcessRecord() {
            if (All.IsPresent) {
                WatcherManager.StopAll();
                return;
            }

            if (!string.IsNullOrEmpty(Name)) {
                WatcherManager.StopWatchersByName(Name);
            }

            if (Id != null) {
                foreach (var guid in Id) {
                    WatcherManager.StopWatcher(guid);
                }
            }
        }
    }
}

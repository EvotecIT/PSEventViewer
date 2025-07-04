using System;
using System.Management.Automation;
using System.Linq;
using EventViewerX;

namespace PSEventViewer {
    [Cmdlet(VerbsLifecycle.Stop, "EVXWatcher")]
    [Alias("Stop-EventViewerXWatcher")]
    public sealed class CmdletStopEVXWatcher : PSCmdlet {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public Guid[] Id { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter All { get; set; }

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

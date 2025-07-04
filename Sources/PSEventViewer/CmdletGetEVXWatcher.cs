using System;
using System.Linq;
using System.Management.Automation;
using EventViewerX;

namespace PSEventViewer {
    [Cmdlet(VerbsCommon.Get, "EVXWatcher")]
    [Alias("Get-EventViewerXWatcher")]
    [OutputType(typeof(WatcherInfo))]
    public sealed class CmdletGetEVXWatcher : PSCmdlet {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public Guid[] Id { get; set; }

        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord() {
            var watchers = WatcherManager.GetWatchers(Name);
            if (Id != null && Id.Length > 0) {
                watchers = watchers.Where(w => Id.Contains(w.Id)).ToList();
            }
            foreach (var watcher in watchers) {
                WriteObject(watcher);
            }
        }
    }
}

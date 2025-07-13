using System;
using System.Linq;
using System.Management.Automation;
using EventViewerX;

namespace PSEventViewer {
    /// <summary>
    /// Retrieves information about active EVX watchers.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EVXWatcher")]
    [Alias("Get-EventViewerXWatcher")]
    [OutputType(typeof(WatcherInfo))]
    public sealed class CmdletGetEVXWatcher : PSCmdlet {
        /// <summary>
        /// Identifiers of watchers to retrieve.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public Guid[] Id { get; set; }

        /// <summary>
        /// Name of the watcher to return.
        /// </summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Outputs watcher information matching the provided parameters.
        /// </summary>
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

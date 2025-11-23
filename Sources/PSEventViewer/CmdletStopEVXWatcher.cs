using System;
using System.Management.Automation;
using System.Linq;
using EventViewerX;

namespace PSEventViewer {
    /// <summary>
    /// <para type="synopsis">Stops running EVX watchers by identifier, name, or en masse.</para>
    /// <para type="description">Supports stopping specific watcher instances, all watchers with a given name, or every watcher currently active.</para>
    /// </summary>
    /// <example>
    ///   <summary>Stop by Id</summary>
    ///   <code>Stop-EVXWatcher -Id 7b4b6d2c-6c2e-47e1-9c3a-1b5a0a4b9d11</code>
    ///   <para>Stops the watcher with the specified identifier.</para>
    /// </example>
    /// <example>
    ///   <summary>Stop by name</summary>
    ///   <code>Stop-EVXWatcher -Name SecurityWatcher</code>
    ///   <para>Stops all watchers whose name matches SecurityWatcher.</para>
    /// </example>
    /// <example>
    ///   <summary>Stop everything</summary>
    ///   <code>Stop-EVXWatcher -All</code>
    ///   <para>Immediately halts every running watcher.</para>
    /// </example>
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

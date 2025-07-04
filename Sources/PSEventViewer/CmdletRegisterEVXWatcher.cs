using System.Management.Automation;

namespace PSEventViewer {
    /// <summary>
    /// Stores watcher definitions in the global registry for later starting.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "EVXWatcher")]
    [OutputType(typeof(void))]
    public sealed class CmdletRegisterEVXWatcher : PSCmdlet {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string MachineName { get; set; }

        [Parameter(Mandatory = true)]
        public string LogName { get; set; }

        [Parameter(Mandatory = true)]
        public int[] EventId { get; set; }

        [Parameter(Mandatory = true)]
        public ScriptBlock Action { get; set; }

        [Parameter]
        [ValidateRange(1, 1024)]
        public int NumberOfThreads { get; set; } = 8;

        protected override void ProcessRecord() {
            var def = new WatcherSettings {
                Name = Name,
                MachineName = MachineName,
                LogName = LogName,
                EventId = EventId,
                Action = Action,
                NumberOfThreads = NumberOfThreads
            };
            WatcherRegistry.Register(def);
        }
    }
}

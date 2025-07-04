using System.Management.Automation;

namespace PSEventViewer {
    /// <summary>
    /// Stops a running watcher and removes it from the registry.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "EVXWatcher", SupportsShouldProcess = true)]
    [OutputType(typeof(bool))]
    public sealed class CmdletRemoveEVXWatcher : PSCmdlet {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter DefinitionOnly { get; set; }

        protected override void ProcessRecord() {
            bool result = false;
            if (DefinitionOnly.IsPresent) {
                result = WatcherRegistry.RemoveDefinition(Name);
            } else {
                result = WatcherRegistry.Stop(Name) || WatcherRegistry.RemoveDefinition(Name);
            }
            WriteObject(result);
        }
    }
}

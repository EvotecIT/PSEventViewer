using System.Diagnostics.Eventing.Reader;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>
/// Returns a list of available Windows event log providers.
/// </summary>
[Cmdlet(VerbsCommon.Get, "EVXProviderList")]
[Alias("Get-EventViewerXProviderList")]
[OutputType(typeof(string))]
public sealed class CmdletGetEVXProviderList : AsyncPSCmdlet
{
    /// <summary>
    /// Retrieves available event log providers from the local system.
    /// </summary>
    protected override Task ProcessRecordAsync()
    {
        using EventLogSession session = new();
        foreach (string provider in session.GetProviderNames())
        {
            WriteObject(provider);
        }
        return Task.CompletedTask;
    }
}

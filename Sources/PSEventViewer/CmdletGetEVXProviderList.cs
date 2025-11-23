using System.Diagnostics.Eventing.Reader;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Returns a list of available Windows event log providers.</para>
/// <para type="description">Enumerates provider names from the local EventLogSession for use in filters and diagnostic queries.</para>
/// </summary>
/// <example>
///   <summary>List all providers</summary>
///   <code>Get-EVXProviderList</code>
///   <para>Outputs provider names such as Microsoft-Windows-Security-Auditing.</para>
/// </example>
/// <example>
///   <summary>Find providers containing IIS</summary>
///   <code>Get-EVXProviderList | Where-Object { $_ -like '*IIS*' }</code>
///   <para>Filters the list to IIS-related providers.</para>
/// </example>
/// <example>
///   <summary>Use in filter construction</summary>
///   <code>$prov = Get-EVXProviderList | Where-Object { $_ -eq 'Microsoft-Windows-DHCP-Server' }</code>
///   <para>Captures a provider name for later use in Get-EVXFilter.</para>
/// </example>
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

using System;
using System.IO;
using System.Reflection;
using System.Management.Automation.Runspaces;

/// <summary>
/// OnModuleImportAndRemove is a class that implements the IModuleAssemblyInitializer and IModuleAssemblyCleanup interfaces.
/// This class is used to handle the assembly resolve event when the module is imported and removed.
/// </summary>
public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup {
    private Guid _ownerId;

    /// <summary>
    /// OnImport is called when the module is imported.
    /// </summary>
    public void OnImport() {
        _ownerId = Guid.NewGuid();
        using (PowerShell powerShell = PowerShell.Create(RunspaceMode.CurrentRunspace)) {
            powerShell.AddCommand("Set-Variable")
                .AddParameter("Name", PSEventViewer.PowerShellWatcherRegistry.OwnerVariableName)
                .AddParameter("Value", _ownerId)
                .AddParameter("Scope", "Global");
            powerShell.Invoke();
            if (powerShell.HadErrors) {
                throw powerShell.Streams.Error[0].Exception;
            }
        }
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve += MyResolveEventHandler;
        }
    }

    /// <summary>
    /// OnRemove is called when the module is removed.
    /// </summary>
    /// <param name="module"></param>
    public void OnRemove(PSModuleInfo module) {
        Guid ownerId = GetCurrentOwnerId();
        PSEventViewer.PowerShellWatcherRegistry.StopAllOwned(ownerId);
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve -= MyResolveEventHandler;
        }
    }

    private Guid GetCurrentOwnerId() {
        using PowerShell powerShell = PowerShell.Create(RunspaceMode.CurrentRunspace);
        powerShell.AddCommand("Get-Variable")
            .AddParameter("Name", PSEventViewer.PowerShellWatcherRegistry.OwnerVariableName)
            .AddParameter("Scope", "Global")
            .AddParameter("ValueOnly");
        PSObject? result = powerShell.Invoke().FirstOrDefault();
        powerShell.Commands.Clear();
        powerShell.AddCommand("Remove-Variable")
            .AddParameter("Name", PSEventViewer.PowerShellWatcherRegistry.OwnerVariableName)
            .AddParameter("Scope", "Global")
            .AddParameter("Force")
            .AddParameter("ErrorAction", ActionPreference.SilentlyContinue);
        powerShell.Invoke();
        return result?.BaseObject is Guid ownerId ? ownerId : _ownerId;
    }

    /// <summary>
    /// MyResolveEventHandler is a method that handles the AssemblyResolve event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private static Assembly? MyResolveEventHandler(object? sender, ResolveEventArgs args) {
        var libDirectory = Path.GetDirectoryName(typeof(OnModuleImportAndRemove).Assembly.Location);
        var directoriesToSearch = new List<string>();

        if (!string.IsNullOrEmpty(libDirectory)) {
            directoriesToSearch.Add(libDirectory);

            if (Directory.Exists(libDirectory)) {
                directoriesToSearch.AddRange(Directory.GetDirectories(libDirectory, "*", SearchOption.AllDirectories));
            }
        }

        var requestedAssemblyName = new AssemblyName(args.Name).Name + ".dll";

        foreach (var directory in directoriesToSearch) {
            var assemblyPath = Path.Combine(directory, requestedAssemblyName);

            if (File.Exists(assemblyPath)) {
                try {
                    return Assembly.LoadFrom(assemblyPath);
                } catch (BadImageFormatException ex) {
                    Console.WriteLine($"Failed to load assembly from {assemblyPath}: {ex.Message}");
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Determine if the current runtime is .NET Framework
    /// </summary>
    /// <returns></returns>
    private bool IsNetFramework() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
    }

    // Determine if the current runtime is .NET Core
    private bool IsNetCore() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determine if the current runtime is .NET 5 or higher
    /// </summary>
    /// <returns></returns>
    private bool IsNet5OrHigher() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 5", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 6", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 7", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 8", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 9", StringComparison.OrdinalIgnoreCase);
    }
}

using System;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static void QueryPowerShellScripts() {
            foreach (var script in SearchEvents.GetPowerShellScripts(PowerShellEdition.WindowsPowerShell, format: true)) {
                Console.WriteLine($"[{script.EventRecord?.TimeCreated}] {script.ScriptBlockId}");
                Console.WriteLine(script.Script);
                // Save reconstructed script to disk
                script.Save(@"C:\Temp\Scripts");
            }
        }
    }
}

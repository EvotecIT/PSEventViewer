using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EventViewerX {
    /// <summary>
    /// Helper methods that wrap <c>wevtutil.exe</c> and related utilities.
    /// </summary>
    public class BinaryWrappers {

        /// <summary>
        /// Imports a compiled manifest file using <c>wevtutil.exe</c>.
        /// </summary>
        /// <param name="manifestPath">Path to the manifest file.</param>
        public static void ImportManifest(string manifestPath) {

            //string manifestPath = @"C:\path\to\your\manifest.man";

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "wevtutil.exe",
                Arguments = $"im {manifestPath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try {
                using (Process process = new Process { StartInfo = startInfo }) {
                    if (!process.Start()) {
                        Console.WriteLine("`wevtutil.exe` is not available.");
                        return;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0) {
                        Console.WriteLine("Manifest imported successfully.");
                    } else {
                        Console.WriteLine($"Failed to import manifest. Exit code: {process.ExitCode}");
                        Console.WriteLine($"Output: {output}");
                    }
                }
            } catch (System.ComponentModel.Win32Exception) {
                Console.WriteLine("`wevtutil.exe` is not available.");
            }
        }


        /// <summary>
        /// Compiles a manifest XML file into a <c>.man</c> file using <c>mc.exe</c>.
        /// </summary>
        /// <param name="xmlPath">Path to the manifest XML.</param>
        /// <param name="manPath">Path to the output <c>.man</c> file.</param>
        /// <param name="mcPath">Optional explicit path to <c>mc.exe</c>.</param>
        public static void ConvertXMLtoMAN(string xmlPath, string manPath, string mcPath = null) {
            //string xmlPath = @"C:\path\to\your\manifest.xml";
            //string manPath = @"C:\path\to\output\manifest.man";

            if (mcPath == null) {
                string windowsKitsPath = @"C:\Program Files (x86)\Windows Kits";
                string mcExeName = "mc.exe";

                // Find the mc.exe path
                mcPath = Directory.EnumerateDirectories(windowsKitsPath)
                    .SelectMany(kitPath => Directory.EnumerateDirectories(Path.Combine(kitPath, "bin")))
                    .SelectMany(binPath => Directory.EnumerateFiles(binPath, mcExeName, SearchOption.AllDirectories))
                    .FirstOrDefault();
            }
            if (mcPath == null || !File.Exists(mcPath)) {
                Console.WriteLine("`mc.exe` is not found in the specified paths.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = mcPath,
                Arguments = $"-um {xmlPath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try {
                using (Process process = Process.Start(startInfo)) {
                    if (process == null) {
                        Console.WriteLine("`mc.exe` is not available.");
                        return;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0) {
                        Console.WriteLine("Manifest compiled successfully.");
                    } else {
                        Console.WriteLine($"Failed to compile manifest. Exit code: {process.ExitCode}");
                        Console.WriteLine($"Output: {output}");
                    }
                }
            } catch (System.ComponentModel.Win32Exception) {
                Console.WriteLine("`mc.exe` is not available.");
            }
        }


        /// <summary>
        /// Displays provider names, GUIDs and associated log names.
        /// </summary>
        public static void GetProvidersResults() {
            // Get the list of providers
            string[] providers = GetProviders();

            // For each provider, get its metadata and print its name, GUID, and log names
            foreach (string provider in providers) {
                string metadata = GetProviderMetadata(provider);
                string name = GetMetadataProperty(metadata, "name");
                string guid = GetMetadataProperty(metadata, "guid");
                string[] logNames = GetMetadataProperty(metadata, "channelReferences")?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray() ?? Array.Empty<string>();
                Console.WriteLine($"Provider Name: {name}, Provider GUID: {guid}, Log Names: {string.Join(", ", logNames)}");
            }
        }

        /// <summary>
        /// Retrieves all registered event providers.
        /// </summary>
        /// <returns>Array of provider names.</returns>
        private static string[] GetProviders() {
            return RunCommand("wevtutil.exe", "ep").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets provider metadata text using <c>wevtutil.exe</c>.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <returns>Metadata text.</returns>
        private static string GetProviderMetadata(string provider) {
            return RunCommand("wevtutil.exe", $"gp \"{provider}\"");
        }

        /// <summary>
        /// Extracts a property value from provider metadata text.
        /// </summary>
        /// <param name="metadata">Metadata text.</param>
        /// <param name="propertyName">Property name to search for.</param>
        /// <returns>Property value if found, otherwise <c>null</c>.</returns>
        private static string GetMetadataProperty(string metadata, string propertyName) {
            string marker = propertyName + ":";
            int startIndex = metadata.IndexOf(marker);
            if (startIndex == -1) {
                return null;
            }

            startIndex += marker.Length;
            int endIndex = metadata.IndexOf('\n', startIndex);
            if (endIndex == -1) {
                endIndex = metadata.Length;
            }

            return metadata.Substring(startIndex, endIndex - startIndex).Trim();
        }

        /// <summary>
        /// Executes a command line process and returns the standard output.
        /// </summary>
        /// <param name="fileName">Executable name.</param>
        /// <param name="arguments">Command line arguments.</param>
        /// <returns>Process output.</returns>
        private static string RunCommand(string fileName, string arguments) {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo)) {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EventViewerX {
    public class BinaryWrappers {

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

        private static string[] GetProviders() {
            return RunCommand("wevtutil.exe", "ep").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetProviderMetadata(string provider) {
            return RunCommand("wevtutil.exe", $"gp \"{provider}\"");
        }

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
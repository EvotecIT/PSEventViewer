namespace EventViewerX.Providers;

/// <summary>Locates the newest suitable Windows SDK and MSVC toolchain.</summary>
public static class EventProviderToolchainDiscovery {
    /// <summary>Resolves explicit paths or discovers installed build tools.</summary>
    public static EventProviderToolchain Find(
        EventProviderToolchainOptions? options = null) {

        options ??= new EventProviderToolchainOptions();
        string messageCompiler = ResolveExplicit(
            options.MessageCompilerPath,
            "mc.exe");
        string resourceCompiler = ResolveExplicit(
            options.ResourceCompilerPath,
            "rc.exe");
        string linker = ResolveExplicit(
            options.LinkerPath,
            "link.exe");

        string sdkVersion = string.Empty;
        if (messageCompiler.Length == 0 ||
            resourceCompiler.Length == 0) {
            (string mc, string rc, string version) =
                FindWindowsSdk();
            messageCompiler = messageCompiler.Length == 0
                ? mc
                : messageCompiler;
            resourceCompiler = resourceCompiler.Length == 0
                ? rc
                : resourceCompiler;
            sdkVersion = version;
        } else {
            sdkVersion = Directory.GetParent(
                Directory.GetParent(messageCompiler)!.FullName)!
                .Name;
        }

        string msvcVersion = string.Empty;
        if (linker.Length == 0) {
            (linker, msvcVersion) = FindMsvcLinker();
        } else {
            msvcVersion = TryReadMsvcVersion(linker);
        }

        return new EventProviderToolchain {
            MessageCompilerPath = messageCompiler,
            ResourceCompilerPath = resourceCompiler,
            LinkerPath = linker,
            WindowsSdkVersion = sdkVersion,
            MsvcVersion = msvcVersion
        };
    }

    private static string ResolveExplicit(
        string path,
        string expectedName) {

        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }
        string fullPath = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(path));
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException(
                $"Configured provider build tool '{fullPath}' does not exist.",
                fullPath);
        }
        if (!string.Equals(
                Path.GetFileName(fullPath),
                expectedName,
                StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException(
                $"Expected '{expectedName}', but '{fullPath}' was configured.");
        }
        return fullPath;
    }

    private static (string MessageCompiler, string ResourceCompiler,
        string Version) FindWindowsSdk() {

        var roots = new List<string>();
        AddRoot(
            roots,
            Environment.GetEnvironmentVariable("WindowsSdkDir"));
        AddRoot(
            roots,
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Windows Kits",
                "10"));

        foreach (string root in roots.Distinct(
                     StringComparer.OrdinalIgnoreCase)) {
            string bin = Path.Combine(root, "bin");
            if (!Directory.Exists(bin)) {
                continue;
            }
            foreach (string versionDirectory in Directory
                         .EnumerateDirectories(bin)
                         .OrderByDescending(
                             static path =>
                                 ParseVersion(Path.GetFileName(path)))) {
                string version = Path.GetFileName(versionDirectory);
                string mc = Path.Combine(
                    versionDirectory,
                    "x64",
                    "mc.exe");
                string rc = Path.Combine(
                    versionDirectory,
                    "x64",
                    "rc.exe");
                if (File.Exists(mc) && File.Exists(rc)) {
                    return (mc, rc, version);
                }
            }

            string legacyMc = Path.Combine(bin, "x64", "mc.exe");
            string legacyRc = Path.Combine(bin, "x64", "rc.exe");
            if (File.Exists(legacyMc) &&
                File.Exists(legacyRc)) {
                return (legacyMc, legacyRc, "legacy");
            }
        }
        throw new FileNotFoundException(
            "A Windows SDK containing mc.exe and rc.exe was not found. " +
            "Build provider packages on a Windows SDK/Visual Studio machine " +
            "or supply explicit tool paths. The resulting package does not " +
            "require these tools on target computers.");
    }

    private static (string Linker, string Version) FindMsvcLinker() {
        string? vcTools = Environment.GetEnvironmentVariable(
            "VCToolsInstallDir");
        if (!string.IsNullOrWhiteSpace(vcTools)) {
            string candidate = Path.Combine(
                vcTools,
                "bin",
                "Hostx64",
                "x64",
                "link.exe");
            if (File.Exists(candidate)) {
                return (candidate, Path.GetFileName(
                    vcTools.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)));
            }
        }

        foreach (string visualStudioRoot in GetVisualStudioRoots()) {
            if (!Directory.Exists(visualStudioRoot)) {
                continue;
            }
            foreach (string release in Directory
                         .EnumerateDirectories(visualStudioRoot)
                         .OrderByDescending(static path => path)) {
                foreach (string edition in Directory
                             .EnumerateDirectories(release)
                             .OrderByDescending(static path => path)) {
                    string toolsRoot = Path.Combine(
                        edition,
                        "VC",
                        "Tools",
                        "MSVC");
                    if (!Directory.Exists(toolsRoot)) {
                        continue;
                    }
                    foreach (string versionDirectory in Directory
                                 .EnumerateDirectories(toolsRoot)
                                 .OrderByDescending(
                                     static path =>
                                         ParseVersion(
                                             Path.GetFileName(path)))) {
                        string candidate = Path.Combine(
                            versionDirectory,
                            "bin",
                            "Hostx64",
                            "x64",
                            "link.exe");
                        if (File.Exists(candidate)) {
                            return (
                                candidate,
                                Path.GetFileName(versionDirectory));
                        }
                    }
                }
            }
        }
        throw new FileNotFoundException(
            "The Microsoft MSVC linker was not found. Install the Visual " +
            "C++ build tools on the provider-package build machine or supply " +
            "LinkerPath. A non-Microsoft link.exe from Unix utilities is not suitable.");
    }

    private static IEnumerable<string> GetVisualStudioRoots() {
        yield return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles),
            "Microsoft Visual Studio");
        yield return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio");
    }

    private static void AddRoot(
        ICollection<string> roots,
        string? value) {

        if (!string.IsNullOrWhiteSpace(value)) {
            roots.Add(
                Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(value)));
        }
    }

    private static Version ParseVersion(string value) {
        return Version.TryParse(value, out Version? version)
            ? version
            : new Version(0, 0);
    }

    private static string TryReadMsvcVersion(string linkerPath) {
        DirectoryInfo? directory =
            Directory.GetParent(linkerPath);
        for (int index = 0;
             index < 4 && directory != null;
             index++) {
            if (Version.TryParse(
                    directory.Name,
                    out _)) {
                return directory.Name;
            }
            directory = directory.Parent;
        }
        return string.Empty;
    }
}

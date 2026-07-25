using System.IO.Compression;
using System.Text.Json;

namespace EventViewerX.Providers;

/// <summary>
/// Builds portable, SDK-free-at-deployment provider packages from typed
/// definitions.
/// </summary>
public static class EventProviderPackageBuilder {
    /// <summary>
    /// Validates, compatibility-checks, compiles, hashes, and packages one
    /// provider definition.
    /// </summary>
    public static EventProviderPackageBuildResult Build(
        EventProviderDefinition definition,
        string outputPath,
        EventProviderPackageBuildOptions? options = null) {

        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }
        if (string.IsNullOrWhiteSpace(outputPath)) {
            throw new ArgumentException(
                "Output path cannot be empty.",
                nameof(outputPath));
        }
        options ??= new EventProviderPackageBuildOptions();
        EventProviderValidationResult validation =
            EventProviderDefinitionValidator.ValidateOrThrow(definition);
        if (!string.IsNullOrWhiteSpace(options.BaselinePath)) {
            EventProviderDefinition baseline =
                LoadBaseline(options.BaselinePath);
            EventProviderCompatibility.EnsureCompatible(
                baseline,
                definition);
        }

        EventProviderToolchain toolchain =
            EventProviderToolchainDiscovery.Find(
                options.Toolchain);
        string finalPath = Path.GetFullPath(outputPath);
        string? outputDirectory = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory)) {
            Directory.CreateDirectory(outputDirectory);
        }
        if (File.Exists(finalPath) && !options.Overwrite) {
            throw new IOException(
                $"Provider package '{finalPath}' already exists.");
        }

        string buildRoot = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX",
            "ProviderBuild",
            Guid.NewGuid().ToString("N"));
        string nativeRoot = Path.Combine(buildRoot, "native");
        Directory.CreateDirectory(nativeRoot);
        string temporaryPackage = finalPath + "." +
                                  Guid.NewGuid().ToString("N") +
                                  ".tmp";
        try {
            BuildContents(
                definition,
                buildRoot,
                nativeRoot,
                toolchain,
                options.ToolTimeout);
            EventProviderPackageManifest packageManifest =
                CreatePackageManifest(
                    definition,
                    buildRoot,
                    toolchain);
            if (options.SigningCertificate != null) {
                EventProviderPackageSignature.Sign(
                    packageManifest,
                    options.SigningCertificate);
            }
            File.WriteAllText(
                Path.Combine(
                    buildRoot,
                    EventProviderPackageLayout
                        .PackageManifestFileName),
                JsonSerializer.Serialize(
                    packageManifest,
                    EventProviderDefinitionJson.SerializerOptions),
                new UTF8Encoding(false));
            CreateArchive(
                buildRoot,
                temporaryPackage);
            Promote(
                temporaryPackage,
                finalPath,
                options.Overwrite);

            return new EventProviderPackageBuildResult {
                OutputPath = finalPath,
                ProviderName = definition.Name,
                ProviderId = definition.Id,
                PackageVersion = definition.PackageVersion,
                PackageSha256 =
                    EventProviderHash.FileSha256(finalPath),
                ResourceSha256 =
                    packageManifest.Files[
                        EventProviderPackageLayout
                            .ResourceFileName],
                Toolchain = toolchain,
                Warnings = validation.Warnings,
                IsSigned = options.SigningCertificate != null,
                SignerThumbprint =
                    options.SigningCertificate?.Thumbprint ??
                    string.Empty
            };
        } finally {
            if (File.Exists(temporaryPackage)) {
                File.Delete(temporaryPackage);
            }
            if (Directory.Exists(buildRoot)) {
                Directory.Delete(buildRoot, recursive: true);
            }
        }
    }

    private static void BuildContents(
        EventProviderDefinition definition,
        string buildRoot,
        string nativeRoot,
        EventProviderToolchain toolchain,
        TimeSpan timeout) {

        string definitionPath = Path.Combine(
            buildRoot,
            EventProviderPackageLayout.DefinitionFileName);
        string manifestPath = Path.Combine(
            buildRoot,
            EventProviderPackageLayout.ManifestFileName);
        string schemaLockPath = Path.Combine(
            buildRoot,
            EventProviderPackageLayout.SchemaLockFileName);
        string resourcePath = Path.Combine(
            buildRoot,
            EventProviderPackageLayout.ResourceFileName);

        File.WriteAllText(
            definitionPath,
            EventProviderDefinitionJson.Serialize(definition),
            new UTF8Encoding(false));
        File.WriteAllText(
            schemaLockPath,
            EventProviderSchemaLock.Create(definition).ToJson(),
            new UTF8Encoding(false));
        File.WriteAllText(
            manifestPath,
            EventProviderManifestGenerator.Generate(
                definition,
                EventProviderPackageLayout.ResourceFileName),
            new UTF8Encoding(false));

        EventProviderProcessResult messageCompiler =
            EventProviderProcessRunner.Run(
                toolchain.MessageCompilerPath,
                new[] {
                    "-um",
                    "-h",
                    nativeRoot,
                    "-r",
                    nativeRoot,
                    manifestPath
                },
                buildRoot,
                timeout);
        EventProviderProcessRunner.EnsureSuccess(
            messageCompiler,
            "Windows Message Compiler");

        string resourceScript = Path.Combine(
            nativeRoot,
            "provider.rc");
        if (!File.Exists(resourceScript)) {
            throw new InvalidDataException(
                "Message Compiler completed without producing provider.rc.");
        }
        string compiledResource = Path.Combine(
            nativeRoot,
            "provider.res");
        EventProviderProcessResult resourceCompiler =
            EventProviderProcessRunner.Run(
                toolchain.ResourceCompilerPath,
                new[] {
                    "/nologo",
                    "/fo",
                    compiledResource,
                    resourceScript
                },
                buildRoot,
                timeout);
        EventProviderProcessRunner.EnsureSuccess(
            resourceCompiler,
            "Windows Resource Compiler");

        EventProviderProcessResult linker =
            EventProviderProcessRunner.Run(
                toolchain.LinkerPath,
                new[] {
                    "/nologo",
                    "/dll",
                    "/noentry",
                    "/Brepro",
                    "/machine:x64",
                    "/out:" + resourcePath,
                    compiledResource
                },
                buildRoot,
                timeout);
        EventProviderProcessRunner.EnsureSuccess(
            linker,
            "Microsoft Linker");
        if (!File.Exists(resourcePath) ||
            new FileInfo(resourcePath).Length == 0) {
            throw new InvalidDataException(
                "Provider resource DLL was not produced.");
        }
    }

    private static EventProviderPackageManifest CreatePackageManifest(
        EventProviderDefinition definition,
        string buildRoot,
        EventProviderToolchain toolchain) {

        var files = new SortedDictionary<string, string>(
            StringComparer.Ordinal);
        foreach (string fileName in new[] {
                     EventProviderPackageLayout.DefinitionFileName,
                     EventProviderPackageLayout.ManifestFileName,
                     EventProviderPackageLayout.ResourceFileName,
                     EventProviderPackageLayout.SchemaLockFileName
                 }) {
            files[fileName] = EventProviderHash.FileSha256(
                Path.Combine(buildRoot, fileName));
        }
        return new EventProviderPackageManifest {
            FormatVersion = 1,
            ProviderName = definition.Name,
            ProviderId = definition.Id,
            PackageVersion = definition.PackageVersion,
            WindowsSdkVersion = toolchain.WindowsSdkVersion,
            MsvcVersion = toolchain.MsvcVersion,
            Files = files
        };
    }

    private static void CreateArchive(
        string buildRoot,
        string packagePath) {

        using FileStream output = new(
            packagePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None);
        using var archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            leaveOpen: false);
        foreach (string sourcePath in Directory
                     .EnumerateFiles(
                         buildRoot,
                         "*",
                         SearchOption.TopDirectoryOnly)
                     .OrderBy(
                         static path => Path.GetFileName(path),
                         StringComparer.Ordinal)) {
            string fileName = Path.GetFileName(sourcePath);
            ZipArchiveEntry entry = archive.CreateEntry(
                fileName,
                CompressionLevel.Optimal);
            entry.LastWriteTime =
                EventProviderPackageLayout.StableZipTimestamp;
            using Stream destination = entry.Open();
            using FileStream source = File.OpenRead(sourcePath);
            source.CopyTo(destination);
        }
    }

    private static void Promote(
        string temporaryPath,
        string finalPath,
        bool overwrite) {

        if (File.Exists(finalPath)) {
            if (!overwrite) {
                throw new IOException(
                    $"Provider package '{finalPath}' already exists.");
            }
            File.Replace(
                temporaryPath,
                finalPath,
                null);
        } else {
            File.Move(
                temporaryPath,
                finalPath);
        }
    }

    private static EventProviderDefinition LoadBaseline(string path) {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException(
                "Provider compatibility baseline was not found.",
                fullPath);
        }
        if (string.Equals(
                Path.GetExtension(fullPath),
                ".evxprovider",
                StringComparison.OrdinalIgnoreCase)) {
            using EventProviderPackage package =
                EventProviderPackageReader.Open(fullPath);
            return package.Definition;
        }
        return EventProviderDefinitionJson.Load(fullPath);
    }
}

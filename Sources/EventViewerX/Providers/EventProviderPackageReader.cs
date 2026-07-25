using System.IO.Compression;
using System.Text.Json;

namespace EventViewerX.Providers;

/// <summary>Opens, validates, and safely extracts provider packages.</summary>
public static class EventProviderPackageReader {
    private const long MaximumEntryBytes = 64L * 1024 * 1024;
    private const long MaximumPackageBytes = 128L * 1024 * 1024;

    /// <summary>Opens a package and verifies every declared file hash.</summary>
    public static EventProviderPackage Open(string path) {
        PackageContents contents = Read(path);
        EventProviderDefinition definition =
            EventProviderDefinitionJson.Parse(
                Encoding.UTF8.GetString(
                    contents.Files[
                        EventProviderPackageLayout
                            .DefinitionFileName]));
        EnsureInternalConsistency(
            contents,
            definition);
        if (!string.Equals(
                contents.Manifest.ProviderName,
                definition.Name,
                StringComparison.Ordinal) ||
            contents.Manifest.ProviderId != definition.Id ||
            !string.Equals(
                contents.Manifest.PackageVersion,
                definition.PackageVersion,
                StringComparison.Ordinal)) {
            throw new InvalidDataException(
                "Provider package metadata does not match provider.definition.json.");
        }
        System.Security.Cryptography.X509Certificates.X509Certificate2?
            signerCertificate =
                EventProviderPackageSignature.Verify(contents.Manifest);
        return new EventProviderPackage(
            Path.GetFullPath(path),
            contents.Manifest,
            definition,
            signerCertificate);
    }

    /// <summary>
    /// Extracts a verified package to an empty destination directory.
    /// </summary>
    public static EventProviderPackage Extract(
        string packagePath,
        string destinationPath) {

        if (string.IsNullOrWhiteSpace(destinationPath)) {
            throw new ArgumentException(
                "Destination path cannot be empty.",
                nameof(destinationPath));
        }
        PackageContents contents = Read(packagePath);
        string destination = Path.GetFullPath(destinationPath);
        if (Directory.Exists(destination) &&
            Directory.EnumerateFileSystemEntries(destination).Any()) {
            throw new IOException(
                $"Destination '{destination}' must be empty.");
        }
        Directory.CreateDirectory(destination);
        try {
            foreach (KeyValuePair<string, byte[]> file in
                     contents.Files) {
                File.WriteAllBytes(
                    Path.Combine(destination, file.Key),
                    file.Value);
            }
            File.WriteAllText(
                Path.Combine(
                    destination,
                    EventProviderPackageLayout
                        .PackageManifestFileName),
                JsonSerializer.Serialize(
                    contents.Manifest,
                    EventProviderDefinitionJson.SerializerOptions),
                new UTF8Encoding(false));
            return Open(packagePath);
        } catch {
            if (Directory.Exists(destination)) {
                Directory.Delete(destination, recursive: true);
            }
            throw;
        }
    }

    internal static IReadOnlyDictionary<string, byte[]> ReadVerifiedFiles(
        string packagePath) {

        return Read(packagePath).Files;
    }

    internal static void EnsureExtractedFilesMatch(
        string packagePath,
        string directoryPath) {

        string directory = Path.GetFullPath(directoryPath);
        IReadOnlyDictionary<string, byte[]> files =
            ReadVerifiedFiles(packagePath);
        foreach (KeyValuePair<string, byte[]> expected in files) {
            string path = Path.Combine(directory, expected.Key);
            if (!File.Exists(path) ||
                !string.Equals(
                    EventProviderHash.FileSha256(path),
                    EventProviderHash.BytesSha256(expected.Value),
                    StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidDataException(
                    $"Installed provider file '{expected.Key}' does not match its archived package.");
            }
        }
    }

    private static void EnsureInternalConsistency(
        PackageContents contents,
        EventProviderDefinition definition) {

        string expectedSchemaLock =
            EventProviderSchemaLock.Create(
                definition).ToJson();
        string actualSchemaLock = Encoding.UTF8.GetString(
            contents.Files[
                EventProviderPackageLayout.SchemaLockFileName]);
        if (!string.Equals(
                expectedSchemaLock,
                actualSchemaLock,
                StringComparison.Ordinal)) {
            throw new InvalidDataException(
                "schema-lock.json does not match provider.definition.json.");
        }
        string expectedManifest =
            EventProviderManifestGenerator.Generate(
                definition,
                EventProviderPackageLayout.ResourceFileName);
        string actualManifest = Encoding.UTF8.GetString(
            contents.Files[
                EventProviderPackageLayout.ManifestFileName]);
        if (!string.Equals(
                expectedManifest,
                actualManifest,
                StringComparison.Ordinal)) {
            throw new InvalidDataException(
                "provider.man does not match provider.definition.json.");
        }
    }

    private static PackageContents Read(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException(
                "Provider package path cannot be empty.",
                nameof(path));
        }
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException(
                "Provider package was not found.",
                fullPath);
        }
        if (new FileInfo(fullPath).Length > MaximumPackageBytes) {
            throw new InvalidDataException(
                $"Provider package exceeds {MaximumPackageBytes} bytes.");
        }

        using ZipArchive archive = ZipFile.OpenRead(fullPath);
        var entries = new Dictionary<string, ZipArchiveEntry>(
            StringComparer.Ordinal);
        long declaredExpandedBytes = 0;
        foreach (ZipArchiveEntry entry in archive.Entries) {
            if (entry.FullName.Length == 0 ||
                entry.FullName != Path.GetFileName(entry.FullName) ||
                entry.FullName.IndexOfAny(new[] {
                    '/',
                    '\\'
                }) >= 0) {
                throw new InvalidDataException(
                    $"Provider package entry '{entry.FullName}' is not a safe root file.");
            }
            if (entry.Length > MaximumEntryBytes) {
                throw new InvalidDataException(
                    $"Provider package entry '{entry.FullName}' exceeds {MaximumEntryBytes} bytes.");
            }
            if (entry.Length >
                MaximumPackageBytes -
                declaredExpandedBytes) {
                throw new InvalidDataException(
                    $"Provider package expanded contents exceed {MaximumPackageBytes} bytes.");
            }
            declaredExpandedBytes += entry.Length;
            if (entries.ContainsKey(entry.FullName)) {
                throw new InvalidDataException(
                    $"Provider package entry '{entry.FullName}' is duplicated.");
            }
            entries.Add(entry.FullName, entry);
        }
        if (!entries.TryGetValue(
                EventProviderPackageLayout.PackageManifestFileName,
                out ZipArchiveEntry? packageEntry)) {
            throw new InvalidDataException(
                "Provider package does not contain package.json.");
        }
        long expandedBytes = 0;
        EventProviderPackageManifest manifest =
            JsonSerializer.Deserialize<EventProviderPackageManifest>(
                ReadText(
                    packageEntry,
                    ref expandedBytes),
                EventProviderDefinitionJson.SerializerOptions) ??
            throw new InvalidDataException(
                "Provider package manifest is invalid.");
        if (manifest.FormatVersion != 1) {
            throw new InvalidDataException(
                $"Provider package format {manifest.FormatVersion} is not supported.");
        }
        if (manifest.Files.ContainsKey(
                EventProviderPackageLayout.PackageManifestFileName)) {
            throw new InvalidDataException(
                "Provider package cannot declare package.json as a hashed payload file.");
        }
        string[] requiredFiles = {
            EventProviderPackageLayout.DefinitionFileName,
            EventProviderPackageLayout.ManifestFileName,
            EventProviderPackageLayout.ResourceFileName,
            EventProviderPackageLayout.SchemaLockFileName
        };
        if (manifest.Files.Count != requiredFiles.Length ||
            requiredFiles.Any(fileName =>
                !manifest.Files.ContainsKey(fileName))) {
            throw new InvalidDataException(
                "Provider package format 1 must declare exactly its definition, manifest, resources, and schema lock.");
        }

        var files = new Dictionary<string, byte[]>(
            StringComparer.Ordinal);
        var expectedNames = new HashSet<string>(
            manifest.Files.Keys,
            StringComparer.Ordinal) {
            EventProviderPackageLayout.PackageManifestFileName
        };
        string[] unexpected = entries.Keys
            .Where(name => !expectedNames.Contains(name))
            .ToArray();
        if (unexpected.Length > 0) {
            throw new InvalidDataException(
                "Provider package contains undeclared file(s): " +
                string.Join(", ", unexpected));
        }
        foreach (KeyValuePair<string, string> expected in manifest.Files) {
            if (!entries.TryGetValue(
                    expected.Key,
                    out ZipArchiveEntry? entry)) {
                throw new InvalidDataException(
                    $"Provider package file '{expected.Key}' is missing.");
            }
            byte[] bytes = ReadBytes(
                entry,
                ref expandedBytes);
            string actual = EventProviderHash.BytesSha256(bytes);
            if (!string.Equals(
                    actual,
                    expected.Value,
                    StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidDataException(
                    $"Provider package file '{expected.Key}' failed SHA-256 verification.");
            }
            files.Add(expected.Key, bytes);
        }
        foreach (string required in requiredFiles) {
            if (!files.ContainsKey(required)) {
                throw new InvalidDataException(
                    $"Provider package required file '{required}' is not declared.");
            }
        }
        return new PackageContents(manifest, files);
    }

    private static string ReadText(
        ZipArchiveEntry entry,
        ref long expandedBytes) {

        return Encoding.UTF8.GetString(
            ReadBytes(
                entry,
                ref expandedBytes));
    }

    private static byte[] ReadBytes(
        ZipArchiveEntry entry,
        ref long expandedBytes) {

        using Stream input = entry.Open();
        return ReadBounded(
            input,
            entry.FullName,
            MaximumEntryBytes,
            MaximumPackageBytes,
            ref expandedBytes);
    }

    internal static byte[] ReadBounded(
        Stream input,
        string entryName,
        long maximumEntryBytes,
        long maximumPackageBytes,
        ref long expandedBytes) {

        if (input == null) {
            throw new ArgumentNullException(nameof(input));
        }
        if (maximumEntryBytes <= 0 ||
            maximumPackageBytes <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(maximumEntryBytes),
                "Expanded package limits must be greater than zero.");
        }
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        long entryBytes = 0;
        while (true) {
            int read = input.Read(
                buffer,
                0,
                buffer.Length);
            if (read == 0) {
                break;
            }
            if (read >
                maximumEntryBytes -
                entryBytes) {
                throw new InvalidDataException(
                    $"Provider package entry '{entryName}' exceeds {maximumEntryBytes} expanded bytes.");
            }
            if (read >
                maximumPackageBytes -
                expandedBytes) {
                throw new InvalidDataException(
                    $"Provider package expanded contents exceed {maximumPackageBytes} bytes.");
            }
            entryBytes += read;
            expandedBytes += read;
            output.Write(
                buffer,
                0,
                read);
        }
        return output.ToArray();
    }

    private sealed class PackageContents {
        internal PackageContents(
            EventProviderPackageManifest manifest,
            IReadOnlyDictionary<string, byte[]> files) {

            Manifest = manifest;
            Files = files;
        }

        internal EventProviderPackageManifest Manifest { get; }
        internal IReadOnlyDictionary<string, byte[]> Files { get; }
    }
}

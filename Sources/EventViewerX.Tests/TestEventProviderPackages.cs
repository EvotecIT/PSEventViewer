using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using EventViewerX.Providers;
using Xunit;
using ProviderEventDefinition =
    EventViewerX.Providers.EventProviderEventDefinition;

namespace EventViewerX.Tests;

public sealed class TestEventProviderPackages {
    [Fact]
    public void GeneratesNamedMessagesAndStableFields() {
        EventProviderDefinition definition = CreateDefinition();

        string manifest = EventProviderManifestGenerator.Generate(
            definition,
            "provider.resources.dll");

        Assert.Contains("name=\"ComputerName\"", manifest);
        Assert.Contains("name=\"FindingCount\"", manifest);
        Assert.Contains(
            "Scan of %1 found %2 issues.",
            manifest);
    }

    [Fact]
    public void RejectsLiteralPercentSequencesBeforeResourceCompilation() {
        EventProviderDefinition definition = CreateDefinition();

        FormatException exception =
            Assert.Throws<FormatException>(() =>
                EventProviderMessageTemplateCompiler.Compile(
                    "Literal %1 and %0; field {ComputerName}; 100%.",
                    definition.Events[0].Fields));

        Assert.Contains(
            "payload field",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeneratedManifestSymbolsContainOnlyAsciiIdentifiers() {
        Assert.Equal(
            "M_NCHEN_1",
            EventProviderManifestNames.Symbol(
                "München-1",
                "Fallback"));
    }

    [Fact]
    public void RejectsSchemaChangesWithoutAnEventVersionBump() {
        EventProviderDefinition baseline = CreateDefinition();
        EventProviderDefinition candidate = CreateDefinition();
        candidate.Events[0].Fields[1].Type =
            EventProviderFieldType.UInt64;

        EventProviderCompatibilityResult result =
            EventProviderCompatibility.Compare(
                baseline,
                candidate);

        Assert.False(result.IsCompatible);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "EventFieldTypeChanged");
    }

    [Fact]
    public void RejectsCaseOnlyFieldNameChangesWithoutAnEventVersionBump() {
        EventProviderDefinition baseline = CreateDefinition();
        EventProviderDefinition candidate = CreateDefinition();
        candidate.Events[0].Fields[0].Name =
            "computerName";

        EventProviderCompatibilityResult result =
            EventProviderCompatibility.Compare(
                baseline,
                candidate);

        Assert.False(result.IsCompatible);
        Assert.Contains(
            result.Issues,
            issue => issue.Code ==
                     "EventFieldNameChanged");
    }

    [Fact]
    public void InfersTypedPayloadsAndRequiresExplicitArrayDimensions() {
        IReadOnlyList<EventProviderFieldDefinition> fields =
            EventProviderTypedPayload.Describe<ScanCompletedPayload>();

        Assert.Collection(
            fields,
            field => {
                Assert.Equal("ComputerName", field.Name);
                Assert.Equal(
                    EventProviderFieldType.UnicodeString,
                    field.Type);
            },
            field => {
                Assert.Equal("FindingCount", field.Name);
                Assert.Equal(
                    EventProviderFieldType.UInt32,
                    field.Type);
            });
        Assert.Throws<ArgumentException>(() =>
            EventProviderTypedPayload.Describe<InvalidArrayPayload>());
    }

    [Fact]
    public void BuildsOpensAndVerifiesSignedProviderPackage() {
        string root = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string packagePath = Path.Combine(
            root,
            "smoke.evxprovider");
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=EventViewerX Package Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate =
            request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddDays(1));
        try {
            EventProviderDefinition definition =
                CreateDefinition();
            EventProviderPackageBuildResult result =
                EventProviderPackageBuilder.Build(
                    definition,
                    packagePath,
                    new EventProviderPackageBuildOptions {
                        SigningCertificate = certificate
                    });
            using EventProviderPackage package =
                EventProviderPackageReader.Open(packagePath);

            Assert.True(result.IsSigned);
            Assert.Equal(
                certificate.Thumbprint,
                result.SignerThumbprint);
            Assert.True(package.IsSigned);
            Assert.Equal(
                certificate.Thumbprint,
                package.SignerCertificate?.Thumbprint);
            Assert.Equal(
                "Evotec-EventViewerX-PackageTest",
                package.Definition.Name);
            Assert.Equal(4, package.Manifest.Files.Count);

            EventProviderPackageTrust.EnsureAllowed(
                package,
                new EventProviderPackageInstallOptions {
                    TrustMode = EventProviderPackageTrustMode
                        .RequireTrustedSignature,
                    TrustedSignerThumbprints =
                        new[] { certificate.Thumbprint }
                });
            InvalidDataException wrongSigner =
                Assert.Throws<InvalidDataException>(() =>
                    EventProviderPackageTrust.EnsureAllowed(
                        package,
                        new EventProviderPackageInstallOptions {
                            TrustMode = EventProviderPackageTrustMode
                                .RequireTrustedSignature,
                            TrustedSignerThumbprints =
                                new[] {
                                    "0000000000000000000000000000000000000000"
                                }
                        }));
            Assert.Contains(
                "allowlist",
                wrongSigner.Message,
                StringComparison.OrdinalIgnoreCase);
            InvalidDataException missingCodeSigning =
                Assert.Throws<InvalidDataException>(() =>
                    EventProviderPackageTrust.EnsureAllowed(
                        package,
                        new EventProviderPackageInstallOptions {
                            TrustMode = EventProviderPackageTrustMode
                                .RequireTrustedSignature
                        }));
            Assert.Contains(
                "code signing",
                missingCodeSigning.Message,
                StringComparison.OrdinalIgnoreCase);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EventProviderPackageTrust.EnsureAllowed(
                    package,
                    new EventProviderPackageInstallOptions {
                        TrustMode =
                            (EventProviderPackageTrustMode)int.MaxValue
                    }));

            string extractedPath = Path.Combine(
                root,
                "extracted");
            using (EventProviderPackage extracted =
                   EventProviderPackageReader.Extract(
                       packagePath,
                       extractedPath)) {
            }
            File.WriteAllText(
                Path.Combine(
                    extractedPath,
                    "provider.definition.json"),
                "{}",
                new UTF8Encoding(false));
            Assert.Throws<InvalidDataException>(() =>
                EventProviderPackageReader
                    .EnsureExtractedFilesMatch(
                        packagePath,
                        extractedPath));
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(
        "{\"name\":null,\"id\":\"520ecea3-f786-459c-8b02-2a288cbef31c\",\"packageVersion\":\"1.0.0\"}",
        "ProviderNameRequired",
        "Name")]
    [InlineData(
        "{\"name\":\"Provider\",\"id\":\"520ecea3-f786-459c-8b02-2a288cbef31c\",\"packageVersion\":null}",
        "PackageVersionRequired",
        "PackageVersion")]
    [InlineData(
        "{\"name\":\"Provider\",\"id\":\"520ecea3-f786-459c-8b02-2a288cbef31c\",\"packageVersion\":\"1.0.0\",\"channels\":null}",
        "DefinitionMemberNull",
        "Channels")]
    [InlineData(
        "{\"name\":\"Provider\",\"id\":\"520ecea3-f786-459c-8b02-2a288cbef31c\",\"packageVersion\":\"1.0.0\",\"events\":[null]}",
        "DefinitionMemberNull",
        "Events[0]")]
    public void NullJsonMembersProduceStructuredValidation(
        string json,
        string code,
        string path) {

        EventProviderValidationException exception =
            Assert.Throws<EventProviderValidationException>(() =>
                EventProviderDefinitionJson.Parse(json));

        Assert.Contains(
            exception.Result.Issues,
            issue => issue.Code == code &&
                     issue.Path == path);
    }

    [Fact]
    public void RejectsUnknownDefinitionPropertiesAtEverySchemaLevel() {
        string json = EventProviderDefinitionJson.Serialize(
            CreateDefinition());

        Assert.Throws<JsonException>(() =>
            EventProviderDefinitionJson.Parse(
                json.Replace(
                    "{",
                    "{\"packageVerzion\":\"1.0.0\",",
                    StringComparison.Ordinal)));
        Assert.Throws<JsonException>(() =>
            EventProviderDefinitionJson.Parse(
                json.Replace(
                    "\"fields\": [",
                    "\"fieldz\": true, \"fields\": [",
                    StringComparison.Ordinal)));
    }

    [Fact]
    public void RejectsNamesThatCollideAfterManifestNormalization() {
        EventProviderDefinition definition = CreateDefinition();
        definition.AddChannel(
            EventProviderChannelDefinition.Operational(
                "A-B",
                definition.Name + "/A-B"));
        definition.AddChannel(
            EventProviderChannelDefinition.Operational(
                "A_B",
                definition.Name + "/A_B"));

        EventProviderValidationResult result =
            EventProviderDefinitionValidator.Validate(
                definition);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code == "GeneratedSymbolCollision");
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "GeneratedLocalizationIdCollision");
    }

    [Fact]
    public void VersionQualifiesSymbolsForOneEventNameAcrossVersions() {
        EventProviderDefinition definition = CreateDefinition();
        ProviderEventDefinition versionOne =
            ProviderEventDefinition.FromType<ScanCompletedPayload>(
                "ScanCompleted",
                1000,
                "Operational",
                version: 1);
        versionOne.Messages["en-US"] =
            "Version one scan of {ComputerName} found {FindingCount} issues.";
        definition.AddEvent(versionOne);

        EventProviderValidationResult validation =
            EventProviderDefinitionValidator.Validate(
                definition);
        string manifest =
            EventProviderManifestGenerator.Generate(
                definition,
                "provider.resources.dll");

        Assert.True(validation.IsValid);
        Assert.Contains(
            "symbol=\"SCANCOMPLETED_V0\"",
            manifest);
        Assert.Contains(
            "symbol=\"SCANCOMPLETED_V1\"",
            manifest);
    }

    [Fact]
    public void RefusesToClaimANonDedicatedProviderRoot() {
        string root = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(root, "unrelated.txt"),
            "do not modify");
        try {
            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    EventProviderManagedDirectorySecurity
                        .EnsureManagedRoot(
                            root,
                            TimeSpan.FromSeconds(10)));

            Assert.Contains(
                "unrelated content",
                exception.Message,
                StringComparison.OrdinalIgnoreCase);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SerializesLifecycleOperationsByProviderIdentity() {
        Guid providerId = Guid.NewGuid();
        using EventProviderLifecycleLock first =
            EventProviderLifecycleLock.Acquire(
                providerId,
                TimeSpan.FromSeconds(1));

        Exception? failure = null;
        var contender = new Thread(() => {
            try {
                using EventProviderLifecycleLock second =
                    EventProviderLifecycleLock.Acquire(
                        providerId,
                        TimeSpan.FromMilliseconds(100));
            } catch (Exception exception) {
                failure = exception;
            }
        });
        contender.Start();
        Assert.True(contender.Join(TimeSpan.FromSeconds(2)));
        Assert.IsType<TimeoutException>(failure);
    }

    [Fact]
    public void ProducesIdenticalSignedPackagesForIdenticalInputs() {
        string root = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=EventViewerX Reproducibility Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate =
            request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddDays(1));
        try {
            EventProviderPackageBuildResult first =
                EventProviderPackageBuilder.Build(
                    CreateDefinition(),
                    Path.Combine(root, "first.evxprovider"),
                    new EventProviderPackageBuildOptions {
                        SigningCertificate = certificate
                    });
            EventProviderPackageBuildResult second =
                EventProviderPackageBuilder.Build(
                    CreateDefinition(),
                    Path.Combine(root, "second.evxprovider"),
                    new EventProviderPackageBuildOptions {
                        SigningCertificate = certificate
                    });

            Assert.Equal(
                first.PackageSha256,
                second.PackageSha256);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RejectsAFileWhoseSignedHashWasTampered() {
        string root = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string packagePath = Path.Combine(
            root,
            "tampered.evxprovider");
        try {
            EventProviderPackageBuilder.Build(
                CreateDefinition(),
                packagePath);
            using (ZipArchive archive = ZipFile.Open(
                       packagePath,
                       ZipArchiveMode.Update)) {
                ZipArchiveEntry definition =
                    archive.GetEntry("provider.definition.json")!;
                using Stream stream = definition.Open();
                stream.SetLength(0);
                using var writer = new StreamWriter(
                    stream,
                    new UTF8Encoding(false),
                    1024,
                    leaveOpen: true);
                writer.Write("{}");
            }

            Assert.Throws<InvalidDataException>(() =>
                EventProviderPackageReader.Open(packagePath));
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    private static EventProviderDefinition CreateDefinition() {
        EventProviderDefinition definition =
            EventProviderDefinition.Create(
                "Evotec-EventViewerX-PackageTest",
                Guid.Parse("520ecea3-f786-459c-8b02-2a288cbef31c"),
                "1.0.0");
        definition.DisplayNames["en-US"] =
            "EventViewerX Package Test";
        definition.AddChannel(
            EventProviderChannelDefinition.Operational(
                "Operational",
                "Evotec-EventViewerX-PackageTest/Operational"));
        ProviderEventDefinition eventDefinition =
            ProviderEventDefinition.FromType<ScanCompletedPayload>(
                "ScanCompleted",
                1000,
                "Operational");
        eventDefinition.Messages["en-US"] =
            "Scan of {ComputerName} found {FindingCount} issues.";
        definition.AddEvent(eventDefinition);
        return definition;
    }

    private sealed class ScanCompletedPayload {
        [EventProviderPayloadField(0)]
        public string ComputerName { get; set; } = string.Empty;

        [EventProviderPayloadField(1)]
        public uint FindingCount { get; set; }
    }

    private sealed class InvalidArrayPayload {
        [EventProviderPayloadField(0)]
        public uint[] Values { get; set; } = Array.Empty<uint>();
    }
}

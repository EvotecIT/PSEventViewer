using EventViewerX.Providers;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventProviderPackageInventory {
    [Fact]
    public void CorruptRetainedPackageDoesNotHideHealthyActivePackage() {
        string root = Path.Combine(
            Path.GetTempPath(),
            "EventViewerX.Tests",
            Guid.NewGuid().ToString("N"));
        EventProviderDefinition definition =
            TestEventProviderPackages.CreateDefinition();
        string providerRoot = Path.Combine(
            root,
            definition.Id.ToString("N"));
        string activeDirectory = Path.Combine(
            providerRoot,
            "active");
        string retainedDirectory = Path.Combine(
            providerRoot,
            "retained-corrupt");
        Directory.CreateDirectory(activeDirectory);
        Directory.CreateDirectory(retainedDirectory);
        string activePackage = Path.Combine(
            activeDirectory,
            EventProviderInstallationStore
                .ArchivedPackageFileName);
        try {
            EventProviderPackageBuildResult build =
                EventProviderPackageBuilder.Build(
                    definition,
                    activePackage);
            File.WriteAllText(
                Path.Combine(
                    retainedDirectory,
                    EventProviderInstallationStore
                        .ArchivedPackageFileName),
                "not a provider package");
            EventProviderInstallationStore.Save(
                providerRoot,
                new EventProviderInstallationState {
                    ProviderName = definition.Name,
                    ProviderId = definition.Id,
                    ActiveVersion =
                        definition.PackageVersion,
                    ActiveDirectoryName =
                        Path.GetFileName(activeDirectory),
                    PackageSha256 =
                        build.PackageSha256,
                    InstalledAtUtc =
                        DateTimeOffset.UtcNow
                });

            InstalledEventProviderPackage installed =
                Assert.Single(
                    EventProviderPackageManager
                        .GetInstalled(root));
            EventProviderDefinition activeDefinition =
                EventProviderPackageManager
                    .GetDefinition(
                        definition.Name,
                        root);

            Assert.True(installed.IsActive);
            Assert.Equal(
                definition.Name,
                installed.ProviderName);
            Assert.Equal(
                definition.Id,
                activeDefinition.Id);
        } finally {
            if (Directory.Exists(root)) {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }
}

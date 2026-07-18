using System;
using System.IO;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventCheckpointStore {
    [Fact]
    public void LegacyNumericCheckpointIsLoadedAndAdvancedInANestedDirectory() {
        string root = CreateTemporaryDirectory();
        try {
            string path = Path.Combine(root, "nested", "checkpoint.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{\"System\":100}");

            EventCheckpointSnapshot initial = EventCheckpointStore.Load(path);
            Assert.Equal(100, initial.Records["System"]);
            Assert.True(initial.TryGetValue("System", out EventCheckpointValue? value));
            Assert.Equal(Guid.Empty, value!.GenerationId);

            EventCheckpointSnapshot updated = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 101, value.GenerationId, boundaryIdentity: "boundary-101") });

            Assert.Equal(101, updated.Records["System"]);
            Assert.Equal("boundary-101", updated.Checkpoints["System"].BoundaryIdentity);
            Assert.True(File.Exists(path + ".state.json"));
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BoundaryIdentityCanBeMigratedWithoutAdvancingTheRecord() {
        string root = CreateTemporaryDirectory();
        try {
            string path = Path.Combine(root, "checkpoint.json");
            EventCheckpointSnapshot initial = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 100, Guid.Empty) });

            EventCheckpointSnapshot migrated = EventCheckpointStore.Update(
                path,
                new[] {
                    new EventCheckpointUpdate(
                        "System",
                        100,
                        initial.Checkpoints["System"].GenerationId,
                        boundaryIdentity: "boundary-100")
                });

            Assert.Equal(100, migrated.Records["System"]);
            Assert.Equal("boundary-100", migrated.Checkpoints["System"].BoundaryIdentity);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResetGenerationRejectsAWriterThatStartedBeforeTheReset() {
        string root = CreateTemporaryDirectory();
        try {
            string path = Path.Combine(root, "checkpoint.json");
            EventCheckpointSnapshot initial = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 1000, Guid.Empty) });
            Guid oldGeneration = initial.Checkpoints["System"].GenerationId;

            EventCheckpointSnapshot reset = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 5, oldGeneration, startsNewGeneration: true) });
            Guid newGeneration = reset.Checkpoints["System"].GenerationId;
            Assert.NotEqual(oldGeneration, newGeneration);

            EventCheckpointSnapshot grownGeneration = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 1005, newGeneration) });
            Assert.Equal(1005, grownGeneration.Records["System"]);

            EventCheckpointSnapshot staleResult = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 1010, oldGeneration) });
            Assert.Equal(1005, staleResult.Records["System"]);
            Assert.Equal(newGeneration, staleResult.Checkpoints["System"].GenerationId);

            EventCheckpointSnapshot currentResult = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 1006, newGeneration) });
            Assert.Equal(1006, currentResult.Records["System"]);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResetWithoutRecordsPersistsATombstoneThatRejectsStaleProgress() {
        string root = CreateTemporaryDirectory();
        try {
            string path = Path.Combine(root, "checkpoint.json");
            EventCheckpointSnapshot initial = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 1000, Guid.Empty) });
            Guid oldGeneration = initial.Checkpoints["System"].GenerationId;

            EventCheckpointSnapshot reset = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", null, oldGeneration, startsNewGeneration: true) });
            Assert.DoesNotContain("System", reset.Records.Keys);
            Assert.NotEqual(oldGeneration, reset.Checkpoints["System"].GenerationId);

            EventCheckpointSnapshot staleResult = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 1001, oldGeneration) });
            Assert.DoesNotContain("System", staleResult.Records.Keys);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CorruptCheckpointFailsInsteadOfSilentlyDiscardingProgress() {
        string root = CreateTemporaryDirectory();
        try {
            string path = Path.Combine(root, "checkpoint.json");
            File.WriteAllText(path, "not-json");

            Assert.Throws<InvalidDataException>(() => EventCheckpointStore.Load(path));
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void NumericCompatibilityMirrorFailureDoesNotInvalidateAuthoritativeUpdate() {
        string root = CreateTemporaryDirectory();
        try {
            string path = Path.Combine(root, "checkpoint.json");
            EventCheckpointSnapshot initial = EventCheckpointStore.Update(
                path,
                new[] { new EventCheckpointUpdate("System", 100, Guid.Empty) });

            File.Delete(path);
            Directory.CreateDirectory(path);

            EventCheckpointSnapshot updated = EventCheckpointStore.Update(
                path,
                new[] {
                    new EventCheckpointUpdate(
                        "System",
                        101,
                        initial.Checkpoints["System"].GenerationId)
                });

            Assert.Equal(101, updated.Records["System"]);
            Assert.Equal(101, EventCheckpointStore.Load(path).Records["System"]);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory() {
        string path = Path.Combine(Path.GetTempPath(), "EventViewerX.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

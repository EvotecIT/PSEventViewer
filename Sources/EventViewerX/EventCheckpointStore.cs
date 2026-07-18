using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace EventViewerX;

/// <summary>Represents one persisted event-log checkpoint and its log-generation identity.</summary>
public sealed class EventCheckpointValue {
    internal EventCheckpointValue(long? recordId, Guid generationId) {
        RecordId = recordId;
        GenerationId = generationId;
    }

    /// <summary>Last contiguously processed record identifier, or <c>null</c> before the first record in a generation.</summary>
    public long? RecordId { get; }

    /// <summary>Identity used to reject stale writers after a log is cleared or replaced.</summary>
    public Guid GenerationId { get; }
}

/// <summary>Immutable snapshot of checkpoints loaded from a checkpoint file.</summary>
public sealed class EventCheckpointSnapshot {
    private readonly Dictionary<string, EventCheckpointValue> _checkpoints;
    private readonly Dictionary<string, long> _records;
    private readonly ReadOnlyDictionary<string, EventCheckpointValue> _checkpointView;
    private readonly ReadOnlyDictionary<string, long> _recordView;

    internal EventCheckpointSnapshot(Dictionary<string, EventCheckpointValue> checkpoints) {
        _checkpoints = new Dictionary<string, EventCheckpointValue>(checkpoints, StringComparer.OrdinalIgnoreCase);
        _records = checkpoints
            .Where(static entry => entry.Value.RecordId.HasValue)
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value.RecordId!.Value,
                StringComparer.OrdinalIgnoreCase);
        _checkpointView = new ReadOnlyDictionary<string, EventCheckpointValue>(_checkpoints);
        _recordView = new ReadOnlyDictionary<string, long>(_records);
    }

    /// <summary>Numeric checkpoint values compatible with the primary JSON checkpoint file.</summary>
    public IReadOnlyDictionary<string, long> Records => _recordView;

    /// <summary>All generation-aware checkpoints, including generations that have not emitted a record yet.</summary>
    public IReadOnlyDictionary<string, EventCheckpointValue> Checkpoints => _checkpointView;

    /// <summary>Gets a checkpoint, including its generation identity.</summary>
    public bool TryGetValue(string key, out EventCheckpointValue? value) {
        if (string.IsNullOrWhiteSpace(key)) {
            value = null;
            return false;
        }

        return _checkpoints.TryGetValue(key, out value);
    }

    internal Dictionary<string, EventCheckpointValue> CopyValues()
        => new(_checkpoints, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Describes a compare-and-advance checkpoint update.</summary>
public sealed class EventCheckpointUpdate {
    /// <summary>Creates a checkpoint update.</summary>
    /// <param name="key">Checkpoint key.</param>
    /// <param name="recordId">Last contiguously processed record, or <c>null</c> to start a generation without progress.</param>
    /// <param name="expectedGenerationId">Generation observed when the query began.</param>
    /// <param name="startsNewGeneration">Whether the source was detected as cleared or replaced.</param>
    public EventCheckpointUpdate(string key, long? recordId, Guid expectedGenerationId, bool startsNewGeneration = false) {
        if (string.IsNullOrWhiteSpace(key)) {
            throw new ArgumentException("Checkpoint key cannot be null or empty.", nameof(key));
        }
        if (recordId < 0) {
            throw new ArgumentOutOfRangeException(nameof(recordId), "Checkpoint record ID must be greater than or equal to zero.");
        }

        Key = key;
        RecordId = recordId;
        ExpectedGenerationId = expectedGenerationId;
        StartsNewGeneration = startsNewGeneration;
    }

    /// <summary>Checkpoint key.</summary>
    public string Key { get; }

    /// <summary>Last contiguously processed record, if any.</summary>
    public long? RecordId { get; }

    /// <summary>Generation observed when the query began.</summary>
    public Guid ExpectedGenerationId { get; }

    /// <summary>Whether this update starts a new log generation.</summary>
    public bool StartsNewGeneration { get; }
}

/// <summary>
/// Loads and atomically advances event checkpoints across processes and log generations.
/// </summary>
/// <remarks>
/// The primary file remains a dictionary of numeric record IDs. A generation-aware sidecar is authoritative for
/// current clients and prevents a query that started before a log clear from restoring a stale high-water mark.
/// </remarks>
public static class EventCheckpointStore {
    private const int StateVersion = 1;
    private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Loads a checkpoint snapshot.</summary>
    public static EventCheckpointSnapshot Load(string path) {
        string checkpointPath = NormalizePath(path);
        EnsureParentDirectory(checkpointPath);
        using (AcquireFileLock(checkpointPath, DefaultLockTimeout)) {
            return LoadUnlocked(checkpointPath);
        }
    }

    /// <summary>
    /// Applies generation-checked updates under a cross-process file lock and returns the persisted snapshot.
    /// </summary>
    public static EventCheckpointSnapshot Update(
        string path,
        IEnumerable<EventCheckpointUpdate> updates,
        TimeSpan? lockTimeout = null) {

        if (updates == null) {
            throw new ArgumentNullException(nameof(updates));
        }

        string checkpointPath = NormalizePath(path);
        EnsureParentDirectory(checkpointPath);

        using (AcquireFileLock(checkpointPath, lockTimeout ?? DefaultLockTimeout)) {
            EventCheckpointSnapshot latest = LoadUnlocked(checkpointPath);
            Dictionary<string, EventCheckpointValue> values = latest.CopyValues();

            foreach (EventCheckpointUpdate update in updates) {
                Guid currentGeneration = values.TryGetValue(update.Key, out EventCheckpointValue? current)
                    ? current.GenerationId
                    : Guid.Empty;
                if (currentGeneration != update.ExpectedGenerationId) {
                    continue;
                }

                if (update.StartsNewGeneration) {
                    values[update.Key] = new EventCheckpointValue(update.RecordId, Guid.NewGuid());
                    continue;
                }

                if (!update.RecordId.HasValue) {
                    continue;
                }

                if (current?.RecordId == null || update.RecordId.Value > current.RecordId.Value) {
                    values[update.Key] = new EventCheckpointValue(update.RecordId, currentGeneration);
                }
            }

            WriteState(checkpointPath, values);
            return new EventCheckpointSnapshot(values);
        }
    }

    private static EventCheckpointSnapshot LoadUnlocked(string checkpointPath) {
        Dictionary<string, EventCheckpointValue> values = ReadNumericFile(checkpointPath)
            .ToDictionary(
                static entry => entry.Key,
                static entry => new EventCheckpointValue(entry.Value, Guid.Empty),
                StringComparer.OrdinalIgnoreCase);

        string statePath = GetStatePath(checkpointPath);
        if (!File.Exists(statePath)) {
            return new EventCheckpointSnapshot(values);
        }

        CheckpointStateDocument? state;
        try {
            state = JsonSerializer.Deserialize<CheckpointStateDocument>(File.ReadAllText(statePath));
        } catch (JsonException ex) {
            throw new InvalidDataException($"Checkpoint generation state '{statePath}' is not valid JSON.", ex);
        }

        if (state == null || state.Version != StateVersion || state.Checkpoints == null) {
            throw new InvalidDataException($"Checkpoint generation state '{statePath}' has an unsupported format.");
        }

        foreach (KeyValuePair<string, CheckpointStateEntry> entry in state.Checkpoints) {
            if (entry.Value == null || entry.Value.RecordId < 0) {
                throw new InvalidDataException($"Checkpoint generation state '{statePath}' contains an invalid entry for '{entry.Key}'.");
            }
            values[entry.Key] = new EventCheckpointValue(entry.Value.RecordId, entry.Value.GenerationId);
        }

        return new EventCheckpointSnapshot(values);
    }

    private static Dictionary<string, long> ReadNumericFile(string checkpointPath) {
        if (!File.Exists(checkpointPath)) {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }

        Dictionary<string, long>? records;
        try {
            records = JsonSerializer.Deserialize<Dictionary<string, long>>(File.ReadAllText(checkpointPath));
        } catch (JsonException ex) {
            throw new InvalidDataException($"Checkpoint file '{checkpointPath}' is not valid JSON.", ex);
        }

        if (records == null) {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }
        if (records.Any(static entry => entry.Value < 0)) {
            throw new InvalidDataException($"Checkpoint file '{checkpointPath}' contains a negative record ID.");
        }
        return new Dictionary<string, long>(records, StringComparer.OrdinalIgnoreCase);
    }

    private static void WriteState(string checkpointPath, Dictionary<string, EventCheckpointValue> values) {
        var document = new CheckpointStateDocument {
            Version = StateVersion,
            Checkpoints = values.ToDictionary(
                static entry => entry.Key,
                static entry => new CheckpointStateEntry {
                    RecordId = entry.Value.RecordId,
                    GenerationId = entry.Value.GenerationId
                },
                StringComparer.OrdinalIgnoreCase)
        };
        var numeric = values
            .Where(static entry => entry.Value.RecordId.HasValue)
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value.RecordId!.Value,
                StringComparer.OrdinalIgnoreCase);

        AtomicWrite(GetStatePath(checkpointPath), JsonSerializer.Serialize(document));
        AtomicWrite(checkpointPath, JsonSerializer.Serialize(numeric));
    }

    private static FileStream AcquireFileLock(string checkpointPath, TimeSpan timeout) {
        if (timeout < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Lock timeout must be greater than or equal to zero.");
        }

        string lockPath = checkpointPath + ".lock";
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (true) {
            try {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            } catch (IOException ex) {
                if (DateTime.UtcNow >= deadline) {
                    throw new TimeoutException($"Timed out waiting to update shared event checkpoint file '{checkpointPath}'.", ex);
                }
                Thread.Sleep(50);
            }
        }
    }

    private static void AtomicWrite(string path, string contents) {
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllText(temporaryPath, contents);
            if (File.Exists(path)) {
                File.Replace(temporaryPath, path, null);
            } else {
                File.Move(temporaryPath, path);
            }
        } finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string NormalizePath(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Checkpoint path cannot be null or empty.", nameof(path));
        }
        return Path.GetFullPath(path);
    }

    private static void EnsureParentDirectory(string checkpointPath) {
        string? directory = Path.GetDirectoryName(checkpointPath);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetStatePath(string checkpointPath)
        => checkpointPath + ".state.json";

    private sealed class CheckpointStateDocument {
        public int Version { get; set; }
        public Dictionary<string, CheckpointStateEntry>? Checkpoints { get; set; }
    }

    private sealed class CheckpointStateEntry {
        public long? RecordId { get; set; }
        public Guid GenerationId { get; set; }
    }
}

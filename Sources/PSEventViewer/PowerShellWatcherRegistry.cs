using EventViewerX;
using System;
using System.Collections.Concurrent;

namespace PSEventViewer;

/// <summary>Tracks only watchers created by the PowerShell module so module removal does not affect other hosts.</summary>
internal static class PowerShellWatcherRegistry {
    internal const string OwnerVariableName = "PSEventViewer_WatcherOwnerId";
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, byte>> OwnedWatcherIds = new();

    internal static void Register(Guid ownerId, Guid watcherId) {
        ConcurrentDictionary<Guid, byte> ownerWatchers = OwnedWatcherIds.GetOrAdd(
            ownerId,
            static _ => new ConcurrentDictionary<Guid, byte>());
        ownerWatchers.TryAdd(watcherId, 0);
    }

    internal static void Unregister(Guid ownerId, Guid watcherId) {
        if (!OwnedWatcherIds.TryGetValue(ownerId, out ConcurrentDictionary<Guid, byte>? ownerWatchers)) {
            return;
        }

        ownerWatchers.TryRemove(watcherId, out _);
    }

    internal static void StopAllOwned(Guid ownerId) {
        if (!OwnedWatcherIds.TryRemove(ownerId, out ConcurrentDictionary<Guid, byte>? ownerWatchers)) {
            return;
        }

        foreach (Guid watcherId in ownerWatchers.Keys) {
            WatcherManager.StopWatcher(watcherId);
        }
    }
}

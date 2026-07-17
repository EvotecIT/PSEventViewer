using EventViewerX;
using System;
using System.Collections.Concurrent;

namespace PSEventViewer;

/// <summary>Tracks only watchers created by the PowerShell module so module removal does not affect other hosts.</summary>
internal static class PowerShellWatcherRegistry {
    private static readonly ConcurrentDictionary<Guid, byte> OwnedWatcherIds = new();

    internal static void Register(Guid watcherId) => OwnedWatcherIds.TryAdd(watcherId, 0);

    internal static void Unregister(Guid watcherId) => OwnedWatcherIds.TryRemove(watcherId, out _);

    internal static void StopAllOwned() {
        foreach (Guid watcherId in OwnedWatcherIds.Keys) {
            WatcherManager.StopWatcher(watcherId);
            OwnedWatcherIds.TryRemove(watcherId, out _);
        }
    }
}

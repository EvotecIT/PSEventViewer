using EventViewerX;
using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PSEventViewer;

/// <summary>Tracks only watchers created by the PowerShell module so module removal does not affect other hosts.</summary>
internal static class PowerShellWatcherRegistry {
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, byte>> OwnedWatcherIds = new();
    private static readonly ConditionalWeakTable<PSModuleInfo, OwnerRegistration> ModuleOwners = new();
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, long>> RunspaceOwners = new();
    private static readonly ConcurrentDictionary<string, Guid> StableModuleOwners = new(StringComparer.OrdinalIgnoreCase);
    private static long _ownerSequence;

    internal static Guid BeginModuleInstance(Guid runspaceId) {
        Guid ownerId = Guid.NewGuid();
        ConcurrentDictionary<Guid, long> owners = RunspaceOwners.GetOrAdd(
            runspaceId,
            static _ => new ConcurrentDictionary<Guid, long>());
        owners[ownerId] = Interlocked.Increment(ref _ownerSequence);
        return ownerId;
    }

    internal static Guid GetOwnerId(PSModuleInfo? module, Guid standaloneOwnerId) {
        if (module != null && ModuleOwners.TryGetValue(module, out OwnerRegistration? registeredOwner)) {
            if (IsActiveOwner(standaloneOwnerId, registeredOwner.OwnerId)) {
                return registeredOwner.OwnerId;
            }
            ModuleOwners.Remove(module);
        }

        string? moduleKey = GetStableModuleKey(module, standaloneOwnerId);
        if (moduleKey != null && StableModuleOwners.TryGetValue(moduleKey, out Guid stableOwnerId)) {
            if (IsActiveOwner(standaloneOwnerId, stableOwnerId)) {
                return stableOwnerId;
            }
            StableModuleOwners.TryRemove(moduleKey, out _);
        }

        Guid ownerId = GetNewestRunspaceOwner(standaloneOwnerId);
        if (moduleKey != null) {
            ownerId = StableModuleOwners.GetOrAdd(moduleKey, ownerId);
        }
        if (module != null) {
            ownerId = ModuleOwners.GetValue(module, _ => new OwnerRegistration(ownerId)).OwnerId;
        }
        return ownerId;
    }

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

    internal static void EndModuleInstance(Guid runspaceId, PSModuleInfo? module) {
        string? moduleKey = GetStableModuleKey(module, runspaceId);
        Guid ownerId;
        if (module != null && ModuleOwners.TryGetValue(module, out OwnerRegistration? registeredOwner)) {
            ownerId = registeredOwner.OwnerId;
        } else if (moduleKey != null && StableModuleOwners.TryGetValue(moduleKey, out Guid stableOwnerId)) {
            ownerId = stableOwnerId;
        } else {
            ownerId = GetNewestRunspaceOwner(runspaceId);
        }
        StopAllOwned(ownerId);
        if (module != null) {
            ModuleOwners.Remove(module);
        }
        if (moduleKey != null) {
            StableModuleOwners.TryRemove(moduleKey, out _);
        }
        foreach (KeyValuePair<string, Guid> stableOwner in StableModuleOwners) {
            if (stableOwner.Value == ownerId) {
                StableModuleOwners.TryRemove(stableOwner.Key, out _);
            }
        }
        if (!RunspaceOwners.TryGetValue(runspaceId, out ConcurrentDictionary<Guid, long>? owners)) {
            return;
        }

        owners.TryRemove(ownerId, out _);
        if (owners.IsEmpty) {
            RunspaceOwners.TryRemove(runspaceId, out _);
        }
    }

    private static Guid GetNewestRunspaceOwner(Guid runspaceId) {
        if (!RunspaceOwners.TryGetValue(runspaceId, out ConcurrentDictionary<Guid, long>? owners)) {
            return runspaceId;
        }

        Guid newestOwner = runspaceId;
        long newestSequence = long.MinValue;
        foreach (KeyValuePair<Guid, long> owner in owners) {
            if (owner.Value > newestSequence) {
                newestOwner = owner.Key;
                newestSequence = owner.Value;
            }
        }
        return newestOwner;
    }

    private static bool IsActiveOwner(Guid runspaceId, Guid ownerId)
        => RunspaceOwners.TryGetValue(runspaceId, out ConcurrentDictionary<Guid, long>? owners) &&
           owners.ContainsKey(ownerId);

    private static string? GetStableModuleKey(PSModuleInfo? module, Guid runspaceId) {
        if (module == null) {
            return null;
        }

        string identity = string.IsNullOrWhiteSpace(module.Path) ? module.Name : module.Path;
        return $"{runspaceId:N}|{identity}|{module.Prefix}";
    }

    private sealed class OwnerRegistration {
        internal OwnerRegistration(Guid ownerId) {
            OwnerId = ownerId;
        }

        internal Guid OwnerId { get; }
    }
}

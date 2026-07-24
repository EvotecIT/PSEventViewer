namespace EventViewerX.Providers;

internal sealed class EventProviderLifecycleLock : IDisposable {
    private readonly Mutex _mutex;
    private bool _ownsMutex;

    private EventProviderLifecycleLock(
        Mutex mutex,
        bool ownsMutex) {

        _mutex = mutex;
        _ownsMutex = ownsMutex;
    }

    internal static EventProviderLifecycleLock Acquire(
        Guid providerId,
        TimeSpan timeout) {

        if (providerId == Guid.Empty) {
            throw new ArgumentException(
                "A provider GUID is required for lifecycle serialization.",
                nameof(providerId));
        }
        if (timeout <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "Lifecycle lock timeout must be positive.");
        }

        var mutex = new Mutex(
            initiallyOwned: false,
            "Global\\EventViewerX.Provider." +
            providerId.ToString("N"));
        bool acquired;
        try {
            acquired = mutex.WaitOne(timeout);
        } catch (AbandonedMutexException) {
            acquired = true;
        }
        if (!acquired) {
            mutex.Dispose();
            throw new TimeoutException(
                $"Timed out waiting for another lifecycle operation on provider {providerId:D}.");
        }
        return new EventProviderLifecycleLock(
            mutex,
            ownsMutex: true);
    }

    public void Dispose() {
        try {
            if (_ownsMutex) {
                _ownsMutex = false;
                _mutex.ReleaseMutex();
            }
        } finally {
            _mutex.Dispose();
        }
    }
}

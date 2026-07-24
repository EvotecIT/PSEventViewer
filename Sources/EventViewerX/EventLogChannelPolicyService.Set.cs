using System.Diagnostics.Eventing.Reader;
using System.Threading;

namespace EventViewerX;

/// <summary>
/// Channel policy mutation over the supported Windows Event Log channel API.
/// </summary>
public static partial class EventLogChannelPolicyService {
    /// <summary>
    /// Applies the provided policy and returns true only when every requested
    /// property was already correct or was saved successfully.
    /// </summary>
    public static bool Apply(
        ChannelPolicy policy,
        CancellationToken cancellationToken = default) {

        return ApplyDetailed(
            policy,
            cancellationToken).Success;
    }

    /// <summary>
    /// Applies the provided policy and reports requested, unchanged, persisted,
    /// and failed properties without claiming unsaved values as applied.
    /// </summary>
    public static ChannelPolicyApplyResult ApplyDetailed(
        ChannelPolicy policy,
        CancellationToken cancellationToken = default) {

        ValidatePolicy(policy);
        cancellationToken.ThrowIfCancellationRequested();

        var result = new ChannelPolicyApplyResult {
            LogName = policy.LogName,
            MachineName = policy.MachineName
        };
        PopulateRequestedProperties(
            policy,
            result.RequestedProperties);

        EventLogSession? session = null;
        try {
            EventLogSessionOpenResult sessionResult =
                EventLogSessionManager.CreateSessionResult(
                    policy.MachineName,
                    "ChannelPolicy.Set",
                    policy.LogName,
                    policy.ConnectionTimeoutMilliseconds,
                    emitDiagnostics: false,
                    credential: policy.Credential,
                    authentication:
                        policy.Authentication);
            session = sessionResult.Session;
            if (session == null) {
                result.Errors.Add(
                    string.IsNullOrWhiteSpace(
                        sessionResult.ErrorMessage)
                        ? $"The event-log session could not be opened ({sessionResult.Status})."
                        : sessionResult.ErrorMessage);
                return result;
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var configuration =
                new EventLogConfiguration(
                    policy.LogName,
                    session);
            result.Before = CreateSnapshot(
                configuration,
                policy.MachineName,
                policy.Credential,
                policy.Authentication,
                policy.ConnectionTimeoutMilliseconds);

            var pendingChanges = new List<string>();
            ApplyRequestedProperties(
                policy,
                configuration,
                pendingChanges,
                result);

            if (pendingChanges.Count > 0) {
                try {
                    cancellationToken.ThrowIfCancellationRequested();
                    configuration.SaveChanges();
                    cancellationToken.ThrowIfCancellationRequested();
                    result.AppliedProperties.AddRange(
                        pendingChanges);
                } catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested) {
                    throw;
                } catch (Exception exception) {
                    result.Errors.Add(
                        $"Failed to save channel policy: {exception.Message}");
                }
            }

            try {
                using var refreshed =
                    new EventLogConfiguration(
                        policy.LogName,
                        session);
                result.After = CreateSnapshot(
                    refreshed,
                    policy.MachineName,
                    policy.Credential,
                    policy.Authentication,
                    policy.ConnectionTimeoutMilliseconds);
                VerifyRequestedProperties(
                    policy,
                    result.After,
                    result);
            } catch (Exception exception) {
                result.Errors.Add(
                    $"Failed to verify the saved channel policy: {exception.Message}");
            }
        } catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception exception) {
            result.Errors.Add(exception.Message);
        } finally {
            session?.Dispose();
        }

        int completedCount =
            result.AppliedProperties.Count +
            result.UnchangedProperties.Count;
        result.Success =
            result.Errors.Count == 0 &&
            result.SkippedOrUnsupported.Count == 0 &&
            completedCount ==
            result.RequestedProperties.Count;
        result.PartialSuccess =
            !result.Success &&
            completedCount > 0;
        return result;
    }

    private static void ApplyRequestedProperties(
        ChannelPolicy policy,
        EventLogConfiguration configuration,
        ICollection<string> pendingChanges,
        ChannelPolicyApplyResult result) {

        if (policy.IsEnabled.HasValue) {
            SetProperty(
                nameof(policy.IsEnabled),
                policy.IsEnabled.Value ==
                configuration.IsEnabled,
                () => configuration.IsEnabled =
                    policy.IsEnabled.Value,
                pendingChanges,
                result);
        }
        if (policy.MaximumSizeInBytes.HasValue) {
            SetProperty(
                nameof(policy.MaximumSizeInBytes),
                policy.MaximumSizeInBytes.Value ==
                configuration.MaximumSizeInBytes,
                () => configuration.MaximumSizeInBytes =
                    policy.MaximumSizeInBytes.Value,
                pendingChanges,
                result);
        }
        if (policy.LogFilePath != null) {
            SetProperty(
                nameof(policy.LogFilePath),
                string.Equals(
                    policy.LogFilePath,
                    configuration.LogFilePath,
                    StringComparison.OrdinalIgnoreCase),
                () => configuration.LogFilePath =
                    policy.LogFilePath,
                pendingChanges,
                result);
        }
        if (policy.Mode.HasValue) {
            SetProperty(
                nameof(policy.Mode),
                policy.Mode.Value ==
                configuration.LogMode,
                () => configuration.LogMode =
                    policy.Mode.Value,
                pendingChanges,
                result);
        }
        if (policy.SecurityDescriptor != null) {
            SetProperty(
                nameof(policy.SecurityDescriptor),
                string.Equals(
                    policy.SecurityDescriptor,
                    configuration.SecurityDescriptor,
                    StringComparison.Ordinal),
                () => configuration.SecurityDescriptor =
                    policy.SecurityDescriptor,
                pendingChanges,
                result);
        }
    }

    private static void SetProperty(
        string propertyName,
        bool unchanged,
        Action setter,
        ICollection<string> pendingChanges,
        ChannelPolicyApplyResult result) {

        if (unchanged) {
            result.UnchangedProperties.Add(
                propertyName);
            return;
        }
        try {
            setter();
            pendingChanges.Add(propertyName);
        } catch (Exception exception) {
            result.Errors.Add(
                $"Failed to set {propertyName}: {exception.Message}");
        }
    }

    private static void PopulateRequestedProperties(
        ChannelPolicy policy,
        ICollection<string> requestedProperties) {

        if (policy.IsEnabled.HasValue) {
            requestedProperties.Add(
                nameof(policy.IsEnabled));
        }
        if (policy.MaximumSizeInBytes.HasValue) {
            requestedProperties.Add(
                nameof(policy.MaximumSizeInBytes));
        }
        if (policy.LogFilePath != null) {
            requestedProperties.Add(
                nameof(policy.LogFilePath));
        }
        if (policy.Mode.HasValue) {
            requestedProperties.Add(
                nameof(policy.Mode));
        }
        if (policy.SecurityDescriptor != null) {
            requestedProperties.Add(
                nameof(policy.SecurityDescriptor));
        }
    }

    private static void VerifyRequestedProperties(
        ChannelPolicy requested,
        ChannelPolicy actual,
        ChannelPolicyApplyResult result) {

        VerifyProperty(
            requested.IsEnabled.HasValue,
            nameof(requested.IsEnabled),
            requested.IsEnabled,
            actual.IsEnabled,
            result);
        VerifyProperty(
            requested.MaximumSizeInBytes.HasValue,
            nameof(requested.MaximumSizeInBytes),
            requested.MaximumSizeInBytes,
            actual.MaximumSizeInBytes,
            result);
        VerifyProperty(
            requested.LogFilePath != null,
            nameof(requested.LogFilePath),
            requested.LogFilePath,
            actual.LogFilePath,
            result,
            StringComparer.OrdinalIgnoreCase);
        VerifyProperty(
            requested.Mode.HasValue,
            nameof(requested.Mode),
            requested.Mode,
            actual.Mode,
            result);
        VerifyProperty(
            requested.SecurityDescriptor != null,
            nameof(requested.SecurityDescriptor),
            requested.SecurityDescriptor,
            actual.SecurityDescriptor,
            result,
            StringComparer.Ordinal);
    }

    private static void VerifyProperty<T>(
        bool requested,
        string propertyName,
        T expected,
        T actual,
        ChannelPolicyApplyResult result,
        IEqualityComparer<T>? comparer = null) {

        if (!requested ||
            (comparer ?? EqualityComparer<T>.Default)
            .Equals(expected, actual)) {
            return;
        }
        result.AppliedProperties.Remove(
            propertyName);
        result.UnchangedProperties.Remove(
            propertyName);
        result.Errors.Add(
            $"Verification failed for {propertyName}: Windows retained '{actual}' instead of '{expected}'.");
    }

    private static void ValidatePolicy(
        ChannelPolicy policy) {

        if (policy == null) {
            throw new ArgumentNullException(
                nameof(policy));
        }
        if (string.IsNullOrWhiteSpace(
                policy.LogName)) {
            throw new ArgumentException(
                "LogName is required.",
                nameof(policy));
        }
        if (policy.ConnectionTimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                "Connection timeout must be greater than zero.");
        }
        if (policy.MaximumSizeInBytes is <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                "MaximumSizeInBytes must be greater than zero when specified.");
        }
        if (policy.LogFilePath != null &&
            string.IsNullOrWhiteSpace(
                policy.LogFilePath)) {
            throw new ArgumentException(
                "LogFilePath cannot be empty when specified.",
                nameof(policy));
        }
        if (policy.SecurityDescriptor != null &&
            string.IsNullOrWhiteSpace(
                policy.SecurityDescriptor)) {
            throw new ArgumentException(
                "SecurityDescriptor cannot be empty when specified.",
                nameof(policy));
        }
        if (!policy.IsEnabled.HasValue &&
            !policy.MaximumSizeInBytes.HasValue &&
            policy.LogFilePath == null &&
            !policy.Mode.HasValue &&
            policy.SecurityDescriptor == null) {
            throw new ArgumentException(
                "At least one mutable channel policy property is required.",
                nameof(policy));
        }
    }
}

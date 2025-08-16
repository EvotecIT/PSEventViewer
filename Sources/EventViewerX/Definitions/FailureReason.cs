namespace EventViewerX;

/// <summary>
/// Failure reasons returned for unsuccessful authentication attempts.
/// </summary>
public enum FailureReason {
    None = 0,
    // Common failure reasons (prefixed with %%)
    UnknownUserNameOrBadPassword = 2304,
    AccountRestrictions = 2305,
    AccountLockedOut = 2307,
    AccountExpired = 2306,
    LogonTypeNotGranted = 2309,
    PasswordExpired = 2308
}

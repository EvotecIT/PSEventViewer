using System.Net;

namespace EventViewerX;

/// <summary>
/// Compares the complete identity of credentials used to create Windows Event
/// Log sessions.
/// </summary>
internal static class EventLogCredentialIdentity {
    internal static bool AreEqual(
        NetworkCredential? left,
        NetworkCredential? right) {

        if (ReferenceEquals(left, right)) {
            return true;
        }
        if (left == null || right == null) {
            return false;
        }
        return string.Equals(
                   left.Domain,
                   right.Domain,
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   left.UserName,
                   right.UserName,
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   left.Password,
                   right.Password,
                   StringComparison.Ordinal);
    }
}

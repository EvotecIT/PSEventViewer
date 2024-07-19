namespace EventViewerX;

/// <summary>
/// Event helper methods.
/// </summary>
internal static class EventsHelper {
    /// <summary>
    /// Translates a string value to an ImpersonationLevel enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated ImpersonationLevel enum.</returns>
    public static ImpersonationLevel? GetImpersonationLevel(string value) {
        if (Enum.TryParse(value.TrimStart('%', '%'), out ImpersonationLevel impersonationLevel)) {
            return impersonationLevel;
        }
        return null;
    }

    /// <summary>
    /// Translates a string value to a VirtualAccount enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated VirtualAccount enum.</returns>
    public static VirtualAccount? GetVirtualAccount(string value) {
        if (Enum.TryParse(value.TrimStart('%', '%'), out VirtualAccount virtualAccount)) {
            return virtualAccount;
        }
        return null;
    }

    /// <summary>
    /// Translates a string value to an ElevatedToken enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated ElevatedToken enum.</returns>
    public static ElevatedToken? GetElevatedToken(string value) {
        if (Enum.TryParse(value.TrimStart('%', '%'), out ElevatedToken elevatedToken)) {
            return elevatedToken;
        }
        return null;
    }

    /// <summary>
    /// Translates a string value to a LogonType enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated LogonType enum.</returns>
    public static LogonType? GetLogonType(string value) {
        if (Enum.TryParse(value, out LogonType logonType)) {
            return logonType;
        }
        return null;
    }


    /// <summary>
    /// Translates a string value to a TicketOptions enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated TicketOptions enum.</returns>
    public static TicketOptions? GetTicketOptions(string value) {
        if (Enum.TryParse(value, out TicketOptions ticketOptions)) {
            return ticketOptions;
        }
        return null;
    }

    /// <summary>
    /// Translates a string value to a Status enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated Status enum.</returns>
    public static Status? GetStatus(string value) {
        if (Enum.TryParse(value, out Status status)) {
            return status;
        }
        return null;
    }

    /// <summary>
    /// Translates a string value to a TicketEncryptionType enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated TicketEncryptionType enum.</returns>
    public static TicketEncryptionType? GetTicketEncryptionType(string value) {
        if (Enum.TryParse(value, out TicketEncryptionType ticketEncryptionType)) {
            return ticketEncryptionType;
        }
        return null;
    }

    /// <summary>
    /// Translates a string value to a PreAuthType enum.
    /// </summary>
    /// <param name="value">The value to translate.</param>
    /// <returns>The translated PreAuthType enum.</returns>
    public static PreAuthType? GetPreAuthType(string value) {
        if (Enum.TryParse(value, out PreAuthType preAuthType)) {
            return preAuthType;
        }
        return null;
    }
}
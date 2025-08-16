namespace EventViewerX;

/// <summary>
/// Protector types available for BitLocker encryption.
/// </summary>
public enum BitLockerProtectorType {
    Tpm = 2200,
    ExternalKey = 2201,
    NumericalPassword = 2202,
    TpmPin = 2203,
    TpmStartupKey = 2204,
    TpmPinAndStartupKey = 2205,
    Passphrase = 2206,
    RecoveryKey = 2207
}

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Provides a stable, hashed machine identifier for the current platform.
/// </summary>
public interface IMachineFingerprint
{
    /// <summary>
    /// Returns the SHA-256 hash of the platform-specific machine identifier.
    /// The raw identifier never leaves the machine.
    /// </summary>
    string GetFingerprintHash();

    /// <summary>
    /// Returns the detected platform name (windows, linux, or macos).
    /// </summary>
    string GetPlatform();
}

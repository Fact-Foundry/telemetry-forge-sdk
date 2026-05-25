namespace FactFoundry.TelemetryForge.Core;

/// <summary>
/// Base configuration options shared by all TelemetryForge packages.
/// </summary>
public abstract class TelemetryOptionsBase
{
    /// <summary>
    /// URL of the TelemetryForge Server instance (e.g., "https://telemetry.yourdomain.com").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Per-site or per-app API key issued during registration.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

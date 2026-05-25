using FactFoundry.TelemetryForge.Core;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Configuration options for desktop application telemetry.
/// </summary>
public sealed class DesktopTelemetryOptions : TelemetryOptionsBase
{
    /// <summary>
    /// Application version string (e.g., "1.2.3"). Populated automatically from the entry assembly if not set.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Optional JWT from an existing licensing system, enabling the server to correlate
    /// sessions with license records.
    /// </summary>
    public string? LicenseJwt { get; set; }
}

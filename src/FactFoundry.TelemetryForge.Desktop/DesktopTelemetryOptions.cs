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
    /// Interval in minutes between heartbeat flushes that send feature/error deltas to the server.
    /// Set to 0 or null to disable periodic heartbeats (only flush at shutdown).
    /// Defaults to 15 minutes.
    /// </summary>
    public int? HeartbeatIntervalMinutes { get; set; } = 15;
}

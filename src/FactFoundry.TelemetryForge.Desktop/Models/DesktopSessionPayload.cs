using System.Text.Json.Serialization;

namespace FactFoundry.TelemetryForge.Desktop.Models;

/// <summary>
/// Payload sent to the TelemetryForge Server for a desktop application session.
/// Sent on the initial flush and on each subsequent heartbeat with incremental data.
/// </summary>
public sealed class DesktopSessionPayload
{
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("sequence")]
    public required int Sequence { get; init; }

    [JsonPropertyName("app_version")]
    public string? AppVersion { get; init; }

    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    [JsonPropertyName("os_version")]
    public required string OsVersion { get; init; }

    [JsonPropertyName("fingerprint_hash")]
    public required string FingerprintHash { get; init; }

    [JsonPropertyName("license_jwt")]
    public string? LicenseJwt { get; init; }

    [JsonPropertyName("session_start")]
    public required DateTimeOffset SessionStart { get; init; }

    [JsonPropertyName("session_end")]
    public required DateTimeOffset SessionEnd { get; init; }

    [JsonPropertyName("duration_ms")]
    public required long DurationMs { get; init; }

    [JsonPropertyName("feature_path")]
    public required IReadOnlyList<string> FeaturePath { get; init; }

    [JsonPropertyName("error_events")]
    public required IReadOnlyList<ErrorEvent> ErrorEvents { get; init; }
}

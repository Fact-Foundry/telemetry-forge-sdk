using System.Text.Json.Serialization;

namespace FactFoundry.TelemetryForge.Desktop.Models;

/// <summary>
/// Payload sent to the TelemetryForge Server for a desktop application session.
/// Sent on the initial flush and on each subsequent heartbeat with incremental data.
/// </summary>
public sealed class DesktopSessionPayload
{
    /// <summary>
    /// Unique identifier for this session (UUID).
    /// </summary>
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Monotonically increasing counter for heartbeat ordering within a session.
    /// </summary>
    [JsonPropertyName("sequence")]
    public required int Sequence { get; init; }

    /// <summary>
    /// Application version string (e.g. "1.2.3"), or null if not set and no entry assembly is available.
    /// </summary>
    [JsonPropertyName("app_version")]
    public string? AppVersion { get; init; }

    /// <summary>
    /// Runtime platform identifier (e.g. "Windows", "Linux", "macOS").
    /// </summary>
    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    /// <summary>
    /// Friendly OS name and kernel version (e.g. "Windows 11 (22631) | Windows 10.0.22631").
    /// </summary>
    [JsonPropertyName("os_version")]
    public required string OsVersion { get; init; }

    /// <summary>
    /// SHA-256 hash of the machine fingerprint. Raw identifiers are never transmitted.
    /// </summary>
    [JsonPropertyName("fingerprint_hash")]
    public required string FingerprintHash { get; init; }

    /// <summary>
    /// When the session started (UTC).
    /// </summary>
    [JsonPropertyName("session_start")]
    public required DateTimeOffset SessionStart { get; init; }

    /// <summary>
    /// Timestamp of this flush (UTC).
    /// </summary>
    [JsonPropertyName("session_end")]
    public required DateTimeOffset SessionEnd { get; init; }

    /// <summary>
    /// Elapsed time in milliseconds from session start to this flush.
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public required long DurationMs { get; init; }

    /// <summary>
    /// Feature/screen names visited since the last flush (delta, not cumulative).
    /// </summary>
    [JsonPropertyName("feature_path")]
    public required IReadOnlyList<string> FeaturePath { get; init; }

    /// <summary>
    /// Errors captured since the last flush (delta, not cumulative).
    /// </summary>
    [JsonPropertyName("error_events")]
    public required IReadOnlyList<ErrorEvent> ErrorEvents { get; init; }
}

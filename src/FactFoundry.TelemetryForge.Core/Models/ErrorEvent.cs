namespace FactFoundry.TelemetryForge.Core.Models;

/// <summary>
/// An error captured during a telemetry session.
/// </summary>
public sealed class ErrorEvent
{
    /// <summary>
    /// The feature or component where the error occurred.
    /// </summary>
    public required string Feature { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// When the error occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

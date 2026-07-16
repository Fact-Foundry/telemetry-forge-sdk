namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// A serialized telemetry payload queued for background delivery.
/// </summary>
internal sealed record TelemetryMessage(string Path, string SerializedPayload);

namespace FactFoundry.TelemetryForge.Web;

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

    /// <summary>
    /// Additional TelemetryForge servers to mirror every payload to, alongside the primary
    /// <see cref="Endpoint"/>. Each mirror carries its own API key. Sends are best-effort and
    /// independent — a slow or failing mirror never blocks or affects the primary or your app.
    /// Leave empty (the default) to send to the primary endpoint only.
    /// </summary>
    public IList<TelemetryTarget> Mirrors { get; } = new List<TelemetryTarget>();

    /// <summary>
    /// Maximum number of telemetry events that can be queued for background delivery.
    /// When the queue is full, the oldest event is discarded. Default is 1000.
    /// </summary>
    public int SendQueueCapacity { get; set; } = 1000;
}

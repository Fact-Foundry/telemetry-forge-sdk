namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// A single TelemetryForge Server destination — an endpoint URL and its API key.
/// Used to mirror telemetry to more than one server (see <see cref="TelemetryOptionsBase.Mirrors"/>).
/// </summary>
public sealed class TelemetryTarget
{
    /// <summary>Creates an empty target (for configuration binding).</summary>
    public TelemetryTarget() { }

    /// <summary>Creates a target for the given server URL and API key.</summary>
    public TelemetryTarget(string endpoint, string apiKey)
    {
        Endpoint = endpoint;
        ApiKey = apiKey;
    }

    /// <summary>URL of the TelemetryForge Server instance (e.g., "https://telemetry.yourdomain.com").</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Per-site or per-app API key issued by this server.</summary>
    public string ApiKey { get; set; } = string.Empty;
}

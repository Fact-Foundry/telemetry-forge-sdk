namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Configuration options for web application telemetry.
/// </summary>
public sealed class WebTelemetryOptions : TelemetryOptionsBase
{
    /// <summary>
    /// When true, reads the <c>_ga</c> cookie (if present) and includes its hash in the payload
    /// for returning-visitor resolution. Defaults to false (opt-in).
    /// </summary>
    public bool UseGaCookie { get; set; }

    /// <summary>
    /// CDN/reverse proxy provider used for geolocation headers.
    /// Set this to avoid checking all providers on every request.
    /// Defaults to <see cref="GeoProvider.Auto"/> (checks all known providers).
    /// </summary>
    public GeoProvider GeoProvider { get; set; } = GeoProvider.Auto;
}

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Configuration options for web application telemetry.
/// </summary>
public sealed class WebTelemetryOptions : TelemetryOptionsBase
{
    /// <summary>
    /// When true, the library will not track sessions for requests that include the DNT header.
    /// Defaults to true.
    /// </summary>
    public bool RespectDnt { get; set; } = true;

    /// <summary>
    /// When true, reads the <c>_ga</c> cookie (if present) and includes its hash in the payload
    /// for returning-visitor resolution. Defaults to false (opt-in).
    /// </summary>
    public bool UseGaCookie { get; set; }
}

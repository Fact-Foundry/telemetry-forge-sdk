namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// Configuration options for HTTP API telemetry.
/// </summary>
public sealed class ApiTelemetryOptions : TelemetryOptionsBase
{
    /// <summary>
    /// Request path prefixes to exclude from telemetry (case-insensitive), e.g. health checks
    /// or API docs ("/health", "/swagger"). Matched requests are not reported.
    /// </summary>
    public IList<string> ExcludedPathPrefixes { get; } = [];

    /// <summary>
    /// CDN/reverse proxy that injects the caller's geolocation headers (e.g. Cloudflare's
    /// <c>CF-IPCountry</c>) into the inbound request. Used to report the caller's country
    /// without ever handling an IP. Defaults to <see cref="GeoProvider.Auto"/> (checks all
    /// known providers); set explicitly to skip the others, or to <see cref="GeoProvider.None"/>
    /// if not behind a CDN.
    /// </summary>
    public GeoProvider GeoProvider { get; set; } = GeoProvider.Auto;
}

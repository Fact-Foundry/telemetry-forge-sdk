namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// CDN/reverse proxy provider that injects geolocation headers into requests.
/// Setting this avoids checking all providers on every request.
/// </summary>
public enum GeoProvider
{
    /// <summary>
    /// Auto-detect by checking all known provider headers. Least performant.
    /// </summary>
    Auto,

    /// <summary>
    /// Cloudflare: reads <c>CF-IPCountry</c> and <c>CF-Region</c> headers.
    /// </summary>
    Cloudflare,

    /// <summary>
    /// AWS CloudFront: reads <c>CloudFront-Viewer-Country</c> and <c>CloudFront-Viewer-Country-Region</c> headers.
    /// </summary>
    CloudFront,

    /// <summary>
    /// Vercel: reads <c>x-vercel-ip-country</c> and <c>x-vercel-ip-country-region</c> headers.
    /// </summary>
    Vercel,

    /// <summary>
    /// Akamai: parses <c>X-Akamai-Edgescape</c> header for <c>country_code</c> and <c>region_code</c> values.
    /// </summary>
    Akamai,

    /// <summary>
    /// No geolocation headers are read. Use this if not behind a CDN.
    /// </summary>
    None
}

using System.Text.Json.Serialization;

namespace FactFoundry.TelemetryForge.Api.Models;

/// <summary>
/// Payload sent to the TelemetryForge Server for a single API request.
/// Carries auto-captured request health data only — no caller IP, body, or PII.
/// </summary>
public sealed class ApiEventPayload
{
    /// <summary>
    /// Low-cardinality route template (e.g. "/license/{id}"), never the raw resolved path.
    /// </summary>
    [JsonPropertyName("route_template")]
    public required string RouteTemplate { get; init; }

    /// <summary>
    /// HTTP method (GET, POST, etc.).
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    /// HTTP response status code.
    /// </summary>
    [JsonPropertyName("status_code")]
    public required int StatusCode { get; init; }

    /// <summary>
    /// Request handling latency in milliseconds.
    /// </summary>
    [JsonPropertyName("latency_ms")]
    public required int LatencyMs { get; init; }

    /// <summary>
    /// When the request occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Caller's country as an ISO 3166-1 alpha-2 code, read from a CDN geolocation header
    /// (e.g. <c>CF-IPCountry</c>) on the inbound request. Null when not behind a CDN or the
    /// header is absent. No IP is ever sent — only this resolved code.
    /// </summary>
    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; init; }

    /// <summary>
    /// Consumer-defined business outcome for the request (e.g. "license_valid"), set via
    /// <see cref="HttpContextExtensions.SetTelemetryOutcome"/>. Distinct from the HTTP status
    /// code. Null when the handler set none.
    /// </summary>
    [JsonPropertyName("outcome")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Outcome { get; init; }
}

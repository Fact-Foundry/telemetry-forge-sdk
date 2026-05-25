using System.Text.Json.Serialization;

namespace FactFoundry.TelemetryForge.Web.Models;

/// <summary>
/// Payload sent to the TelemetryForge Server for a single web event.
/// </summary>
public sealed class WebEventPayload
{
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }

    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("ip_address")]
    public required string IpAddress { get; init; }

    [JsonPropertyName("ga_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GaValue { get; init; }

    [JsonPropertyName("user_agent")]
    public required string UserAgent { get; init; }

    [JsonPropertyName("referrer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Referrer { get; init; }

    [JsonPropertyName("language")]
    public required string Language { get; init; }

    [JsonPropertyName("page_path")]
    public required string PagePath { get; init; }

    [JsonPropertyName("status_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StatusCode { get; init; }

    [JsonPropertyName("duration_ms")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? DurationMs { get; init; }

    [JsonPropertyName("sec_ch_ua")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SecChUa { get; init; }

    [JsonPropertyName("sec_ch_ua_mobile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SecChUaMobile { get; init; }

    [JsonPropertyName("sec_ch_ua_platform")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SecChUaPlatform { get; init; }

    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; init; }

    [JsonPropertyName("region")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Region { get; init; }

    [JsonPropertyName("event_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EventName { get; init; }

    [JsonPropertyName("event_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object>? EventData { get; init; }

    [JsonPropertyName("target_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetUrl { get; init; }
}

using System.Text.Json.Serialization;

namespace FactFoundry.TelemetryForge.Web.Models;

/// <summary>
/// Payload sent to the TelemetryForge Server for a web session.
/// </summary>
public sealed class WebSessionPayload
{
    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    [JsonPropertyName("session_start")]
    public required DateTimeOffset SessionStart { get; init; }

    [JsonPropertyName("session_end")]
    public required DateTimeOffset SessionEnd { get; init; }

    [JsonPropertyName("duration_ms")]
    public required long DurationMs { get; init; }

    [JsonPropertyName("ip_address")]
    public required string IpAddress { get; init; }

    [JsonPropertyName("ga_value")]
    public string? GaValue { get; init; }

    [JsonPropertyName("user_agent")]
    public required string UserAgent { get; init; }

    [JsonPropertyName("referrer")]
    public string? Referrer { get; init; }

    [JsonPropertyName("language")]
    public required string Language { get; init; }

    [JsonPropertyName("entry_page")]
    public required string EntryPage { get; init; }

    [JsonPropertyName("exit_page")]
    public required string ExitPage { get; init; }

    [JsonPropertyName("page_path")]
    public required IReadOnlyList<string> PagePath { get; init; }

    [JsonPropertyName("status_codes")]
    public required IReadOnlyDictionary<string, int> StatusCodes { get; init; }

    [JsonPropertyName("dnt")]
    public required bool Dnt { get; init; }
}

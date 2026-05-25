using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// HTTP client that posts telemetry payloads to a TelemetryForge Server instance.
/// Configured with resilience policies via <c>Microsoft.Extensions.Http.Resilience</c>.
/// </summary>
public sealed class TelemetryForgeHttpClient : ITelemetryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelemetryForgeHttpClient> _logger;

    public TelemetryForgeHttpClient(HttpClient httpClient, ILogger<TelemetryForgeHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(string path, T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(path, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "TelemetryForge server returned {StatusCode} for {Path}",
                    (int)response.StatusCode, path);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send telemetry payload to {Path}", path);
        }
    }
}

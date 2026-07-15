using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Posts telemetry payloads to one or more TelemetryForge Server instances (the primary
/// <see cref="TelemetryOptionsBase.Endpoint"/> plus any configured
/// <see cref="TelemetryOptionsBase.Mirrors"/>). Each target is sent to concurrently and
/// independently — a slow or failing target never blocks or fails the others. Requests use
/// the named "TelemetryForge" <see cref="HttpClient"/>, which carries the resilience policies.
/// </summary>
public sealed class TelemetryForgeHttpClient : ITelemetryClient
{
    internal const string HttpClientName = "TelemetryForge";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelemetryForgeHttpClient> _logger;
    private readonly IReadOnlyList<TelemetryTarget> _targets;

    public TelemetryForgeHttpClient(
        IHttpClientFactory httpClientFactory,
        IOptions<WebTelemetryOptions> options,
        ILogger<TelemetryForgeHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _targets = BuildTargets(options.Value);
    }

    /// <summary>Resolves the primary endpoint plus any mirrors into the set of targets to send to.</summary>
    private static IReadOnlyList<TelemetryTarget> BuildTargets(TelemetryOptionsBase options)
    {
        var targets = new List<TelemetryTarget>();

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
            targets.Add(new TelemetryTarget(options.Endpoint, options.ApiKey));

        foreach (var mirror in options.Mirrors)
        {
            if (!string.IsNullOrWhiteSpace(mirror.Endpoint))
                targets.Add(mirror);
        }

        return targets;
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(string path, T payload, CancellationToken cancellationToken = default)
    {
        if (_targets.Count == 0)
            return;

        // Fan out to every target at once; each send is isolated so one failure can't affect the rest.
        await Task.WhenAll(_targets.Select(target => SendToTargetAsync(target, path, payload, cancellationToken)));
    }

    private async Task SendToTargetAsync<T>(
        TelemetryTarget target, string path, T payload, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var url = $"{target.Endpoint.TrimEnd('/')}/{path.TrimStart('/')}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-TelemetryForge-Key", target.ApiKey);

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "TelemetryForge server {Host} returned {StatusCode} for {Path}",
                    HostOf(target.Endpoint), (int)response.StatusCode, path);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send telemetry payload to {Host} for {Path}",
                HostOf(target.Endpoint), path);
        }
    }

    private static string HostOf(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : endpoint;
}

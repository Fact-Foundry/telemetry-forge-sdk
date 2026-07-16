using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// Posts telemetry payloads to one or more TelemetryForge Server instances (the primary
/// <see cref="TelemetryOptionsBase.Endpoint"/> plus any configured
/// <see cref="TelemetryOptionsBase.Mirrors"/>). Each target is sent to concurrently and
/// independently — a slow or failing target never blocks or fails the others. Requests use
/// the named "TelemetryForge" <see cref="HttpClient"/>, which carries the resilience policies.
/// Every request also carries the SDK version header so the server can record which SDK
/// version each app is running.
/// </summary>
public sealed class TelemetryForgeHttpClient : ITelemetryClient
{
    internal const string HttpClientName = "TelemetryForge";

    /// <summary>
    /// This SDK's version, read once from the assembly's informational version (build
    /// metadata after '+' stripped). Sent as a header on every telemetry post so the server
    /// can record which SDK version each app is running, without any per-event cost.
    /// </summary>
    private static readonly string SdkVersion = ResolveSdkVersion();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelemetryForgeHttpClient> _logger;
    private readonly IReadOnlyList<TelemetryTarget> _targets;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryForgeHttpClient"/> class.
    /// </summary>
    public TelemetryForgeHttpClient(
        IHttpClientFactory httpClientFactory,
        IOptions<ApiTelemetryOptions> options,
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

    /// <summary>
    /// Sends an already-serialized JSON payload to all configured targets. Used by
    /// <see cref="TelemetrySendWorker"/> to forward items that were serialized at enqueue time.
    /// </summary>
    internal async Task SendPreserializedAsync(string path, string json, CancellationToken cancellationToken = default)
    {
        if (_targets.Count == 0)
            return;

        await Task.WhenAll(_targets.Select(target => SendJsonToTargetAsync(target, path, json, cancellationToken)));
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
            request.Headers.Add("X-TelemetryForge-Sdk-Version", SdkVersion);

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

    private async Task SendJsonToTargetAsync(
        TelemetryTarget target, string path, string json, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var url = $"{target.Endpoint.TrimEnd('/')}/{path.TrimStart('/')}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-TelemetryForge-Key", target.ApiKey);
            request.Headers.Add("X-TelemetryForge-Sdk-Version", SdkVersion);

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

    private static string ResolveSdkVersion()
    {
        var informational = typeof(TelemetryForgeHttpClient).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
        {
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return typeof(TelemetryForgeHttpClient).Assembly.GetName().Version?.ToString() ?? "unknown";
    }
}

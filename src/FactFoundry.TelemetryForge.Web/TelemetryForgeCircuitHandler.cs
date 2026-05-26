using FactFoundry.TelemetryForge.Web.Models;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Blazor Server circuit handler that sends a <c>circuit_open</c> event on connect,
/// per-navigation <c>page_view</c> events, and a <c>circuit_close</c> event on disconnect.
/// Also implements <see cref="ITelemetryForge"/> for custom event tracking within
/// a Blazor circuit.
/// </summary>
public sealed class TelemetryForgeCircuitHandler : CircuitHandler, ITelemetryForge
{
    private readonly ITelemetryClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly WebTelemetryOptions _options;
    private readonly ILogger<TelemetryForgeCircuitHandler> _logger;

    private string _sessionId = Guid.NewGuid().ToString();
    private string? _cacheKey;
    private RequestContext? _requestContext;
    private string _lastPagePath = "/";

    public TelemetryForgeCircuitHandler(
        ITelemetryClient client,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IOptions<WebTelemetryOptions> options,
        ILogger<TelemetryForgeCircuitHandler> logger)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var cache = _serviceProvider.GetService(typeof(RequestContextAccessor)) as RequestContextAccessor;

        if (httpContext is not null)
        {
            var ip = RequestContext.GetClientIp(httpContext);
            var ua = httpContext.Request.Headers.UserAgent.ToString();
            _cacheKey = RequestContextAccessor.BuildKey(ip, ua);

            _requestContext = cache?.TryGet(_cacheKey);

            var cachedSessionId = cache?.GetSessionId(_cacheKey);
            if (cachedSessionId is not null)
                _sessionId = cachedSessionId;
            else
                cache?.StoreSessionId(_cacheKey, _sessionId);
        }

        if (_requestContext is null && httpContext is not null)
            _requestContext = RequestContext.FromHttpContext(httpContext, _options);

        if (_requestContext is not null)
            _lastPagePath = _requestContext.PagePath;

        await SendEventAsync("circuit_open", _lastPagePath);
    }

    /// <summary>
    /// Records a page navigation within the Blazor circuit and sends a <c>page_view</c> event.
    /// Call this from a <c>NavigationManager.LocationChanged</c> handler.
    /// </summary>
    public void TrackNavigation(string path)
    {
        _lastPagePath = path;
        _ = SendEventAsync("page_view", path);
    }

    /// <inheritdoc />
    public void TrackEvent(string eventName, Dictionary<string, object>? data = null)
    {
        _ = SendEventAsync("custom", _lastPagePath, eventName, data);
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await SendEventAsync("circuit_close", _lastPagePath, cancellationToken: cancellationToken);

        if (_cacheKey is not null)
        {
            var cache = _serviceProvider.GetService(typeof(RequestContextAccessor)) as RequestContextAccessor;
            cache?.RemoveSessionId(_cacheKey);
        }
    }

    private async Task SendEventAsync(
        string eventType,
        string pagePath,
        string? eventName = null,
        IReadOnlyDictionary<string, object>? eventData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rc = _requestContext;

            var payload = new WebEventPayload
            {
                SessionId = _sessionId,
                EventType = eventType,
                Platform = "blazor-server",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = rc?.IpAddress ?? "unknown",
                GaValue = rc?.GaValue,
                UserAgent = rc?.UserAgent ?? string.Empty,
                Referrer = rc?.Referrer,
                Language = rc?.Language ?? string.Empty,
                SecChUa = rc?.SecChUa,
                SecChUaMobile = rc?.SecChUaMobile,
                SecChUaPlatform = rc?.SecChUaPlatform,
                PagePath = pagePath,
                Country = rc?.Country,
                Region = rc?.Region,
                EventName = eventName,
                EventData = eventData
            };

            await _client.SendAsync("/api/telemetry/web", payload, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send {EventType} event for {Path}", eventType, pagePath);
        }
    }
}

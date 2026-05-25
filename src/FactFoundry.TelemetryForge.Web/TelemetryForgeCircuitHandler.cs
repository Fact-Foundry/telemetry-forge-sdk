using FactFoundry.TelemetryForge.Web.Models;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Blazor Server circuit handler that sends per-navigation <c>page_view</c> events
/// and a <c>circuit_close</c> event when the circuit disconnects.
/// Also implements <see cref="ITelemetryForge"/> for custom event tracking within
/// a Blazor circuit.
/// </summary>
public sealed class TelemetryForgeCircuitHandler : CircuitHandler, ITelemetryForge
{
    private readonly ITelemetryClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WebTelemetryOptions _options;
    private readonly ILogger<TelemetryForgeCircuitHandler> _logger;

    private string _ipAddress = string.Empty;
    private string? _gaValue;
    private string _userAgent = string.Empty;
    private string? _referrer;
    private string _language = string.Empty;
    private bool _dnt;
    private string _lastPagePath = "/";

    public TelemetryForgeCircuitHandler(
        ITelemetryClient client,
        IHttpContextAccessor httpContextAccessor,
        IOptions<WebTelemetryOptions> options,
        ILogger<TelemetryForgeCircuitHandler> logger)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is not null)
        {
            _ipAddress = GetClientIp(context);
            _userAgent = context.Request.Headers.UserAgent.ToString();
            _referrer = context.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null;
            _language = context.Request.Headers.AcceptLanguage.ToString();
            _dnt = context.Request.Headers["DNT"].ToString() == "1";

            if (_options.UseGaCookie
                && context.Request.Cookies.TryGetValue("_ga", out var gaValue)
                && !string.IsNullOrEmpty(gaValue))
            {
                _gaValue = gaValue;
            }

            _lastPagePath = context.Request.Path.Value ?? "/";
        }

        if (!(_options.RespectDnt && _dnt))
        {
            _ = SendEventAsync("page_view", _lastPagePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Records a page navigation within the Blazor circuit and sends a <c>page_view</c> event.
    /// Call this from a <c>NavigationManager.LocationChanged</c> handler.
    /// </summary>
    public void TrackNavigation(string path)
    {
        _lastPagePath = path;

        if (_options.RespectDnt && _dnt)
            return;

        _ = SendEventAsync("page_view", path);
    }

    /// <inheritdoc />
    public void TrackEvent(string eventName, Dictionary<string, object>? data = null)
    {
        if (_options.RespectDnt && _dnt)
            return;

        _ = SendEventAsync("custom", _lastPagePath, eventName, data);
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_options.RespectDnt && _dnt)
            return;

        await SendEventAsync("circuit_close", _lastPagePath, cancellationToken: cancellationToken);

        _logger.LogDebug("Blazor circuit close event sent for {Path}", _lastPagePath);
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
            var payload = new WebEventPayload
            {
                EventType = eventType,
                Platform = "blazor-server",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = _ipAddress,
                GaValue = _gaValue,
                UserAgent = _userAgent,
                Referrer = _referrer,
                Language = _language,
                PagePath = pagePath,
                Dnt = _dnt,
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

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(forwarded))
        {
            var firstIp = forwarded.Split(',', StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrEmpty(firstIp))
                return firstIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

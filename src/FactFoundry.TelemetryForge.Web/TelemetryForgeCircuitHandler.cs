using FactFoundry.TelemetryForge.Web.Models;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Blazor Server circuit handler that tracks the full session lifecycle —
/// from circuit open to close — and flushes a single atomic session record.
/// </summary>
public sealed class TelemetryForgeCircuitHandler : CircuitHandler
{
    private readonly ITelemetryClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WebTelemetryOptions _options;
    private readonly ILogger<TelemetryForgeCircuitHandler> _logger;

    private DateTimeOffset _sessionStart;
    private string _ipAddress = string.Empty;
    private string? _gaValue;
    private string _userAgent = string.Empty;
    private string? _referrer;
    private string _language = string.Empty;
    private bool _dnt;
    private readonly List<string> _pagePath = [];
    private readonly Dictionary<string, int> _statusCodes = [];
    private readonly object _lock = new();

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
        _sessionStart = DateTimeOffset.UtcNow;

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

            var path = context.Request.Path.Value ?? "/";
            lock (_lock)
            {
                _pagePath.Add(path);
                IncrementStatus("200");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Records a page navigation within the Blazor circuit session.
    /// Call this from a <c>NavigationManager.LocationChanged</c> handler.
    /// </summary>
    public void TrackNavigation(string path)
    {
        lock (_lock)
        {
            _pagePath.Add(path);
            IncrementStatus("200");
        }
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_options.RespectDnt && _dnt)
            return;

        var sessionEnd = DateTimeOffset.UtcNow;

        List<string> pagePath;
        Dictionary<string, int> statusCodes;

        lock (_lock)
        {
            pagePath = [.. _pagePath];
            statusCodes = new Dictionary<string, int>(_statusCodes);
        }

        var payload = new WebSessionPayload
        {
            Platform = "blazor-server",
            SessionStart = _sessionStart,
            SessionEnd = sessionEnd,
            DurationMs = (long)(sessionEnd - _sessionStart).TotalMilliseconds,
            IpAddress = _ipAddress,
            GaValue = _gaValue,
            UserAgent = _userAgent,
            Referrer = _referrer,
            Language = _language,
            EntryPage = pagePath.Count > 0 ? pagePath[0] : "/",
            ExitPage = pagePath.Count > 0 ? pagePath[^1] : "/",
            PagePath = pagePath,
            StatusCodes = statusCodes,
            Dnt = _dnt
        };

        await _client.SendAsync("/api/telemetry/web", payload, cancellationToken);
        _logger.LogDebug(
            "Blazor circuit telemetry flushed ({DurationMs}ms, {PageCount} pages)",
            payload.DurationMs, pagePath.Count);
    }

    private void IncrementStatus(string code)
    {
        _statusCodes.TryGetValue(code, out var count);
        _statusCodes[code] = count + 1;
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

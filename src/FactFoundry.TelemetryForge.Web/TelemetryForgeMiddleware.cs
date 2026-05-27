using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FactFoundry.TelemetryForge.Web.Models;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// ASP.NET middleware that captures per-request telemetry and posts a single
/// <c>page_view</c> event to the TelemetryForge Server for each HTTP request.
/// </summary>
public sealed class TelemetryForgeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryClient _client;
    private readonly WebTelemetryOptions _options;
    private readonly ILogger<TelemetryForgeMiddleware> _logger;

    public TelemetryForgeMiddleware(
        RequestDelegate next,
        ITelemetryClient client,
        IOptions<WebTelemetryOptions> options,
        ILogger<TelemetryForgeMiddleware> logger)
    {
        _next = next;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        var rc = RequestContext.FromHttpContext(context, _options);

        var cache = context.RequestServices.GetService(typeof(RequestContextAccessor)) as RequestContextAccessor;
        var cacheKey = RequestContextAccessor.BuildKey(rc.IpAddress, rc.UserAgent);
        cache?.Store(cacheKey, rc);

        var connectionIp = context.Connection.RemoteIpAddress?.ToString();
        string? fallbackKey = null;
        if (connectionIp is not null && connectionIp != rc.IpAddress)
        {
            fallbackKey = RequestContextAccessor.BuildKey(connectionIp, rc.UserAgent);
            cache?.Store(fallbackKey, rc);
        }

        var sessionId = cache?.GetSessionId(cacheKey);
        if (sessionId is null)
        {
            sessionId = Guid.NewGuid().ToString();
            cache?.StoreSessionId(cacheKey, sessionId);
            if (fallbackKey is not null)
                cache?.StoreSessionId(fallbackKey, sessionId);
        }

        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        try
        {
            var payload = new WebEventPayload
            {
                SessionId = sessionId,
                EventType = "page_view",
                Platform = "aspnet",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = rc.IpAddress,
                GaValue = rc.GaValue,
                UserAgent = rc.UserAgent,
                Referrer = rc.Referrer,
                Language = rc.Language,
                SecChUa = rc.SecChUa,
                SecChUaMobile = rc.SecChUaMobile,
                SecChUaPlatform = rc.SecChUaPlatform,
                PagePath = rc.PagePath,
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Country = rc.Country,
                Region = rc.Region
            };

            await _client.SendAsync("/api/telemetry/web", payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture web telemetry for {Path}", context.Request.Path);
        }
    }

    private static bool ShouldSkip(HttpContext context)
    {
        return IsStaticFile(context.Request.Path);
    }

    private static bool IsStaticFile(PathString path)
    {
        var value = path.Value;
        if (value is null) return false;

        return value.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase);
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FactFoundry.TelemetryForge.Core;
using FactFoundry.TelemetryForge.Web.Models;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// ASP.NET middleware that captures request telemetry and posts session payloads
/// to the TelemetryForge Server. For non-Blazor requests, each request is treated
/// as a single-page session.
/// </summary>
public sealed class TelemetryForgeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryClient _client;
    private readonly WebTelemetryOptions _options;
    private readonly IpHashingService _ipHashingService;
    private readonly ILogger<TelemetryForgeMiddleware> _logger;

    public TelemetryForgeMiddleware(
        RequestDelegate next,
        ITelemetryClient client,
        IOptions<WebTelemetryOptions> options,
        IpHashingService ipHashingService,
        ILogger<TelemetryForgeMiddleware> logger)
    {
        _next = next;
        _client = client;
        _options = options.Value;
        _ipHashingService = ipHashingService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        var sessionStart = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var sessionEnd = DateTimeOffset.UtcNow;

        try
        {
            var ip = GetClientIp(context);
            var path = context.Request.Path.Value ?? "/";

            var payload = new WebSessionPayload
            {
                Platform = "aspnet",
                SessionStart = sessionStart,
                SessionEnd = sessionEnd,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IpHash = _ipHashingService.HashForSession(ip),
                GaHash = GetGaHash(context),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Referrer = context.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
                Language = context.Request.Headers.AcceptLanguage.ToString(),
                EntryPage = path,
                ExitPage = path,
                PagePath = [path],
                StatusCodes = new Dictionary<string, int>
                {
                    [context.Response.StatusCode.ToString()] = 1
                },
                Dnt = HasDnt(context)
            };

            await _client.SendAsync("/api/telemetry/web", payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture web telemetry for {Path}", context.Request.Path);
        }
    }

    private bool ShouldSkip(HttpContext context)
    {
        if (_options.RespectDnt && HasDnt(context))
            return true;

        if (IsStaticFile(context.Request.Path))
            return true;

        return false;
    }

    private static bool HasDnt(HttpContext context)
    {
        return context.Request.Headers["DNT"].ToString() == "1";
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

    private string? GetGaHash(HttpContext context)
    {
        if (!_options.UseGaCookie)
            return null;

        if (context.Request.Cookies.TryGetValue("_ga", out var gaValue) && !string.IsNullOrEmpty(gaValue))
            return HashingService.Hash(gaValue);

        return null;
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

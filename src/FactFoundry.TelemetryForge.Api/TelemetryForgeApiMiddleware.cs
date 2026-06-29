using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FactFoundry.TelemetryForge.Api.Models;

namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// ASP.NET middleware that captures per-request API telemetry and posts a single
/// <see cref="ApiEventPayload"/> to the TelemetryForge Server for each matched HTTP request.
/// Stateless — one request maps to one event, independent of any session or circuit lifecycle.
/// Must be registered after <c>UseRouting</c> so the route template is available.
/// </summary>
public sealed class TelemetryForgeApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryClient _client;
    private readonly ApiTelemetryOptions _options;
    private readonly ILogger<TelemetryForgeApiMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryForgeApiMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="client">The telemetry client.</param>
    /// <param name="options">The API telemetry options.</param>
    /// <param name="logger">The logger.</param>
    public TelemetryForgeApiMiddleware(
        RequestDelegate next,
        ITelemetryClient client,
        IOptions<ApiTelemetryOptions> options,
        ILogger<TelemetryForgeApiMiddleware> logger)
    {
        _next = next;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Times the request, then reports a single API telemetry event if it matched a route
    /// and is not excluded by a configured path prefix.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExcluded(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var routeTemplate = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
        if (routeTemplate is null)
            return;

        try
        {
            var (country, _) = GeoHeaderResolver.Resolve(context, _options.GeoProvider);
            var outcome = context.Items.TryGetValue(HttpContextExtensions.OutcomeItemKey, out var o)
                ? o as string
                : null;

            var payload = new ApiEventPayload
            {
                RouteTemplate = routeTemplate,
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Timestamp = DateTimeOffset.UtcNow,
                Country = country,
                Outcome = outcome
            };

            await _client.SendAsync("/api/telemetry/api", payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture API telemetry for {Path}", context.Request.Path);
        }
    }

    private bool IsExcluded(PathString path)
    {
        var value = path.Value;
        if (value is null) return false;

        foreach (var prefix in _options.ExcludedPathPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

using Microsoft.AspNetCore.Http;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Captures request-scoped data from an <see cref="HttpContext"/> for telemetry payload construction.
/// </summary>
internal sealed class RequestContext
{
    /// <summary>
    /// Client IP address resolved from X-Forwarded-For or the connection.
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>
    /// Raw User-Agent header value.
    /// </summary>
    public string UserAgent { get; init; } = string.Empty;

    /// <summary>
    /// HTTP Referer header value, if present.
    /// </summary>
    public string? Referrer { get; init; }

    /// <summary>
    /// Accept-Language header value.
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// Sec-CH-UA client hint header value, if present.
    /// </summary>
    public string? SecChUa { get; init; }

    /// <summary>
    /// Sec-CH-UA-Mobile client hint header value, if present.
    /// </summary>
    public string? SecChUaMobile { get; init; }

    /// <summary>
    /// Sec-CH-UA-Platform client hint header value, if present.
    /// </summary>
    public string? SecChUaPlatform { get; init; }

    /// <summary>
    /// Visitor's country from CDN geolocation headers, if available.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Visitor's region from CDN geolocation headers, if available.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Google Analytics cookie value, if present and enabled.
    /// </summary>
    public string? GaValue { get; init; }

    /// <summary>
    /// Request path from the URL.
    /// </summary>
    public string PagePath { get; init; } = "/";

    /// <summary>
    /// Extracts request context from an <see cref="HttpContext"/>.
    /// </summary>
    public static RequestContext FromHttpContext(HttpContext context, WebTelemetryOptions options)
    {
        var (country, region) = GeoHeaderResolver.Resolve(context, options.GeoProvider);

        string? gaValue = null;
        if (options.UseGaCookie
            && context.Request.Cookies.TryGetValue("_ga", out var ga)
            && !string.IsNullOrEmpty(ga))
        {
            gaValue = ga;
        }

        return new RequestContext
        {
            IpAddress = GetClientIp(context),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Referrer = context.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
            Language = context.Request.Headers.AcceptLanguage.ToString(),
            SecChUa = context.Request.Headers["Sec-CH-UA"].ToString() is { Length: > 0 } ch ? ch : null,
            SecChUaMobile = context.Request.Headers["Sec-CH-UA-Mobile"].ToString() is { Length: > 0 } chm ? chm : null,
            SecChUaPlatform = context.Request.Headers["Sec-CH-UA-Platform"].ToString() is { Length: > 0 } chp ? chp : null,
            Country = country,
            Region = region,
            GaValue = gaValue,
            PagePath = context.Request.Path.Value ?? "/"
        };
    }

    internal static string GetClientIp(HttpContext context)
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

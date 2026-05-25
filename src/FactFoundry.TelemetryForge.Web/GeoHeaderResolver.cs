using Microsoft.AspNetCore.Http;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Resolves country and region from CDN/reverse proxy geolocation headers.
/// </summary>
internal static class GeoHeaderResolver
{
    public static (string? Country, string? Region) Resolve(HttpContext context, GeoProvider provider)
    {
        return provider switch
        {
            GeoProvider.Cloudflare => ReadCloudflare(context),
            GeoProvider.CloudFront => ReadCloudFront(context),
            GeoProvider.Vercel => ReadVercel(context),
            GeoProvider.Akamai => ReadAkamai(context),
            GeoProvider.None => (null, null),
            _ => AutoDetect(context)
        };
    }

    private static (string? Country, string? Region) AutoDetect(HttpContext context)
    {
        var result = ReadCloudflare(context);
        if (result.Country is not null) return result;

        result = ReadCloudFront(context);
        if (result.Country is not null) return result;

        result = ReadVercel(context);
        if (result.Country is not null) return result;

        result = ReadAkamai(context);
        if (result.Country is not null) return result;

        return (null, null);
    }

    private static (string? Country, string? Region) ReadCloudflare(HttpContext context)
    {
        var country = HeaderValue(context, "CF-IPCountry");
        var region = HeaderValue(context, "CF-Region");
        return (country, region);
    }

    private static (string? Country, string? Region) ReadCloudFront(HttpContext context)
    {
        var country = HeaderValue(context, "CloudFront-Viewer-Country");
        var region = HeaderValue(context, "CloudFront-Viewer-Country-Region");
        return (country, region);
    }

    private static (string? Country, string? Region) ReadVercel(HttpContext context)
    {
        var country = HeaderValue(context, "x-vercel-ip-country");
        var region = HeaderValue(context, "x-vercel-ip-country-region");
        return (country, region);
    }

    private static (string? Country, string? Region) ReadAkamai(HttpContext context)
    {
        var edgescape = HeaderValue(context, "X-Akamai-Edgescape");
        if (edgescape is null) return (null, null);

        string? country = null;
        string? region = null;

        foreach (var pair in edgescape.Split(',', StringSplitOptions.TrimEntries))
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex < 0) continue;

            var key = pair[..eqIndex];
            var value = pair[(eqIndex + 1)..];

            if (key.Equals("country_code", StringComparison.OrdinalIgnoreCase))
                country = value;
            else if (key.Equals("region_code", StringComparison.OrdinalIgnoreCase))
                region = value;
        }

        return (country, region);
    }

    private static string? HeaderValue(HttpContext context, string header)
    {
        return context.Request.Headers[header].ToString() is { Length: > 0 } v ? v : null;
    }
}

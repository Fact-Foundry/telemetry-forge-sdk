using System.Collections.Concurrent;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Singleton cache that bridges <see cref="RequestContext"/> from the middleware to the circuit handler.
/// The middleware and circuit handler run in different DI scopes (HTTP request vs Blazor circuit),
/// so a scoped service cannot carry data between them. This cache uses client IP + User-Agent as
/// the lookup key with a short TTL.
/// </summary>
internal sealed class RequestContextAccessor
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    /// Stores a <see cref="RequestContext"/> for later retrieval by the circuit handler.
    /// </summary>
    public void Store(string key, RequestContext context)
    {
        _cache[key] = new CacheEntry(context, DateTime.UtcNow.AddSeconds(30));

        if (_cache.Count > 100)
            Cleanup();
    }

    /// <summary>
    /// Retrieves a cached <see cref="RequestContext"/> if it exists and hasn't expired.
    /// </summary>
    public RequestContext? TryGet(string key)
    {
        if (_cache.TryRemove(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return entry.Context;

        return null;
    }

    /// <summary>
    /// Builds a cache key from client IP and User-Agent.
    /// </summary>
    public static string BuildKey(string ip, string userAgent) => $"{ip}|{userAgent}";

    private void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _cache.Keys)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt <= now)
                _cache.TryRemove(key, out _);
        }
    }

    private sealed record CacheEntry(RequestContext Context, DateTime ExpiresAt);
}

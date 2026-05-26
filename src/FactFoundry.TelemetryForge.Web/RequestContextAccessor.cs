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
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();

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
    /// Stores a session ID for sharing between middleware and circuit handler.
    /// </summary>
    public void StoreSessionId(string key, string sessionId)
    {
        _sessions[key] = new SessionEntry(sessionId, DateTime.UtcNow.AddMinutes(30));
    }

    /// <summary>
    /// Retrieves the session ID for a given key, if it exists and hasn't expired.
    /// </summary>
    public string? GetSessionId(string key)
    {
        if (_sessions.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return entry.SessionId;

        return null;
    }

    /// <summary>
    /// Removes the session ID when the circuit closes.
    /// </summary>
    public void RemoveSessionId(string key)
    {
        _sessions.TryRemove(key, out _);
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

        foreach (var key in _sessions.Keys)
        {
            if (_sessions.TryGetValue(key, out var sessionEntry) && sessionEntry.ExpiresAt <= now)
                _sessions.TryRemove(key, out _);
        }
    }

    private sealed record CacheEntry(RequestContext Context, DateTime ExpiresAt);
    private sealed record SessionEntry(string SessionId, DateTime ExpiresAt);
}

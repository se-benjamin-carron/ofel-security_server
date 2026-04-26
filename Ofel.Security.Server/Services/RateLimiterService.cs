using System.Collections.Concurrent;

namespace Ofel.Security.Server.Services;

public class RateLimiterService
{
    private readonly SecurityConfig _config;
    private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _buckets = new();

    public RateLimiterService(SecurityConfig config) => _config = config;

    /// <returns>true if the request is within the allowed rate; false if the limit is exceeded.</returns>
    public bool TryConsume(string key)
    {
        var now    = DateTime.UtcNow;
        var window = TimeSpan.FromSeconds(_config.RateLimitWindowSeconds);

        var updated = _buckets.AddOrUpdate(
            key,
            _ => (1, now),
            (_, existing) => now - existing.WindowStart > window
                ? (1, now)
                : (existing.Count + 1, existing.WindowStart));

        return updated.Count <= _config.RateLimitMaxRequests;
    }
}

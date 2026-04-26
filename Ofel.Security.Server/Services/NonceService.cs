using System.Collections.Concurrent;

namespace Ofel.Security.Server.Services;

public class NonceService
{
    private readonly SecurityConfig _config;
    private readonly ConcurrentDictionary<string, DateTime> _seen = new();

    public NonceService(SecurityConfig config) => _config = config;

    /// <returns>true if the nonce is fresh (first use); false if it is a replay.</returns>
    public bool TryConsume(string nonce)
    {
        var expiry = DateTime.UtcNow.AddSeconds(_config.NonceWindowSeconds);
        if (!_seen.TryAdd(nonce, expiry))
            return false;

        // Lazy eviction of expired nonces.
        var now = DateTime.UtcNow;
        foreach (var kv in _seen)
            if (kv.Value < now) _seen.TryRemove(kv.Key, out _);

        return true;
    }
}

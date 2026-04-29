using Ofel.Security.Server.Models;
using Ofel.Security.Server.Services;

namespace Ofel.Security.Server.Endpoints;

public static class VerifyEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/verify", (
            VerifyRequest   req,
            SecurityConfig  config,
            RateLimiterService rateLimiter,
            NonceService    nonceService,
            BlacklistService blacklist,
            WhitelistService whitelist) =>
        {
            // 0. Whitelist — bypass all checks for trusted machines.
            if (whitelist.IsWhitelisted(req.MachineId))
                return Results.Ok(new { authorized = true });

            // 1. Timestamp tolerance — prevents replay of old requests.
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Math.Abs(nowMs - req.Timestamp) > config.TimestampToleranceMs)
                return Results.Ok(new { authorized = false, reason = "timestamp_expired" });

            // 2. Nonce uniqueness — prevents replay of the exact same request.
            if (!nonceService.TryConsume(req.Nonce))
                return Results.Ok(new { authorized = false, reason = "nonce_replayed" });

            // 3. Rate limiting — per machine ID.
            if (!rateLimiter.TryConsume(req.MachineId))
            {
                Console.WriteLine($"[RateLimit] Blocked {req.MachineId}");
                return Results.Ok(new { authorized = false, reason = "rate_limited" });
            }

            // 4. Blacklist check.
            if (blacklist.IsBlacklisted(req.MachineId))
                return Results.Ok(new { authorized = false, reason = "blacklisted" });

            // 5. Hash validation — SHA-256 of the license key, compared case-insensitively.
            bool hashOk = string.Equals(req.Hash, config.PasswordHash, StringComparison.OrdinalIgnoreCase);
            if (!hashOk)
            {
                Console.WriteLine($"[Verify] Invalid hash from {req.MachineId}");
                return Results.Ok(new { authorized = false, reason = "invalid_hash" });
            }

            return Results.Ok(new { authorized = true });
        });
    }
}

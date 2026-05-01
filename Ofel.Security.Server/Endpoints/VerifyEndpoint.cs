using Ofel.Security.Server.Models;
using Ofel.Security.Server.Services;

namespace Ofel.Security.Server.Endpoints;

public static class VerifyEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/verify", (
            VerifyRequest      req,
            SecurityConfig     config,
            RateLimiterService rateLimiter,
            NonceService       nonceService,
            BlacklistService   blacklist,
            WhitelistService   whitelist,
            TrustedService     trusted,
            TrialService       trial) =>
        {
            // 0. Whitelist — debugger-enabled machines bypass all checks.
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

            // 6. Trusted check — permanent access, no expiry, no debugger.
            if (trusted.IsTrusted(req.MachineId))
                return Results.Ok(new { authorized = true });

            // 7. Trial check — machine must be in an active trial.
            //    If the machine has never been seen before, enroll it in a 10-day trial.
            if (trial.IsTrialExpired(req.MachineId))
            {
                Console.WriteLine($"[Trial] Expired trial for {req.MachineId}");
                return Results.Ok(new { authorized = false, reason = "trial_expired" });
            }

            if (!trial.IsInTrial(req.MachineId))
            {
                trial.Add(req.MachineId, req.Email ?? "");
                Console.WriteLine($"[Trial] Enrolled {req.MachineId} ({req.Email})");
            }

            return Results.Ok(new { authorized = true });
        });
    }
}

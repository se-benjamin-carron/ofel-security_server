using Ofel.Security.Server.Models;
using Ofel.Security.Server.Services;

namespace Ofel.Security.Server.Endpoints;

public static class AdminEndpoint
{
    public static void Map(WebApplication app)
    {
        // --- Health ---

        app.MapGet("/admin/health", (HttpContext ctx, SecurityConfig config, BlacklistService blacklist, WhitelistService whitelist, TrialService trial) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();

            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            Log("health", null, remoteIp);

            return Results.Ok(new
            {
                server_utc = DateTime.UtcNow.ToString("o"),
                blacklist  = FileStats(config.BlacklistPath, blacklist.Count),
                whitelist  = FileStats(config.WhitelistPath, whitelist.Count),
                trial      = FileStats(config.TrialPath,     trial.Count)
            });
        }).RequireCors("AdminCors");

        // --- Blacklist ---

        app.MapGet("/admin/blacklist", (HttpContext ctx, SecurityConfig config, BlacklistService blacklist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            Log("list-blacklist", null, remoteIp);
            return Results.Ok(blacklist.GetAll());
        }).RequireCors("AdminCors");

        app.MapPost("/admin/blacklist", (HttpContext ctx, AddBlacklistRequest req, SecurityConfig config, BlacklistService blacklist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!blacklist.Add(req.MachineId, req.Reason))
            {
                Log("add-blacklist-duplicate", req.MachineId, remoteIp);
                return Results.Conflict(new { added = false, error = "machine_id already blacklisted" });
            }
            Log("add-blacklist", req.MachineId, remoteIp);
            return Results.Ok(new { added = true });
        }).RequireCors("AdminCors");

        app.MapDelete("/admin/blacklist/{machineId}", (HttpContext ctx, string machineId, SecurityConfig config, BlacklistService blacklist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var removed = blacklist.Remove(machineId);
            Log(removed ? "delete-blacklist" : "delete-blacklist-notfound", machineId, remoteIp);
            return removed
                ? Results.Ok(new { removed = true })
                : Results.NotFound(new { removed = false });
        }).RequireCors("AdminCors");

        // --- Whitelist (trusted) ---

        app.MapGet("/admin/whitelist", (HttpContext ctx, SecurityConfig config, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            Log("list-whitelist", null, remoteIp);
            return Results.Ok(whitelist.GetAll());
        }).RequireCors("AdminCors");

        app.MapPost("/admin/whitelist", (HttpContext ctx, AddWhitelistRequest req, SecurityConfig config, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!whitelist.Add(req.MachineId, req.Reason))
            {
                Log("add-whitelist-duplicate", req.MachineId, remoteIp);
                return Results.Conflict(new { added = false, error = "machine_id already whitelisted" });
            }
            Log("add-whitelist", req.MachineId, remoteIp);
            return Results.Ok(new { added = true });
        }).RequireCors("AdminCors");

        app.MapDelete("/admin/whitelist/{machineId}", (HttpContext ctx, string machineId, SecurityConfig config, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var removed = whitelist.Remove(machineId);
            Log(removed ? "delete-whitelist" : "delete-whitelist-notfound", machineId, remoteIp);
            return removed
                ? Results.Ok(new { removed = true })
                : Results.NotFound(new { removed = false });
        }).RequireCors("AdminCors");

        // --- Trial ---

        app.MapGet("/admin/trial", (HttpContext ctx, SecurityConfig config, TrialService trial) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            Log("list-trial", null, remoteIp);
            return Results.Ok(trial.GetAll());
        }).RequireCors("AdminCors");

        app.MapPost("/admin/trial", (HttpContext ctx, AddTrialRequest req, SecurityConfig config, TrialService trial) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!trial.Add(req.MachineId, req.Email))
            {
                Log("add-trial-duplicate", req.MachineId, remoteIp);
                return Results.Conflict(new { added = false, error = "machine_id already in trial" });
            }
            Log("add-trial", req.MachineId, remoteIp);
            return Results.Ok(new { added = true });
        }).RequireCors("AdminCors");

        app.MapDelete("/admin/trial/{machineId}", (HttpContext ctx, string machineId, SecurityConfig config, TrialService trial) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var removed = trial.Remove(machineId);
            Log(removed ? "delete-trial" : "delete-trial-notfound", machineId, remoteIp);
            return removed
                ? Results.Ok(new { removed = true })
                : Results.NotFound(new { removed = false });
        }).RequireCors("AdminCors");

        // Promote a trial user to the whitelist (trusted).
        app.MapPost("/admin/trial/{machineId}/promote", (HttpContext ctx, string machineId, SecurityConfig config, TrialService trial, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (!trial.IsInTrial(machineId))
            {
                Log("promote-trial-notfound", machineId, remoteIp);
                return Results.NotFound(new { promoted = false, error = "machine_id not in trial" });
            }

            var email = trial.GetEmail(machineId) ?? "";
            trial.Remove(machineId);
            whitelist.Add(machineId, string.IsNullOrEmpty(email) ? "promoted_from_trial" : $"promoted_from_trial:{email}");

            Log("promote-trial", machineId, remoteIp);
            return Results.Ok(new { promoted = true });
        }).RequireCors("AdminCors");
    }

    private static bool IsAuthorized(HttpContext ctx, SecurityConfig config)
    {
        if (string.IsNullOrEmpty(config.AdminKey)) return false;
        var headerValue = ctx.Request.Headers["X-Admin-Key"].FirstOrDefault();
        return string.Equals(headerValue, config.AdminKey, StringComparison.Ordinal);
    }

    private static void Log(string action, string? machineId, string ip)
    {
        var idPart = machineId is not null ? $" | machine_id={machineId}" : "";
        Console.WriteLine($"[Admin] {action}{idPart} | ip={ip} | ts={DateTime.UtcNow:o}");
    }

    private static object FileStats(string path, int count)
    {
        if (!File.Exists(path))
            return new { path, exists = false, file_size_bytes = 0, line_count = count, last_modified = (string?)null };

        var fi = new FileInfo(path);
        return new
        {
            path,
            exists          = true,
            file_size_bytes = fi.Length,
            line_count      = count,
            last_modified   = fi.LastWriteTimeUtc.ToString("o")
        };
    }
}

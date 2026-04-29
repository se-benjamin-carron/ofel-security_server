using Ofel.Security.Server.Models;
using Ofel.Security.Server.Services;

namespace Ofel.Security.Server.Endpoints;

public static class AdminEndpoint
{
    public static void Map(WebApplication app)
    {
        // --- Blacklist ---

        app.MapGet("/admin/blacklist", (HttpContext ctx, SecurityConfig config, BlacklistService blacklist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            return Results.Ok(blacklist.GetAll());
        });

        app.MapPost("/admin/blacklist", (HttpContext ctx, AddBlacklistRequest req, SecurityConfig config, BlacklistService blacklist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            blacklist.Add(req.MachineId, req.Reason);
            return Results.Ok(new { added = true });
        });

        app.MapDelete("/admin/blacklist/{machineId}", (HttpContext ctx, string machineId, SecurityConfig config, BlacklistService blacklist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            return blacklist.Remove(machineId)
                ? Results.Ok(new { removed = true })
                : Results.NotFound(new { removed = false });
        });

        // --- Whitelist ---

        app.MapGet("/admin/whitelist", (HttpContext ctx, SecurityConfig config, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            return Results.Ok(whitelist.GetAll());
        });

        app.MapPost("/admin/whitelist", (HttpContext ctx, AddWhitelistRequest req, SecurityConfig config, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            whitelist.Add(req.MachineId, req.Reason);
            return Results.Ok(new { added = true });
        });

        app.MapDelete("/admin/whitelist/{machineId}", (HttpContext ctx, string machineId, SecurityConfig config, WhitelistService whitelist) =>
        {
            if (!IsAuthorized(ctx, config)) return Results.Unauthorized();
            return whitelist.Remove(machineId)
                ? Results.Ok(new { removed = true })
                : Results.NotFound(new { removed = false });
        });
    }

    private static bool IsAuthorized(HttpContext ctx, SecurityConfig config)
    {
        if (string.IsNullOrEmpty(config.AdminKey)) return false;
        var headerValue = ctx.Request.Headers["X-Admin-Key"].FirstOrDefault();
        return string.Equals(headerValue, config.AdminKey, StringComparison.Ordinal);
    }
}

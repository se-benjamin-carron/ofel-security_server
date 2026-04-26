using Ofel.Security.Server.Models;
using Ofel.Security.Server.Services;

namespace Ofel.Security.Server.Endpoints;

public static class AlertEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/alert", (
            AlertRequest     req,
            BlacklistService blacklist,
            EmailAlertService email) =>
        {
            Console.WriteLine(
                $"[ALERT] Type: {req.Type} | Machine: {req.MachineId} | " +
                $"Version: {req.Version} | Details: {req.Details ?? "none"}");

            string subject = $"[OFEL ALERT] Tamper detected — {req.Type}";
            string body    =
                $"Machine ID : {req.MachineId}\n" +
                $"Type       : {req.Type}\n" +
                $"Version    : {req.Version}\n" +
                $"Timestamp  : {DateTimeOffset.FromUnixTimeMilliseconds(req.Timestamp):O}\n" +
                $"Details    : {req.Details ?? "none"}\n";

            // Auto-blacklist immediately (synchronous, in-memory + file).
            blacklist.Add(req.MachineId, req.Type);

            // Email is fire-and-forget — never block the response on SMTP.
            _ = Task.Run(() => email.Send(subject, body));

            return Results.Ok(new { received = true });
        });
    }
}

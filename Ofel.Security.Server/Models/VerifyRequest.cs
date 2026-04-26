namespace Ofel.Security.Server.Models;

public record VerifyRequest(
    string Hash,
    string MachineId,
    string Nonce,
    long   Timestamp);

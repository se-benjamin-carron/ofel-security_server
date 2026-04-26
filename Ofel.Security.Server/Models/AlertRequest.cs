namespace Ofel.Security.Server.Models;

public record AlertRequest(
    string  MachineId,
    string  Type,
    string  Version,
    long    Timestamp,
    string? Details = null);

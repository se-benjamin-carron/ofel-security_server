using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Models;

public record AddTrustedRequest(
    [property: JsonPropertyName("machine_id")] string MachineId,
    [property: JsonPropertyName("email")]      string Email,
    [property: JsonPropertyName("reason")]     string Reason);

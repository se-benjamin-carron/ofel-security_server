using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Models;

public record AddTrialRequest(
    [property: JsonPropertyName("machine_id")] string MachineId,
    [property: JsonPropertyName("email")]      string Email);

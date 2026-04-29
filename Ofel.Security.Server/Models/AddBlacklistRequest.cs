using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Models;

public record AddBlacklistRequest(
    [property: JsonPropertyName("machine_id")] string MachineId,
    [property: JsonPropertyName("reason")]     string Reason);

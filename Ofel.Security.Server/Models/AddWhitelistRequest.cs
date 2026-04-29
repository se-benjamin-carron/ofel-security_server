using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Models;

public record AddWhitelistRequest(
    [property: JsonPropertyName("machine_id")] string MachineId,
    [property: JsonPropertyName("reason")]     string Reason);

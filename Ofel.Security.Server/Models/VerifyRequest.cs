using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Models;

public record VerifyRequest(
    [property: JsonPropertyName("hash")]       string Hash,
    [property: JsonPropertyName("machine_id")] string MachineId,
    [property: JsonPropertyName("nonce")]      string Nonce,
    [property: JsonPropertyName("timestamp")]  long   Timestamp);

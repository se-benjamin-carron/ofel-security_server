using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Models;

public record AlertRequest(
    [property: JsonPropertyName("machine_id")] string  MachineId,
    [property: JsonPropertyName("type")]       string  Type,
    [property: JsonPropertyName("version")]    string  Version,
    [property: JsonPropertyName("timestamp")]  long    Timestamp,
    [property: JsonPropertyName("details")]    string? Details = null);

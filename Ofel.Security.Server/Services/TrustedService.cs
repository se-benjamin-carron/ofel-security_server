using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Services;

public class TrustedService
{
    private readonly string _path;
    private readonly ConcurrentDictionary<string, (string Email, string Reason, string AddedDate)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public TrustedService(SecurityConfig config)
    {
        _path = config.TrustedPath;
        Load();
        Console.WriteLine($"[Trusted] Loaded {_cache.Count} entries from {_path}");
    }

    public bool IsTrusted(string machineId) => _cache.ContainsKey(machineId);

    public bool Add(string machineId, string email, string reason)
    {
        var addedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (!_cache.TryAdd(machineId, (email, reason, addedDate))) return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            if (!File.Exists(_path))
                File.WriteAllText(_path, "machine_id,email,added_date,reason\n");

            File.AppendAllText(_path, $"{machineId},{email},{addedDate},{reason}\n");
            Console.WriteLine($"[Trusted] Added {machineId} ({email}) — {reason}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trusted] Failed to persist {machineId}: {ex.Message}");
        }

        return true;
    }

    public bool Remove(string machineId)
    {
        if (!_cache.TryRemove(machineId, out _)) return false;

        try
        {
            Rewrite();
            Console.WriteLine($"[Trusted] Removed {machineId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trusted] Failed to rewrite after removing {machineId}: {ex.Message}");
        }

        return true;
    }

    public int Count => _cache.Count;

    public IEnumerable<object> GetAll() =>
        _cache.Select(kv => (object)new TrustedEntry(kv.Key, kv.Value.Email, kv.Value.AddedDate, kv.Value.Reason));

    private void Load()
    {
        if (!File.Exists(_path)) return;
        foreach (var line in File.ReadAllLines(_path).Skip(1))
        {
            var parts = line.Split(',', 4);
            if (parts.Length < 1) continue;
            var id = parts[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;
            var email     = parts.Length >= 2 ? parts[1].Trim() : "";
            var addedDate = parts.Length >= 3 ? parts[2].Trim() : "";
            var reason    = parts.Length >= 4 ? parts[3].Trim() : "";
            _cache[id] = (email, reason, addedDate);
        }
    }

    private void Rewrite()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var lines = new List<string> { "machine_id,email,added_date,reason" };
        lines.AddRange(_cache.Select(kv => $"{kv.Key},{kv.Value.Email},{kv.Value.AddedDate},{kv.Value.Reason}"));
        File.WriteAllLines(_path, lines);
    }

    private record TrustedEntry(
        [property: JsonPropertyName("machine_id")] string MachineId,
        [property: JsonPropertyName("email")]      string Email,
        [property: JsonPropertyName("added_date")] string AddedDate,
        [property: JsonPropertyName("reason")]     string Reason);
}

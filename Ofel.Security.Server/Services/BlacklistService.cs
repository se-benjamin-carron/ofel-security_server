using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Services;

public class BlacklistService
{
    private readonly string _path;
    private readonly ConcurrentDictionary<string, (string Reason, string AddedDate)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public BlacklistService(SecurityConfig config)
    {
        _path = config.BlacklistPath;
        Load();
        Console.WriteLine($"[Blacklist] Loaded {_cache.Count} entries from {_path}");
    }

    public bool IsBlacklisted(string machineId) => _cache.ContainsKey(machineId);

    public bool Add(string machineId, string reason)
    {
        var addedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (!_cache.TryAdd(machineId, (reason, addedDate))) return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // Write header on first creation.
            if (!File.Exists(_path))
                File.WriteAllText(_path, "machine_id,added_date,reason\n");

            File.AppendAllText(_path, $"{machineId},{addedDate},{reason}\n");
            Console.WriteLine($"[Blacklist] Added {machineId} ({reason})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Blacklist] Failed to persist {machineId}: {ex.Message}");
        }

        return true;
    }

    public bool Remove(string machineId)
    {
        if (!_cache.TryRemove(machineId, out _)) return false;

        try
        {
            Rewrite();
            Console.WriteLine($"[Blacklist] Removed {machineId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Blacklist] Failed to rewrite after removing {machineId}: {ex.Message}");
        }

        return true;
    }

    public int Count => _cache.Count;

    public IEnumerable<object> GetAll() =>
        _cache.Select(kv => (object)new BlacklistEntry(kv.Key, kv.Value.AddedDate, kv.Value.Reason));

    private void Load()
    {
        if (!File.Exists(_path)) return;
        foreach (var line in File.ReadAllLines(_path).Skip(1)) // skip header
        {
            var parts = line.Split(',', 3);
            if (parts.Length < 1) continue;
            var id = parts[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;
            var addedDate = parts.Length >= 2 ? parts[1].Trim() : "";
            var reason    = parts.Length >= 3 ? parts[2].Trim() : "";
            _cache[id] = (reason, addedDate);
        }
    }

    private void Rewrite()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var lines = new List<string> { "machine_id,added_date,reason" };
        lines.AddRange(_cache.Select(kv => $"{kv.Key},{kv.Value.AddedDate},{kv.Value.Reason}"));
        File.WriteAllLines(_path, lines);
    }

    private record BlacklistEntry(
        [property: JsonPropertyName("machine_id")] string MachineId,
        [property: JsonPropertyName("added_date")] string AddedDate,
        [property: JsonPropertyName("reason")]     string Reason);
}

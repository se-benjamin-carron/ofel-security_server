using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Ofel.Security.Server.Services;

public class TrialService
{
    private const int TrialDays = 10;

    private readonly string _path;
    private readonly ConcurrentDictionary<string, (string Email, DateTime AddedDate, DateTime ExpiryDate)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public TrialService(SecurityConfig config)
    {
        _path = config.TrialPath;
        Load();
        Console.WriteLine($"[Trial] Loaded {_cache.Count} entries from {_path}");
    }

    public bool IsInTrial(string machineId)    => _cache.ContainsKey(machineId);
    public bool IsTrialActive(string machineId) => _cache.TryGetValue(machineId, out var e) && DateTime.UtcNow <= e.ExpiryDate;
    public bool IsTrialExpired(string machineId) => _cache.TryGetValue(machineId, out var e) && DateTime.UtcNow > e.ExpiryDate;

    public bool Add(string machineId, string email)
    {
        var addedDate  = DateTime.UtcNow;
        var expiryDate = addedDate.AddDays(TrialDays);

        if (!_cache.TryAdd(machineId, (email, addedDate, expiryDate))) return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            if (!File.Exists(_path))
                File.WriteAllText(_path, "machine_id,email,added_date,expiry_date\n");

            File.AppendAllText(_path, $"{machineId},{email},{addedDate:yyyy-MM-dd},{expiryDate:yyyy-MM-dd}\n");
            Console.WriteLine($"[Trial] Added {machineId} ({email}) — expires {expiryDate:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trial] Failed to persist {machineId}: {ex.Message}");
        }

        return true;
    }

    public bool Remove(string machineId)
    {
        if (!_cache.TryRemove(machineId, out _)) return false;

        try
        {
            Rewrite();
            Console.WriteLine($"[Trial] Removed {machineId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trial] Failed to rewrite after removing {machineId}: {ex.Message}");
        }

        return true;
    }

    public string? GetEmail(string machineId) =>
        _cache.TryGetValue(machineId, out var e) ? e.Email : null;

    public int Count => _cache.Count;

    public IEnumerable<object> GetAll() =>
        _cache.Select(kv => (object)new TrialEntry(
            kv.Key,
            kv.Value.Email,
            kv.Value.AddedDate.ToString("yyyy-MM-dd"),
            kv.Value.ExpiryDate.ToString("yyyy-MM-dd"),
            DateTime.UtcNow <= kv.Value.ExpiryDate));

    private void Load()
    {
        if (!File.Exists(_path)) return;
        foreach (var line in File.ReadAllLines(_path).Skip(1))
        {
            var parts = line.Split(',', 4);
            if (parts.Length < 1) continue;
            var id = parts[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;
            var email      = parts.Length >= 2 ? parts[1].Trim() : "";
            var addedDate  = parts.Length >= 3 && DateTime.TryParse(parts[2].Trim(), out var ad) ? ad : DateTime.UtcNow;
            var expiryDate = parts.Length >= 4 && DateTime.TryParse(parts[3].Trim(), out var ed) ? ed : addedDate.AddDays(TrialDays);
            _cache[id] = (email, addedDate, expiryDate);
        }
    }

    private void Rewrite()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var lines = new List<string> { "machine_id,email,added_date,expiry_date" };
        lines.AddRange(_cache.Select(kv =>
            $"{kv.Key},{kv.Value.Email},{kv.Value.AddedDate:yyyy-MM-dd},{kv.Value.ExpiryDate:yyyy-MM-dd}"));
        File.WriteAllLines(_path, lines);
    }

    private record TrialEntry(
        [property: JsonPropertyName("machine_id")]  string MachineId,
        [property: JsonPropertyName("email")]       string Email,
        [property: JsonPropertyName("added_date")]  string AddedDate,
        [property: JsonPropertyName("expiry_date")] string ExpiryDate,
        [property: JsonPropertyName("active")]      bool   Active);
}

using System.Collections.Concurrent;

namespace Ofel.Security.Server.Services;

public class BlacklistService
{
    private readonly string _path;
    private readonly ConcurrentDictionary<string, bool> _cache = new(StringComparer.OrdinalIgnoreCase);

    public BlacklistService(SecurityConfig config)
    {
        _path = config.BlacklistPath;
        Load();
        Console.WriteLine($"[Blacklist] Loaded {_cache.Count} entries from {_path}");
    }

    public bool IsBlacklisted(string machineId) => _cache.ContainsKey(machineId);

    public void Add(string machineId, string reason)
    {
        if (!_cache.TryAdd(machineId, true)) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // Write header on first creation.
            if (!File.Exists(_path))
                File.WriteAllText(_path, "machine_id,added_date,reason\n");

            File.AppendAllText(_path, $"{machineId},{DateTime.UtcNow:yyyy-MM-dd},{reason}\n");
            Console.WriteLine($"[Blacklist] Added {machineId} ({reason})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Blacklist] Failed to persist {machineId}: {ex.Message}");
        }
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        foreach (var line in File.ReadAllLines(_path).Skip(1)) // skip header
        {
            var id = line.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(id))
                _cache[id] = true;
        }
    }
}

using System.Text.Json;
using ParadowSync.Core.Models;

namespace ParadowSync.Core.Services;

public sealed class ProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _directory;

    public ProfileStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "paradow-sync", "profiles");
        Directory.CreateDirectory(_directory);
    }

    public async Task<IReadOnlyList<Profile>> ListAsync(CancellationToken ct = default)
    {
        var profiles = new List<Profile>();
        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file, ct);
            var profile = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
            if (profile is not null)
                profiles.Add(profile);
        }

        return profiles;
    }

    public async Task<Profile?> GetAsync(string id, CancellationToken ct = default)
    {
        var path = Path.Combine(_directory, $"{id}.json");
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<Profile>(json, JsonOptions);
    }

    public async Task SaveAsync(Profile profile, CancellationToken ct = default)
    {
        var path = Path.Combine(_directory, $"{profile.Id}.json");
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var path = Path.Combine(_directory, $"{id}.json");
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}

namespace ParadowSync.Core.Catalog;

public sealed class IconLoader
{
    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IconLoader(string? iconsDirectory = null)
    {
        var directory = iconsDirectory ?? Path.Combine(AppContext.BaseDirectory, "icons");
        if (!Directory.Exists(directory))
            return;

        foreach (var classInfo in ClassCatalog.All)
        {
            var fileName = Path.GetFileName(classInfo.IconPath);
            var filePath = Path.Combine(directory, fileName);
            if (!File.Exists(filePath))
                continue;

            _cache[classInfo.Name] = File.ReadAllBytes(filePath);
        }
    }

    public int LoadedIconCount => _cache.Count;

    public byte[]? GetIconBytes(string className) =>
        _cache.TryGetValue(className, out var bytes) ? bytes : null;
}

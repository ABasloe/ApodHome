using System.Text.Json;

namespace ApodHome.Core;

public sealed class CacheStore
{
    private readonly string _root;
    public CacheStore(string root) { _root = root; Directory.CreateDirectory(root); }
    public string CurrentImagePath => Path.Combine(_root, "current.jpg");
    public string MetadataPath => Path.Combine(_root, "current.json");

    public static CacheStore ForDesktop() => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApodHome", "desktop"));

    public async Task<CachedApod?> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(MetadataPath)) return null;
        await using var stream = File.OpenRead(MetadataPath);
        var cached = await JsonSerializer.DeserializeAsync<CachedApod>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return cached is null ? null : cached with { ImagePath = CurrentImagePath };
    }

    public async Task WriteAsync(CachedApod metadata, CancellationToken cancellationToken = default)
    {
        var temporary = MetadataPath + ".tmp";
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
        File.Move(temporary, MetadataPath, true);
        PruneHistory();
    }

    public void ArchiveCurrent()
    {
        if (!File.Exists(CurrentImagePath)) return;
        var archive = Path.Combine(_root, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}.jpg");
        File.Copy(CurrentImagePath, archive, true);
    }

    private void PruneHistory()
    {
        foreach (var file in new DirectoryInfo(_root).GetFiles("*.jpg").Where(x => x.Name != "current.jpg").OrderByDescending(x => x.LastWriteTimeUtc).Skip(7))
            file.Delete();
    }
}

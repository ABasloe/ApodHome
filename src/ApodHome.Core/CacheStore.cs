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

    // Windows requires a different filename when a lock-screen image changes.
    // Retaining a dated, immutable copy also ensures the shell never reads a file
    // while the desktop updater is replacing it.
    public string GetOrCreateLockScreenImage(DateOnly date)
    {
        if (!File.Exists(CurrentImagePath)) throw new FileNotFoundException("Current APOD image was not found.", CurrentImagePath);
        var lockImage = Path.Combine(_root, $"lock-{date:yyyyMMdd}.jpg");
        if (!File.Exists(lockImage))
        {
            File.Copy(CurrentImagePath, lockImage);
        }
        else if (!File.ReadAllBytes(CurrentImagePath).AsSpan().SequenceEqual(File.ReadAllBytes(lockImage)))
        {
            // Windows can retain an open handle to a lock-screen image. Keep
            // both files immutable and switch to a fresh filename instead of
            // overwriting the file Windows may still be displaying.
            lockImage = Path.Combine(_root, $"lock-{date:yyyyMMdd}-{DateTime.UtcNow:HHmmssfff}.jpg");
            File.Copy(CurrentImagePath, lockImage);
        }
        PruneHistory();
        return lockImage;
    }

    private void PruneHistory()
    {
        var images = new DirectoryInfo(_root).GetFiles("*.jpg");
        foreach (var file in images.Where(x => x.Name != "current.jpg" && !x.Name.StartsWith("lock-", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.LastWriteTimeUtc).Skip(7))
            file.Delete();
        // Keep the active lock-screen source but do not accumulate duplicate copies.
        foreach (var file in images.Where(x => x.Name.StartsWith("lock-", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.LastWriteTimeUtc).Skip(1))
            file.Delete();
    }
}

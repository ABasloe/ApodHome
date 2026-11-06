using ApodHome.Core;
using System.IO;
using Xunit;

namespace ApodHome.Core.Tests;

public sealed class CacheStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ApodHomeTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Metadata_round_trips_without_losing_the_image_path()
    {
        var store = new CacheStore(_directory);
        var expected = new CachedApod(new DateOnly(2026, 11, 5), "Enceladus", "https://example.test/apod", store.CurrentImagePath, DateTimeOffset.UtcNow);
        await store.WriteAsync(expected);

        var actual = await store.ReadAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArchiveCurrent_keeps_a_copy_of_the_last_good_image()
    {
        var store = new CacheStore(_directory);
        File.WriteAllBytes(store.CurrentImagePath, [1, 2, 3]);

        store.ArchiveCurrent();

        Assert.Single(Directory.GetFiles(_directory, "*.jpg"), x => Path.GetFileName(x) != "current.jpg");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}

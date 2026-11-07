using System.Net.Http.Headers;
using System.Net.Http;

namespace ApodHome.Core;

public sealed class UpdateCoordinator
{
    private const int CurrentImageQualityRevision = 2;
    private readonly IApodProvider _provider;
    private readonly HttpClient _http;
    private readonly ImageNormalizer _normalizer;
    private readonly IWallpaperService _wallpaper;
    private readonly ILockScreenService _lockScreen;

    public UpdateCoordinator(IApodProvider provider, HttpClient http, ImageNormalizer normalizer, IWallpaperService wallpaper, ILockScreenService lockScreen)
    {
        _provider = provider;
        _http = http;
        _normalizer = normalizer;
        _wallpaper = wallpaper;
        _lockScreen = lockScreen;
    }

    public async Task<UpdateResult> UpdateAsync(UpdateTarget target, CancellationToken cancellationToken = default)
    {
        if (target != UpdateTarget.Desktop) throw new ArgumentOutOfRangeException(nameof(target));
        var desktop = await UpdateCacheAsync(CacheStore.ForDesktop(), cancellationToken).ConfigureAwait(false);
        if (!File.Exists(desktop.ImagePath)) throw new InvalidDataException("Desktop APOD cache was not created.");
        _wallpaper.SetFillWallpaper(desktop.ImagePath);
        try
        {
            var lockImage = CacheStore.ForDesktop().GetOrCreateLockScreenImage(desktop.Date);
            if (!await _lockScreen.TrySetAsync(lockImage, cancellationToken).ConfigureAwait(false))
                return new UpdateResult(true, desktop, "Desktop updated, but Windows did not accept the lock-screen image.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A lock-screen refusal must not replace a good desktop update with a failed task.
            return new UpdateResult(true, desktop, "Desktop updated, but the lock-screen image could not be changed.");
        }
        return new UpdateResult(true, desktop);
    }

    private async Task<CachedApod> UpdateCacheAsync(CacheStore cache, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var existing = await cache.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (existing?.Date == today && existing.ImageQualityRevision >= CurrentImageQualityRevision &&
            File.Exists(cache.CurrentImagePath) && IsSpecificApodPage(existing.PageUrl))
            return existing with { ImagePath = cache.CurrentImagePath };

        var apod = await _provider.GetAsync(today, cancellationToken).ConfigureAwait(false);
        if (apod.Date != today) throw new InvalidDataException("NASA returned an APOD for a different date.");

        using var response = await SafeHttp.GetAsync(_http, apod.MediaUrl, SafeHttp.IsHttps, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentType?.MediaType is not { } mediaType || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("APOD media did not identify itself as an image.");
        if (response.Content.Headers.ContentLength is > SafeHttp.MaxImageBytes)
            throw new InvalidDataException("APOD image is too large to use safely.");

        cache.ArchiveCurrent();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await _normalizer.SaveAsJpegAsync(stream, cache.CurrentImagePath, cancellationToken).ConfigureAwait(false);
        var saved = new CachedApod(apod.Date, apod.Title, apod.PageUrl.AbsoluteUri, cache.CurrentImagePath, DateTimeOffset.UtcNow, CurrentImageQualityRevision);
        await cache.WriteAsync(saved, cancellationToken).ConfigureAwait(false);
        return saved;
    }

    private static bool IsSpecificApodPage(string pageUrl) =>
        Uri.TryCreate(pageUrl, UriKind.Absolute, out var page) &&
        SafeHttp.IsNasaPage(page) &&
        !string.Equals(page.AbsolutePath.TrimEnd('/'), "/apod", StringComparison.OrdinalIgnoreCase);
}

namespace ApodHome.Core;

public enum ApodMediaType { Image, Video }
public enum UpdateTarget { Desktop }

public sealed record ApodItem(
    DateOnly Date,
    string Title,
    Uri PageUrl,
    Uri MediaUrl,
    ApodMediaType MediaType,
    string? Explanation);

public sealed record CachedApod(
    DateOnly Date,
    string Title,
    string PageUrl,
    string ImagePath,
    DateTimeOffset UpdatedAtUtc,
    int ImageQualityRevision = 0);

public sealed record UpdateResult(bool Updated, CachedApod? Item, string? Message = null);

public interface IApodProvider
{
    Task<ApodItem> GetAsync(DateOnly date, CancellationToken cancellationToken = default);
}

public interface IWallpaperService
{
    void SetFillWallpaper(string imagePath);
}

public interface ILockScreenService
{
    Task<bool> TrySetAsync(string imagePath, CancellationToken cancellationToken = default);
}

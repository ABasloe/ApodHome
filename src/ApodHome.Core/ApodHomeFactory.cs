namespace ApodHome.Core;

public static class ApodHomeFactory
{
    public static UpdateCoordinator CreateUpdater()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ApodHome/1.0");
        return new UpdateCoordinator(new NasaApodProvider(client), client, new ImageNormalizer(), new WindowsWallpaperService());
    }
}

using ApodHome.Core;

namespace ApodHome.Updater;

internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var target = args.FirstOrDefault()?.ToLowerInvariant() switch
            {
                "desktop" => UpdateTarget.Desktop,
                _ => UpdateTarget.Desktop
            };
            await ApodHomeFactory.CreateUpdater().UpdateAsync(target);
            return 0;
        }
        catch (Exception exception)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApodHome");
                Directory.CreateDirectory(folder);
                await File.AppendAllTextAsync(Path.Combine(folder, "updater.log"), $"{DateTimeOffset.UtcNow:O} {exception}\n");
            }
            catch { /* A silent scheduled task must not crash trying to log. */ }
            return 1;
        }
    }
}

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
            var result = await ApodHomeFactory.CreateUpdater().UpdateAsync(target);
            if (result.Message is not null) await WriteLogAsync("Warning: " + result.Message);
            return 0;
        }
        catch (Exception exception)
        {
            try
            {
                await WriteLogAsync(exception.ToString());
            }
            catch { /* A silent scheduled task must not crash trying to log. */ }
            return 1;
        }
    }

    private static async Task WriteLogAsync(string message)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApodHome");
        Directory.CreateDirectory(folder);
        await File.AppendAllTextAsync(Path.Combine(folder, "updater.log"), $"{DateTimeOffset.UtcNow:O} {message}\n");
    }
}

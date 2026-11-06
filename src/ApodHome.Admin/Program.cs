using ApodHome.Core;

namespace ApodHome.Admin;

internal static class Program
{
    [STAThread]
    private static Task<int> Main(string[] args)
    {
        try
        {
            var command = args.FirstOrDefault()?.ToLowerInvariant();
            var policy = new LockScreenPolicyManager();
            var updaterPath = Path.Combine(AppContext.BaseDirectory, "ApodHome.Updater.exe");
            switch (command)
            {
                case "install":
                    new TaskSchedulerManager().Install(updaterPath);
                    // Never download or decode remote media while elevated.
                    // The per-user task creates this image after sign-in.
                    policy.Configure(CacheStore.ForDesktop().CurrentImagePath);
                    return Task.FromResult(0);
                case "refresh-lock":
                    policy.Configure(CacheStore.ForDesktop().CurrentImagePath);
                    return Task.FromResult(0);
                case "uninstall":
                    new TaskSchedulerManager().Remove();
                    policy.Remove();
                    return Task.FromResult(0);
                default:
                    throw new ArgumentException("Expected install, refresh-lock, or uninstall.");
            }
        }
        catch (Exception exception)
        {
            WriteLog(exception);
            Console.Error.WriteLine(exception.Message);
            return Task.FromResult(1);
        }
    }

    private static void WriteLog(Exception exception)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ApodHome");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "admin.log"), $"{DateTimeOffset.UtcNow:O} {exception}\n");
        }
        catch { /* Preserve the primary installation error even if logging is unavailable. */ }
    }
}

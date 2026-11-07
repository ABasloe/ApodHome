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
                    // Version 1.0.0 used a machine policy that Windows Pro ignores.
                    // Remove only the policy this app previously owned; current versions
                    // use the Windows per-user lock-screen API without elevation.
                    policy.Remove();
                    return Task.FromResult(0);
                case "refresh-lock":
                    policy.Remove();
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

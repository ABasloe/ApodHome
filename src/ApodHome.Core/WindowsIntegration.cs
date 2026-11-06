using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace ApodHome.Core;

public sealed class WindowsWallpaperService : IWallpaperService
{
    private const uint SpiSetDesktopWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendWinIniChange = 0x02;

    public void SetFillWallpaper(string imagePath)
    {
        if (!File.Exists(imagePath)) throw new FileNotFoundException("Wallpaper image not found.", imagePath);
        using (var desktop = Registry.CurrentUser.CreateSubKey("Control Panel\\Desktop", writable: true))
        {
            desktop!.SetValue("WallpaperStyle", "10", RegistryValueKind.String);
            desktop.SetValue("TileWallpaper", "0", RegistryValueKind.String);
        }
        if (!SystemParametersInfo(SpiSetDesktopWallpaper, 0, imagePath, SpifUpdateIniFile | SpifSendWinIniChange))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows rejected the wallpaper update.");
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, string value, uint flags);
}

public sealed class LockScreenPolicyManager
{
    private const string PolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
    private const string AppPath = @"SOFTWARE\ApodHome";

    public void Configure(string imagePath)
    {
        var expectedPath = Path.GetFullPath(CacheStore.ForDesktop().CurrentImagePath);
        if (!string.Equals(Path.GetFullPath(imagePath), expectedPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("APOD Home will only configure its fixed per-user image path.");
        using var app = Registry.LocalMachine.CreateSubKey(AppPath, writable: true)!;
        using var policy = Registry.LocalMachine.CreateSubKey(PolicyPath, writable: true)!;
        var owned = IsOne(app.GetValue("OwnsLockScreenPolicy"));
        var existingValue = policy.GetValue("LockScreenImage", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        var existing = existingValue as string;
        if (!owned && existingValue is not null)
            throw new InvalidOperationException("The lock/sign-in image is already managed by another Windows policy. APOD Home will not override it.");
        if (owned && !string.Equals(existing, expectedPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The lock/sign-in policy changed after APOD Home was installed. Reinstall it only after reviewing the change.");
        if (!owned) policy.SetValue("LockScreenImage", expectedPath, RegistryValueKind.String);
        app.SetValue("OwnsLockScreenPolicy", 1, RegistryValueKind.DWord);
        app.SetValue("ManagedLockScreenPath", expectedPath, RegistryValueKind.String);
    }

    public void Remove()
    {
        using (var app = Registry.LocalMachine.OpenSubKey(AppPath, writable: true))
        {
            if (app is null || !IsOne(app.GetValue("OwnsLockScreenPolicy"))) return;
            var expectedPath = app.GetValue("ManagedLockScreenPath") as string;
            using var policy = Registry.LocalMachine.CreateSubKey(PolicyPath, writable: true)!;
            var current = policy.GetValue("LockScreenImage", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            if (string.Equals(current, expectedPath, StringComparison.OrdinalIgnoreCase))
                policy.DeleteValue("LockScreenImage", throwOnMissingValue: false);
        }
        Registry.LocalMachine.DeleteSubKeyTree(AppPath, throwOnMissingSubKey: false);
    }

    private static bool IsOne(object? value) => value is not null && Convert.ToInt32(value) == 1;
}

public sealed class TaskSchedulerManager
{
    private const string DesktopLogonTask = "APOD Home Desktop at Sign-in";
    private const string DesktopHourlyTask = "APOD Home Desktop Hourly";
    // Old versions created these SYSTEM tasks. Remove them during upgrade.
    private const string LockStartupTask = "APOD Home Lock Screen at Startup";
    private const string LockHourlyTask = "APOD Home Lock Screen Hourly";

    public void Install(string updaterPath)
    {
        var desktopCommand = Quote(updaterPath) + " desktop";
        var interactiveUser = WindowsIdentity.GetCurrent().Name;
        Run(allowFailure: true, "/Delete", "/TN", LockStartupTask, "/F");
        Run(allowFailure: true, "/Delete", "/TN", LockHourlyTask, "/F");
        Run("/Create", "/TN", DesktopLogonTask, "/TR", desktopCommand, "/SC", "ONLOGON", "/RU", interactiveUser, "/IT", "/RL", "LIMITED", "/F");
        Run("/Create", "/TN", DesktopHourlyTask, "/TR", desktopCommand, "/SC", "HOURLY", "/MO", "1", "/RU", interactiveUser, "/IT", "/RL", "LIMITED", "/F");
    }

    public void Remove()
    {
        foreach (var task in new[] { DesktopLogonTask, DesktopHourlyTask, LockStartupTask, LockHourlyTask })
            Run(allowFailure: true, "/Delete", "/TN", task, "/F");
    }

    private static void Run(params string[] args) => Run(false, args);
    private static void Run(bool allowFailure, params string[] args)
    {
        var processStart = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args) processStart.ArgumentList.Add(arg);
        using var process = Process.Start(processStart) ?? throw new InvalidOperationException("Could not start Task Scheduler.");
        process.WaitForExit();
        if (!allowFailure && process.ExitCode != 0) throw new InvalidOperationException("Task Scheduler did not accept the APOD Home task.");
    }
    private static string Quote(string value) => '"' + value.Replace("\"", "\\\"") + '"';
}

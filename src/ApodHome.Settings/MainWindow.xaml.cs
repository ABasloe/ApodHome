using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ApodHome.Core;

namespace ApodHome.Settings;

public partial class MainWindow : Window
{
    private CachedApod? _current;
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        var current = await CacheStore.ForDesktop().ReadAsync();
        _current = current;
        if (current is null || !File.Exists(current.ImagePath))
        {
            StatusText.Text = "No desktop APOD has been downloaded yet.";
            TitleText.Text = "Run Refresh desktop to download today’s image.";
            OpenSourceButton.IsEnabled = false;
            return;
        }
        StatusText.Text = "Your desktop image is managed by APOD Home.";
        TitleText.Text = current.Title;
        UpdatedText.Text = $"APOD date {current.Date:MMMM d, yyyy} · Updated {TimeZoneInfo.ConvertTime(current.UpdatedAtUtc, TimeZoneInfo.Local):g}";
        PreviewImage.Source = new BitmapImage(new Uri(current.ImagePath, UriKind.Absolute));
        OpenSourceButton.IsEnabled = true;
    }

    private async void RefreshDesktop_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () => await ApodHomeFactory.CreateUpdater().UpdateAsync(UpdateTarget.Desktop));
        await RefreshStatusAsync();
    }

    private async void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            await ApodHomeFactory.CreateUpdater().UpdateAsync(UpdateTarget.Desktop);
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "ApodHome.Admin.exe"),
                Arguments = "refresh-lock",
                Verb = "runas",
                UseShellExecute = true
            });
            process?.WaitForExit();
            if (process?.ExitCode != 0) throw new InvalidOperationException("The administrator refresh did not finish successfully.");
        });
        await RefreshStatusAsync();
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || !Uri.TryCreate(_current.PageUrl, UriKind.Absolute, out var page) || !SafeHttp.IsNasaPage(page))
        {
            MessageBox.Show(this, "The cached APOD page link is not a trusted NASA URL.", "APOD Home", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Process.Start(new ProcessStartInfo(page.AbsoluteUri) { UseShellExecute = true });
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        RefreshDesktopButton.IsEnabled = RefreshAllButton.IsEnabled = false;
        try { await action(); StatusText.Text = "Updated successfully."; }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "APOD Home", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { RefreshDesktopButton.IsEnabled = RefreshAllButton.IsEnabled = true; }
    }
}

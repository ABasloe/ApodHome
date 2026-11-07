# APOD Home

APOD Home is a Windows 11 companion that applies NASA's Astronomy Picture of
the Day to the desktop and the lock/sign-in screen without leaving a process
running. It uses the current NASA Science WordPress API rather than the
retiring legacy APOD API.

## Security model

The installer uses elevation only to set up user-level scheduled tasks and to
remove a legacy policy left by version 1.0.0. APOD media is always downloaded,
decoded, and applied by the signed-in user, never by SYSTEM or an elevated
process. The application uses Windows' per-user lock-screen API instead of a
machine policy, because that policy is unsupported on Windows Pro. It accepts
HTTPS URLs only, validates every redirect, and limits image downloads to 30 MB
and 100 million pixels.

Release builds must be code-signed before distribution. The repository does
not contain a signing certificate or private key; do not install binaries from
an unverified source.

## Build

Install the .NET 10 SDK, then run:

```powershell
dotnet restore
dotnet test
dotnet publish .\src\ApodHome.Updater\ApodHome.Updater.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
dotnet publish .\src\ApodHome.Admin\ApodHome.Admin.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
dotnet publish .\src\ApodHome.Settings\ApodHome.Settings.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

Compile `installer/ApodHome.iss` with Inno Setup after publishing. The
installer needs administrator approval once to create scheduled tasks. Use the
Start-menu **APOD Home** shortcut for status or a manual refresh. In Windows
Settings > Personalization > Lock screen, turn on **Show the lock screen
background picture on the sign-in screen** once if you also want the picture
behind the password/PIN prompt.

Per-user tasks update the desktop and lock screen at sign-in and hourly while
you are signed in. Every successful image is retained briefly, so an
unavailable network or APOD never replaces a working background.


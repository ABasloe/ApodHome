# APOD Home

APOD Home is a Windows 11 companion that applies NASA's Astronomy Picture of
the Day to the desktop and the lock/sign-in screen without leaving a process
running. It uses the current NASA Science WordPress API rather than the
retiring legacy APOD API.

## Security model

The installer uses elevation only to configure the fixed Windows lock/sign-in
policy and its user-level scheduled tasks. APOD media is always downloaded and
decoded by the signed-in user, never by SYSTEM or an elevated process. The
application accepts HTTPS URLs only, validates every redirect, limits image
downloads to 30 MB and 100 million pixels, and uses the same fixed per-user
cache image for desktop and lock/sign-in display.

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
installer needs administrator approval once to create the lock/sign-in policy
and scheduled tasks. Use the Start-menu **APOD Home** shortcut for status or a
manual refresh.

Per-user tasks update the image at sign-in and hourly while you are signed in.
Windows then uses the fixed cached image on the lock/sign-in screen. Every
successful image is retained briefly, so an unavailable network or APOD never
replaces a working background.


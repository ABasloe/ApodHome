#define MyAppName "APOD Home"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "APOD Home"
#define PublishDir "..\publish\win-x64"

[Setup]
AppId={{D31405D2-905B-49D6-BB42-F02CD8D6F13C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\APOD Home
DefaultGroupName={#MyAppName}
OutputDir=..\publish\installer
OutputBaseFilename=APOD-Home-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Release pipeline requirement: sign the generated setup EXE with a trusted
; Authenticode certificate. No signing key is stored in this repository.

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\APOD Home"; Filename: "{app}\ApodHome.Settings.exe"

[Run]
Filename: "{app}\ApodHome.Admin.exe"; Parameters: "install"; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "{app}\ApodHome.Admin.exe"; Parameters: "uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "APODHomeRemovePolicy"

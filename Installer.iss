#define AppVersion "6.2.0"
[Setup]
AppId={{609ECE40-9C04-4AFD-90DF-37535D846D26}
AppName=Playdeck
AppVersion={#AppVersion}
AppPublisher=Playdeck
AppPublisherURL=https://github.com/DopaLab/playdeck
AppSupportURL=https://github.com/DopaLab/playdeck/issues
AppUpdatesURL=https://github.com/DopaLab/playdeck/releases
DefaultDirName={localappdata}\Programs\Playdeck
DefaultGroupName=Playdeck
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..
OutputBaseFilename=Playdeck-Setup-{#AppVersion}
SetupIconFile=src\Playdeck\app.ico
UninstallDisplayIcon={app}\Playdeck.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=Playdeck.exe,Playdeck.Tracker.exe
RestartApplications=no
ChangesAssociations=yes
DisableProgramGroupPage=yes
SetupLogging=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "Portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\Playdeck"; Filename: "{app}\Playdeck.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Playdeck"; Filename: "{app}\Playdeck.exe"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\Playdeck.exe"; Parameters: "--install-menu"; Flags: runhidden waituntilterminated
Filename: "{app}\Playdeck.exe"; Description: "Open Playdeck"; Flags: nowait postinstall skipifsilent unchecked
[UninstallRun]
Filename: "{app}\Playdeck.exe"; Parameters: "--remove-menu"; Flags: runhidden waituntilterminated; RunOnceId: "RemovePlaydeckMenu"

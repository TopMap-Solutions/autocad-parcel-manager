#define MyAppName "Land Parcel Manager"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "TopMap Solutions"
#define MyAppURL "https://topmapsolutions.com/"
#define MyAppExeName "LandParcelManager.exe"

[Setup]

AppId={{B5844311-55AC-491E-80AD-7A707A211CAA}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}

SetupIconFile=..\Assets\topmap-logo.ico

UninstallDisplayIcon={app}\{#MyAppExeName}

LicenseFile=LICENSE.txt

DisableProgramGroupPage=yes

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

OutputDir=output
OutputBaseFilename=Land-Parcel-Manager-Setup

Compression=lzma2
SolidCompression=yes

WizardStyle=modern

PrivilegesRequired=admin

Uninstallable=yes


[Languages]

Name: "english"; MessagesFile: "compiler:Default.isl"


[Tasks]

Name: "desktopicon"; \
    Description: "{cm:CreateDesktopIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; \
    Flags: unchecked


[Files]

Source: "..\bin\Release\net10.0-windows\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]

Name: "{autoprograms}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon


[Run]

Filename: "{app}\{#MyAppExeName}"; \
    Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent
#define MyAppName "TM Land Parcel Buddy"
#define MyAppVersion "1.0.5"
#define MyAppPublisher "TopMap Solutions"

[Setup]

AppId={{TM-LAND-PARCEL-BUDDY}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\Autodesk\ApplicationPlugins

SetupIconFile=..\Assets\topmap-logo.ico

UninstallDisplayIcon={app}\topmap-logo.ico

LicenseFile=LICENSE.txt

DisableProgramGroupPage=yes

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

OutputDir=output
OutputBaseFilename=TM-Land-Parcel-Buddy-Setup

Compression=lzma2
SolidCompression=yes

WizardStyle=modern

PrivilegesRequired=admin

Uninstallable=yes


[Files]

Source: "..\release\TMParcel.bundle\*"; \
    DestDir: "{app}\TMParcel.bundle"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

Source: "..\Assets\topmap-logo.ico"; \
    DestDir: "{app}"; \
    Flags: ignoreversion


[UninstallDelete]

Type: filesandordirs; \
    Name: "{app}\TMParcel.bundle"

Type: files; \
    Name: "{app}\topmap-logo.ico"
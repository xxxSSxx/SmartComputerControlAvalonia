; ============================================================
; SmartComputerControl - Inno Setup installer script
; ============================================================
; Per buildare: aprire questo file con Inno Setup Compiler,
; oppure usare lo script build-windows-installer.ps1
; ============================================================

#define MyAppName "SmartComputerControl"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sergio Sbracia"
#define MyAppURL "https://github.com/xxxSSxx/SmartComputerControlAvalonia"
#define MyAppExeName "SmartComputerControl.exe"

[Setup]
; AppId univoco (NON CAMBIARE dopo il primo release!)
AppId={{F4A8B2E7-3C5D-4F6A-9D1B-8E7A9C0D1F23}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=installer-output
OutputBaseFilename=SmartComputerControl-Setup-{#MyAppVersion}
SetupIconFile=Assets\scc-icon.ico
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Avvia automaticamente all'accensione del computer"; GroupDescription: "Opzioni:"; Flags: unchecked

[Files]
; Tutto il contenuto della publish folder (escluso .pdb che e' solo per debug)
Source: "publish-win-x64\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Autoavvio (solo se l'utente ha spuntato il task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Killa il processo se in esecuzione prima di disinstallare
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM {#MyAppExeName}"; RunOnceId: "KillApp"; Flags: runhidden

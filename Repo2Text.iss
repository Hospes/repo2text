; Repo2Text.iss
; Script for Repo2Text Installer

#define MyAppName "Repo2Text"

; Define MyAppVersion. Use MyAppVersionFromWorkflow if defined, otherwise a default.
#ifndef MyAppVersionFromWorkflow
  #define MyAppVersion "0.0.0-dev" 
#else
  #define MyAppVersion MyAppVersionFromWorkflow
#endif

#define MyAppPublisher "Hosp Home"
; Update this
#define MyAppURL "https://github.com/Hospes/repo2text.git"
#define MyAppExeName "Repo2Text.exe"

[Setup]
; Generates a unique ID for your application
AppId={{AUTO_GUID}}
AppName={#MyAppName}
; AppVersion will also use the dynamic MyAppVersion
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Where ISCC will place the setup.exe relative to this script
OutputDir=.\installer_output
OutputBaseFilename=Repo2Text_Setup_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=none

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish_output\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: The above line will copy all files and subfolders from publish_output to the installation directory.

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}"; Filename: "{#MyAppURL}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; Add Uninstall information
[UninstallDelete]
Type: filesandordirs; Name: "{app}"
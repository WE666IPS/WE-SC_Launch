; SC Launch Inno Setup Script
#define MyAppName "SC Launch"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "WE"
#define MyAppURL "https://github.com/WE666IPS/WE-SC_Launch"
#define MyAppExeName "SC Launch.exe"

[Setup]
AppId={{B1E5E4F0-4A3C-4D2E-8F1A-9B6C7D8E0F1A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=installer_output
OutputBaseFilename=SC_Launch_windows_install
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion=1.0.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
MinVersion=10.0
WizardSizePercent=110

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Languages\English.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "startmenuicon"; Description: "{cm:CreateProgramGroup}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[CustomMessages]
; Chinese Simplified
chinesesimplified.CreateDesktopIcon=创建桌面快捷方式
chinesesimplified.CreateProgramGroup=创建开始菜单快捷方式
chinesesimplified.AdditionalIcons=附加图标:
chinesesimplified.UninstallProgram=卸载 %1
chinesesimplified.LaunchProgram=启动 %1

; English
english.CreateDesktopIcon=Create desktop shortcut
english.CreateProgramGroup=Create start menu shortcut
english.AdditionalIcons=Additional icons:
english.UninstallProgram=Uninstall %1
english.LaunchProgram=Launch %1

; Russian
russian.CreateDesktopIcon=Создать ярлык на рабочем столе
russian.CreateProgramGroup=Создать ярлык в меню "Пуск"
russian.AdditionalIcons=Дополнительные ярлыки:
russian.UninstallProgram=Удалить %1
russian.LaunchProgram=Запустить %1
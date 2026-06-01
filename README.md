# Win NCSI Fix

A stylish Windows utility for fixing and diagnosing Windows NCSI / Network Connectivity Status Indicator issues.

Windows sometimes shows **No Internet** or changes Wi-Fi behavior when it cannot reach Microsoft's connectivity probe endpoints. This tool makes the common registry fix safe, reversible, and easy to use — now with a much more polished and visually rich console experience.

> Built by **Mohammad Mehdi Azizi**  
> X / Twitter: https://x.com/the_azzi  
> Telegram: https://t.me/luluch_code  
> GitHub: https://github.com/TheGreatAzizi

---

## What's new in v3.0.0

- Fancy interactive console UI powered by **Spectre.Console**
- Figlet title, rounded panels, colored status badges, and selection menu
- Better dashboard for active probing, admin state, service state, and paths
- Nicer About, Status, Help, and Diagnose screens
- Same registry-safe behavior with backup / restore / logs / JSON output

---

## Features

- Rich interactive menu for double-click usage
- Auto UAC elevation through app manifest
- Disable / enable Windows NCSI Active Probing
- Full registry backup before destructive changes
- Restore latest backup or a selected `.reg` backup file
- Diagnostic mode for common connectivity detection problems
- Custom probe endpoint support
- Reset to Windows default NCSI values
- Restart Network Location Awareness service
- JSON output for automation and scripts
- Local log file in `%ProgramData%\WinNcsiFix\logs`
- GitHub Actions workflow for automatic release builds

---

## Usage

Double-click `WinNcsiFix.exe` to open the interactive menu.

CLI commands:

```powershell
WinNcsiFix.exe status
WinNcsiFix.exe status --json
WinNcsiFix.exe disable
WinNcsiFix.exe disable --yes
WinNcsiFix.exe enable
WinNcsiFix.exe diagnose
WinNcsiFix.exe diagnose --json
WinNcsiFix.exe backup
WinNcsiFix.exe restore
WinNcsiFix.exe restore C:\Path\To\backup.reg
WinNcsiFix.exe custom-probe --host example.com --path connecttest.txt --content OK
WinNcsiFix.exe reset-defaults
WinNcsiFix.exe restart-nla
WinNcsiFix.exe about
WinNcsiFix.exe open github
WinNcsiFix.exe open x
WinNcsiFix.exe open telegram
```

---

## Build

Requirements:

- Windows
- .NET 8 SDK

Build single-file EXE:

```powershell
dotnet publish src\WinNcsiFix\WinNcsiFix.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output:

```text
src\WinNcsiFix\bin\Release\net8.0-windows\win-x64\publish\WinNcsiFix.exe
```

---

## Dependency

This version uses:

- [Spectre.Console](https://spectreconsole.net/) for the fancy terminal UI

---

## License

MIT

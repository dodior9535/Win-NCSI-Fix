# Win NCSI Fix

Fix Windows "No Internet" detection issues safely and professionally.

🌍 Language:
🇺🇸 English | [🇮🇷 فارسی](README_fa.md)

## Download

### Latest Release
Download the latest version from:
https://github.com/TheGreatAzizi/win-ncsi-fix/releases/latest

### Direct Release Page
https://github.com/TheGreatAzizi/win-ncsi-fix/releases

## What is NCSI?
NCSI (Network Connectivity Status Indicator) is a Windows component that determines whether your device has internet access.

When Microsoft's probe endpoints are blocked by:
- ISPs
- DNS filtering
- Firewalls
- VPNs
- Corporate networks
- Country-wide restrictions

Windows may incorrectly display:
- No Internet
- Limited Connectivity
- Missing Wi-Fi features
- Microsoft Store issues
- Office activation problems

even though the internet is working.

## Features
✅ Interactive Console UI
✅ Automatic Administrator Elevation
✅ Safe Registry Backups
✅ Restore Previous Configurations
✅ Diagnostics Mode
✅ JSON Output
✅ Custom Probe Endpoints
✅ Reset To Windows Defaults
✅ Restart NLA Service
✅ Logging Support
✅ GitHub Actions Build Pipeline

## Installation
Download the latest executable from:
https://github.com/TheGreatAzizi/win-ncsi-fix/releases/latest

Run:
```powershell
WinNcsiFix.exe
```

or use CLI mode.

## Commands
### Status
```powershell
WinNcsiFix.exe status
```

### Disable Active Probe
```powershell
WinNcsiFix.exe disable
```

### Enable Active Probe
```powershell
WinNcsiFix.exe enable
```

### Diagnose
```powershell
WinNcsiFix.exe diagnose
```

### Backup
```powershell
WinNcsiFix.exe backup
```

### Restore
```powershell
WinNcsiFix.exe restore
```

### Custom Probe
```powershell
WinNcsiFix.exe custom-probe --host example.com --path connecttest.txt --content OK
```

### Reset Defaults
```powershell
WinNcsiFix.exe reset-defaults
```

## Build From Source
Requirements:
- Windows
- .NET 8 SDK

```powershell
dotnet publish src\WinNcsiFix\WinNcsiFix.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Security
This tool only modifies:
```reg
HKLM\SYSTEM\CurrentControlSet\Services\NlaSvc\Parameters\Internet
```
Automatic backups are created before modifications.

## License
MIT License

## Author
Mohammad Mehdi Azizi
GitHub: https://github.com/TheGreatAzizi
Telegram: https://t.me/luluch_code
X/Twitter: https://x.com/the_azzi

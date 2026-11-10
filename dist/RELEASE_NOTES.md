# Release Notes — v1.1.0

**Date:** 2026-11-10  
**Build:** release-win-x64

## What's New

### Per-Pad Rumble Scaling
You can now adjust the vibration intensity for EACH gamepad independently (0–100%).  
Open the tray menu → **"Pad Settings..."** to configure:
- **100%** — full rumble (default, recommended)
- **50%** — half-intensity  
- **0%** — disabled for that pad

### Bug Fixes
- **Fixed critical bug:** rumble was completely silent due to uninitialized `RumbleScale` field defaulting to 0. Now defaults to 100%.
- **Fixed race condition:** multiple gamepads plugging in simultaneously no longer cause duplicate connections or crashes.

### Documentation
- Full bilingual README (English / Русский)
- Updated STEAM-MultiPad setup guide
- Complete file structure explanation
- Troubleshooting table

## Files in This Release

| File | Description |
|------|-------------|
| `GamepadRumbleBridge.exe` | Main executable (runs as system tray app) |
| `GamepadRumbleBridge.dll` | Application assembly |
| `Nefarius.ViGEm.Client.dll` | ViGEmBus controller emulation library |
| `RunBridge.bat` | Launcher (right-click → Run as administrator) |
| `SendVibrate.ps1` | Manual rumble test script |
| `AddToStartup.vbs` | Register bridge to start with Windows |
| `README.md` | Full documentation (EN/RU) |
| `CHANGELOG.md` | Version history |
| `STEAM-MultiPad.txt` | Steam Input specific guide |
| `ru/*/`, `en/*/` etc. | Localization directories |

## System Requirements

- Windows 10/11 (64-bit)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ViGEmBus Driver](https://github.com/nefarius/ViGEmBus) (install once, then reboot)

## Supported Gamepads

Only controllers with **VID 0079 & PID 0006** are supported:
- Original Microsoft Xbox 360 Wired Controller
- Xbox 360 Wireless Controller (with USB receiver)
- Most clone/aftermarket USB gamepads using the same chip

## Quick Start

```powershell
# 1. Install ViGEmBus (one-time, requires admin)
winget install ViGEmBus
# Reboot after install

# 2. Run the bridge (as administrator)
RunBridge.bat

# 3. Plug in gamepads — auto-detected within 3 seconds

# 4. Test rumble
powershell -File SendVibrate.ps1 "0 255 255"
powershell -File SendVibrate.ps1 "1 255 0"
```

## GitHub

Repository: https://github.com/ersdgera/GamepadRumbleBridge  
Issues: https://github.com/ersdgera/GamepadRumbleBridge/issues

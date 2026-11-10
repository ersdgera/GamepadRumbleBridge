# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- Per-pad rumble scaling (0–100%) configurable via tray menu → "Pad Settings..."
- New `PadSettingsForm` dialog with per-slot trackbars
- Rumble scale is applied when routing XInputSetState → HID output report
- Log lines showing effective scale during rumble transmission ([TAG16])
- Bilingual README (English + Russian) with full documentation

### Fixed
- **Critical**: Default `RumbleScale` was 0 (struct default) causing ALL rumble to be silenced
- Race condition in `HidMonitor`: duplicate ConnectPad calls for same paths when multiple pads connected simultaneously — fixed with HashSet claimed-paths tracking

### Changed
- Default rumble scale set to 100% on new pad connection
- Improved logging: pad index and scale factor now shown on every rumble event

---

## [1.0.0] — 2026-11-10

### Added
- Background WinForms NotifyIcon app with admin privileges
- Automatic HID gamepad detection (VID 0079 PID 0006) every 3 seconds
- One ViGEmBus virtual Xbox 360 controller per physical pad (up to 4)
- XInputSetState(n) → correct slot → independent rumble routing
- Named Pipe server: `\\.\pipe\GamepadRumblePipe`
- Hotkey `Ctrl+Shift+R` to toggle rumble on all pads
- Tray menu with status, toggle, settings, and exit
- PowerShell test script `SendVibrate.ps1`
- Batch launcher `RunBridge.bat` with admin/ViGEm checks
- Auto-start registry helper `AddToStartup.vbs`
- Full bilingual documentation (README, STEAM-MultiPad.txt)

### Technical Details
- Language: C# .NET 8 WinForms
- ViGEm Client library v1.x for Xbox 360 controller emulation
- Direct HID I/O via kernel32.dll (CreateFile, HidD_SetOutputReport)
- SetupDi API for device enumeration
- Explicit UserIndex assignment via reflection before Connect()

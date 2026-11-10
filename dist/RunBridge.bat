@echo off
setlocal

echo ========================================
echo  Gamepad Rumble Bridge - Launcher
echo ========================================

:: Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo [!] Administrator privileges required!
    echo [!] Right-click RunBridge.bat -> "Run as administrator"
    echo.
    pause
    exit /b 1
)

:: Check ViGEmBus driver
sc query ViGEmBus >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo [!] ViGEmBus driver not found!
    echo [!] Install it: winget install ViGEmBus
    echo [!] Then run as administrator
    echo.
    pause
    exit /b 1
)

:: Start ViGEmBus service if stopped
sc query ViGEmBus | findstr /i "STOPPED" >nul 2>&1
if %errorlevel% equ 0 (
    echo [*] Starting ViGEmBus service...
    sc start ViGEmBus >nul 2>&1
    timeout /t 2 /nobreak >nul
)

:: Start bridge hidden
echo [*] Starting Gamepad Rumble Bridge...
start /b "" "%~dp0GamepadRumbleBridge.exe"

echo [*] Bridge started! Check tray icon.
echo [*] Ctrl+Shift+R to toggle vibration
timeout /t 2 /nobreak >nul
exit /b 0
' Добавляет мост вибрации в автозагрузку Windows
Dim WshShell, runKey
Set WshShell = CreateObject("WScript.Shell")
runKey = "HKCU\Software\Microsoft\Windows\CurrentVersion\Run\"
WshShell.RegWrite runKey & "GamepadRumbleBridge", """D:\MAN\GamepadBridge\dist\GamepadRumbleBridge.exe""", "REG_SZ"
WScript.Echo "Мост добавлен в автозагрузку!"

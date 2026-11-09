<script>
Set WshShell = CreateObject("WScript.Shell")
RunCmd = """D:\MAN\GamepadBridge\dist\RunBridge.bat"" /Q"
WshShell.Run RunCmd, 0, False
MsgBox "Мост вибрации запущен!", 64, "Готово"
</script>

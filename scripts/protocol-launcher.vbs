' Protocol launcher for windows-reminder://  (window style 0 = hidden)
Set WshShell = CreateObject("Wscript.Shell")
handlerPath = "C:\Users\chenjunjin\.claude\skills\windows-reminder\scripts\protocol-handler.ps1"
WshShell.Run "powershell.exe -NoProfile -ExecutionPolicy Bypass -File """ & handlerPath & """ """ & WScript.Arguments(0) & """", 0, False

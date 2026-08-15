Set WshShell = CreateObject("WScript.Shell")
currentDir = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName)
WshShell.Run "cmd /c taskkill /f /im LyricBar.exe >nul 2>&1 & if not exist """ & currentDir & "\bin\Debug\net9.0-windows10.0.22621.0\LyricBar.exe"" (dotnet build """ & currentDir & """) & start """" """ & currentDir & "\bin\Debug\net9.0-windows10.0.22621.0\LyricBar.exe""", 0, False

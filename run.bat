@echo off
cd /d "%~dp0"
taskkill /f /im LyricBar.exe >nul 2>&1
taskkill /f /im DesktopLyrics.exe >nul 2>&1
if not exist "%~dp0bin\Debug\net9.0-windows10.0.22621.0\LyricBar.exe" (
    dotnet build
)
start "" "%~dp0bin\Debug\net9.0-windows10.0.22621.0\LyricBar.exe"

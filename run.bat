@echo off
cd /d "%~dp0"
taskkill /f /im LyricBar.exe >nul 2>&1
taskkill /f /im DesktopLyrics.exe >nul 2>&1
dotnet run

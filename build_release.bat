@echo off
echo =======================================================
echo          Building LyricBar Lightweight Release
echo =======================================================
cd /d "%~dp0"

taskkill /f /im LyricBar.exe >nul 2>&1
taskkill /f /im DesktopLyrics.exe >nul 2>&1

dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./dist

echo.
echo =======================================================
echo [SUCCESS] Lightweight standalone EXE generated in:
echo           %~dp0dist\LyricBar.exe
echo =======================================================
pause

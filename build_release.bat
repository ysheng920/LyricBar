@echo off
echo =======================================================
echo          Building LyricBar Single-File Release
echo =======================================================
cd /d "%~dp0"

dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./dist

echo.
echo =======================================================
echo [SUCCESS] Standalone EXE generated in:
echo           %~dp0dist\LyricBar.exe
echo =======================================================
pause

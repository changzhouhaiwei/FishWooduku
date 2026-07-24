@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
echo.
echo WoodukuClient Android one-click build
echo   Full pipeline: Unity export + Gradle APK/AAB
echo   Gradle only:   BuildAndroid.ps1 -SkipUnityExport true -VersionName x -BuildNumber n -BuildTarget 3
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%BuildAndroid.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"

if "%~1"=="" (
    echo.
    pause
)

exit /b %EXIT_CODE%

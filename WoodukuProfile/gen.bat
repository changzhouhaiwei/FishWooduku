@echo off
setlocal
cd /d "%~dp0"

rem 快速校验：仅导出 json 到本目录 output（相对路径）
set "REPO_ROOT=%~dp0.."
set "LUBAN_DLL=%REPO_ROOT%\Tools\Luban\Luban.dll"
set "CONF_ROOT=%~dp0"

if not exist "%LUBAN_DLL%" (
    echo [ERROR] Luban not found: "%LUBAN_DLL%"
    exit /b 1
)

dotnet "%LUBAN_DLL%" ^
    -t all ^
    -d json ^
    --conf "%CONF_ROOT%luban.conf" ^
    -x outputDataDir="%CONF_ROOT%output"

set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" (
    echo [FAILED] exit code %EXIT_CODE%
    exit /b %EXIT_CODE%
)

echo [OK] json exported to WoodukuProfile\output
exit /b 0

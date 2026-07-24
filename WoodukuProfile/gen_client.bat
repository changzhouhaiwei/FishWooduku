@echo off
setlocal
cd /d "%~dp0"

rem 仓库根目录（相对本脚本：WoodukuProfile 的上一级）
set "REPO_ROOT=%~dp0.."
set "LUBAN_DLL=%REPO_ROOT%\Tools\Luban\Luban.dll"
set "CONF_ROOT=%~dp0"

if not exist "%LUBAN_DLL%" (
    echo [ERROR] Luban not found: "%LUBAN_DLL%"
    echo Please keep Tools\Luban under the repo root.
    exit /b 1
)

echo Luban: "%LUBAN_DLL%"
echo Conf : "%CONF_ROOT%luban.conf"
echo Code : "%REPO_ROOT%\WoodukuClient\Assets\Scripts\GameLogic\Cfg"
echo Data : "%REPO_ROOT%\WoodukuClient\Assets\GameRes\Config"
echo.

dotnet "%LUBAN_DLL%" ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf "%CONF_ROOT%luban.conf" ^
    -x outputCodeDir="%REPO_ROOT%\WoodukuClient\Assets\Scripts\GameLogic\Cfg" ^
    -x outputDataDir="%REPO_ROOT%\WoodukuClient\Assets\GameRes\Config"

set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" (
    echo.
    echo [FAILED] exit code %EXIT_CODE%
    exit /b %EXIT_CODE%
)

echo.
echo [OK] client config generated.
exit /b 0

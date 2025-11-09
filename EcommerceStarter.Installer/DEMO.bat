@echo off
REM EcommerceStarter Installer - Beautiful Demo Launcher

echo.
echo ============================================
echo   EcommerceStarter Demo Launcher
echo   Loading beautiful UI...
echo ============================================
echo.

set LAUNCHER_PATH=%~dp0DemoLauncher\bin\Release\net8.0-windows\DemoLauncher.exe

REM Check if GUI launcher exists
if exist "%LAUNCHER_PATH%" (
    echo Launching beautiful demo launcher...
    start "" "%LAUNCHER_PATH%"
    exit /b 0
)

REM Fallback: Try to build and launch
echo Building demo launcher...
cd "%~dp0"
dotnet build EcommerceStarter.DemoLauncher -c Release --no-restore >nul 2>&1

if exist "%LAUNCHER_PATH%" (
    echo Launching beautiful demo launcher...
    start "" "%LAUNCHER_PATH%"
    exit /b 0
)

REM Final fallback: Use command-line version
echo.
echo Beautiful launcher not available. Using command-line version...
echo.

REM [Rest of the existing DEMO.bat logic as fallback]
set RELEASE_PATH=%~dp0bin\Release\net8.0-windows\EcommerceStarter.Installer.exe
set DEBUG_PATH=%~dp0bin\Debug\net8.0-windows\EcommerceStarter.Installer.exe

if exist "%RELEASE_PATH%" (set R=1) else (set R=0)
if exist "%DEBUG_PATH%" (set D=1) else (set D=0)

if %R%==0 if %D%==0 (
    echo No builds found. Build now?
    echo   [R] Release
    echo   [D] Debug  
    echo   [Q] Quit
    echo.
    set /p CHOICE="Choice (R/D/Q): "
    if /i "%CHOICE%"=="Q" exit /b
    if /i "%CHOICE%"=="R" goto BldRel
    if /i "%CHOICE%"=="D" goto BldDbg
    pause
    exit /b
)

echo Available:
if %R%==1 echo   [R] Release
if %D%==1 echo   [D] Debug
echo   [B] Build
echo.

if %R%==1 if %D%==0 (set EXE=%RELEASE_PATH% & goto Pick)
if %D%==1 if %R%==0 (set EXE=%DEBUG_PATH% & goto Pick)

:Ask
set /p CHOICE="Use (R/D/B): "
if /i "%CHOICE%"=="R" (set EXE=%RELEASE_PATH% & goto Pick)
if /i "%CHOICE%"=="D" (set EXE=%DEBUG_PATH% & goto Pick)
if /i "%CHOICE%"=="B" (
    set /p BC="Build (R/D/C): "
    if /i "%BC%"=="R" goto BldRel
    if /i "%BC%"=="D" goto BldDbg
)
goto Ask

:BldRel
dotnet build -c Release
if errorlevel 1 pause
set EXE=%RELEASE_PATH%
goto Pick

:BldDbg
dotnet build -c Debug
if errorlevel 1 pause
set EXE=%DEBUG_PATH%
goto Pick

:Pick
echo.
echo Scenario:
echo   [1] All
echo   [2] Fresh Install
echo   [3] Upgrade
echo   [4] Reconfigure
echo   [5] Repair
echo   [6] Uninstall
set /p S="Choice: "

set F=--demo
if "%S%"=="2" set F=--demo-fresh
if "%S%"=="3" set F=--demo-upgrade
if "%S%"=="4" set F=--demo-reconfig
if "%S%"=="5" set F=--demo-repair
if "%S%"=="6" set F=--demo-uninstall

echo.
echo Launching...
"%EXE%" %F%
pause

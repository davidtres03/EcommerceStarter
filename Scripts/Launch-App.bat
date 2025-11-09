@echo off
REM Quick launcher for EcommerceStarter application

echo.
echo Starting EcommerceStarter...
echo.

REM Set environment to Development
set ASPNETCORE_ENVIRONMENT=Development

REM Change to application directory
cd /d "%~dp0..\EcommerceStarter"

REM Launch using dotnet run (recommended)
dotnet run

pause

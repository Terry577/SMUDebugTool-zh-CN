@echo off
setlocal

set "RELEASE_VALUE=0"
set "RELEASE_NUMBER=0"

for /f "tokens=3" %%R in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul ^| find /i "Release"') do (
    set "RELEASE_VALUE=%%R"
)

set /a RELEASE_NUMBER=%RELEASE_VALUE% 2>nul
if %RELEASE_NUMBER% GEQ 533320 goto launch

echo Microsoft .NET Framework 4.8.1 is required.
echo Starting the included official Microsoft offline installer...
echo.

if not exist "%~dp0NDP481-x86-x64-AllOS-ENU.exe" (
    echo The .NET Framework installer is missing.
    echo Please download it from:
    echo https://dotnet.microsoft.com/download/dotnet-framework/net481
    pause
    exit /b 1
)

start /wait "" "%~dp0NDP481-x86-x64-AllOS-ENU.exe" /passive /norestart
set "INSTALL_EXIT=%ERRORLEVEL%"

if "%INSTALL_EXIT%"=="0" goto launch
if "%INSTALL_EXIT%"=="3010" goto restart_required

echo.
echo .NET Framework installation failed with exit code %INSTALL_EXIT%.
pause
exit /b %INSTALL_EXIT%

:restart_required
echo.
echo .NET Framework 4.8.1 was installed successfully.
echo Restart Windows before running SMUDebugTool.
pause
exit /b 0

:launch
start "" "%~dp0SMUDebugTool.exe"
exit /b 0

@echo off
echo DesktopMemo Startup Fix Tool
echo ============================

echo.
echo Checking current status...

REM Check current registry entry
reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Found existing startup entry
    echo Current registered startup:
    reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo
    echo.

    echo Removing invalid startup entry...
    reg delete "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo /f >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        echo Successfully removed invalid startup entry
    ) else (
        echo Failed to remove startup entry
    )
) else (
    echo No existing startup entry found
)

echo.
echo Looking for executable file...

REM Find executable file
set "EXE_PATH="
if exist "%~dp0DesktopMemo.exe" (
    set "EXE_PATH=%~dp0DesktopMemo.exe"
    echo Found executable: %EXE_PATH%
) else if exist "%~dp0bin\Release\net8.0-windows\win-x64\DesktopMemo.exe" (
    set "EXE_PATH=%~dp0bin\Release\net8.0-windows\win-x64\DesktopMemo.exe"
    echo Found build output: %EXE_PATH%
) else if exist "%~dp0bin\Release\net8.0-windows\DesktopMemo.exe" (
    set "EXE_PATH=%~dp0bin\Release\net8.0-windows\DesktopMemo.exe"
    echo Found build output: %EXE_PATH%
) else (
    echo Executable file not found
    echo Please ensure the program is properly built
    goto :error
)

echo.
echo Setting up startup...

REM Set new startup entry
reg add "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo /t REG_SZ /d "\"%EXE_PATH%\"" /f >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Startup setup successful
) else (
    echo Startup setup failed
    goto :error
)

echo.
echo Verifying setup...

REM Verify setup
reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Startup verification successful
    echo New startup entry:
    reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo
) else (
    echo Startup verification failed
    goto :error
)

echo.
echo Startup fix completed successfully!
echo The program will start automatically on next boot.
goto :end

:error
echo.
echo Fix failed!
echo Possible reasons:
echo - Insufficient administrator privileges
echo - Executable file does not exist
echo - Registry access blocked
echo.
echo Suggestions:
echo 1. Run this script as administrator
echo 2. Ensure the program is properly built
echo 3. Check if antivirus software is blocking registry modifications

:end
echo.
pause

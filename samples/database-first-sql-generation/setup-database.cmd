@echo off
REM Wrapper script to run PowerShell setup script with proper execution policy
echo Setting up LocalDB with EfcptSampleDb...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0setup-database.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Setup failed. Please check the error messages above.
    pause
    exit /b 1
)
echo.
pause

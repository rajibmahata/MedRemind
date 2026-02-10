@echo off
REM ========================================
REM  MedRemind - Run Backend API + Frontend
REM ========================================

echo.
echo ========================================
echo  Starting MedRemind Application
echo ========================================
echo.

REM Navigate to backend API
cd /d "%~dp0backend\MedRemind.API"

echo [1/2] Starting Backend API on port 5000...
echo.

REM Start backend in new window
start "MedRemind API" cmd /k "dotnet run --urls=https://localhost:5000"

REM Wait for API to start
timeout /t 5 /nobreak >nul

echo.
echo [2/2] Starting Blazor UI on port 5001...
echo.

REM Navigate to web project
cd /d "%~dp0web\MedRemind.Web"

REM Start frontend in new window
start "MedRemind UI" cmd /k "dotnet watch run"

echo.
echo ========================================
echo  Both services started!
echo ========================================
echo.
echo Backend API: https://localhost:5000
echo Frontend UI: https://localhost:5001
echo Swagger: https://localhost:5000/swagger
echo.
echo Press any key to stop all services...
pause >nul

REM Kill both processes
taskkill /FI "WINDOWTITLE eq MedRemind API*" /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq MedRemind UI*" /F >nul 2>&1

echo Services stopped.

# ========================================
#  MedRemind - Run Backend API + Frontend
# ========================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Starting MedRemind Application" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get project root
$projectRoot = $PSScriptRoot

# Start Backend API
Write-Host "[1/2] Starting Backend API on port 5000..." -ForegroundColor Yellow
Write-Host ""

$apiPath = Join-Path $projectRoot "backend\MedRemind.API"
$apiJob = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$apiPath'; dotnet run --urls=http://localhost:5000" -PassThru

# Wait for API to start
Start-Sleep -Seconds 5

# Start Frontend UI
Write-Host ""
Write-Host "[2/2] Starting Blazor UI on port 5001..." -ForegroundColor Yellow
Write-Host ""

$webPath = Join-Path $projectRoot "web\MedRemind.Web"
$uiJob = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$webPath'; dotnet watch run" -PassThru

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Both services started!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Backend API: " -NoNewline -ForegroundColor White
Write-Host "http://localhost:5000" -ForegroundColor Cyan
Write-Host "Frontend UI: " -NoNewline -ForegroundColor White
Write-Host "http://localhost:5001" -ForegroundColor Cyan
Write-Host "Swagger:     " -NoNewline -ForegroundColor White
Write-Host "http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Yellow
Write-Host ""

# Keep script running
try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
}
finally {
    # Cleanup on exit
    Write-Host ""
    Write-Host "Stopping services..." -ForegroundColor Red
    
    if ($apiJob -and !$apiJob.HasExited) {
        Stop-Process -Id $apiJob.Id -Force -ErrorAction SilentlyContinue
    }
    
    if ($uiJob -and !$uiJob.HasExited) {
        Stop-Process -Id $uiJob.Id -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "Services stopped." -ForegroundColor Green
}

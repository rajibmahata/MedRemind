# MedRemind Build Fix Script
# Fixes GetQueryable() errors and other build issues

Write-Host "?? MedRemind Build Fix Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$projectRoot = "F:\rajibmahata\MedRemind"

# Fix 1: ValidationWorkflowService.cs - Replace all GetQueryable() calls
Write-Host "?? Fixing ValidationWorkflowService.cs..." -ForegroundColor Yellow

$validationWorkflowFile = Join-Path $projectRoot "backend\MedRemind.Services\Validation\ValidationWorkflowService.cs"

if (Test-Path $validationWorkflowFile) {
    $content = Get-Content $validationWorkflowFile -Raw
    
    # Replace .GetQueryable().Include(w => w.Prescription).FirstOrDefaultAsync with separate calls
    $content = $content -replace '\.GetQueryable\(\)\s+\.Include\([^\)]+\)\s+\.FirstOrDefaultAsync', '.FirstOrDefaultAsync'
    
    # Replace remaining .GetQueryable().FirstOrDefaultAsync patterns
    $content = $content -replace '\.GetQueryable\(\)\s+\.FirstOrDefaultAsync', '.FirstOrDefaultAsync'
    
    # Replace .GetQueryable().Where(...).ToListAsync() patterns with FindAsync
    $content = $content -replace 'await\s+(\w+Repository)\s+\.GetQueryable\(\)\s+\.Where\(([^\)]+)\)\s+\.ToListAsync\(\);', '(await $1.FindAsync($2)).ToList();'
    
    Set-Content $validationWorkflowFile -Value $content
    Write-Host "  ? ValidationWorkflowService.cs fixed" -ForegroundColor Green
} else {
    Write-Host "  ??  ValidationWorkflowService.cs not found" -ForegroundColor Red
}

# Fix 2: VoiceRecordingService.cs - Replace GetQueryable() calls
Write-Host "?? Fixing VoiceRecordingService.cs..." -ForegroundColor Yellow

$voiceRecordingServiceFile = Join-Path $projectRoot "backend\MedRemind.Services\VoiceRecordings\VoiceRecordingService.cs"

if (Test-Path $voiceRecordingServiceFile) {
    $content = Get-Content $voiceRecordingServiceFile -Raw
    
    # Replace GetQueryable patterns
    $content = $content -replace '\.GetQueryable\(\)\s+\.FirstOrDefaultAsync', '.FirstOrDefaultAsync'
    $content = $content -replace 'await\s+(\w+Repository)\s+\.GetQueryable\(\)\s+\.Where\(([^\)]+)\)\s+\.ToListAsync\(\);', '(await $1.FindAsync($2)).ToList();'
    
    Set-Content $voiceRecordingServiceFile -Value $content
    Write-Host "  ? VoiceRecordingService.cs fixed" -ForegroundColor Green
} else {
    Write-Host "  ??  VoiceRecordingService.cs not found" -ForegroundColor Red
}

# Fix 3: VoiceRecordingStorageService.cs - Already fixed in previous steps
Write-Host "?? Verifying VoiceRecordingStorageService.cs..." -ForegroundColor Yellow

$voiceStorageFile = Join-Path $projectRoot "backend\MedRemind.Services\Storage\VoiceRecordingStorageService.cs"

if (Test-Path $voiceStorageFile) {
    $content = Get-Content $voiceStorageFile -Raw
    
    # Check if GetFullPath or GetFolderPath still exist
    if ($content -match 'GetFullPath|GetFolderPath') {
        Write-Host "  ??  Found GetFullPath/GetFolderPath - needs manual review" -ForegroundColor Yellow
    } else {
        Write-Host "  ? VoiceRecordingStorageService.cs looks good" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "?? Building solution..." -ForegroundColor Cyan

# Build the backend API
$apiProject = Join-Path $projectRoot "backend\MedRemind.API\MedRemind.API.csproj"

try {
    $buildOutput = dotnet build $apiProject 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? BUILD SUCCESSFUL!" -ForegroundColor Green
        Write-Host ""
        
        # Show warnings summary
        $warnings = $buildOutput | Select-String "warning"
        if ($warnings) {
            Write-Host "??  Warnings:" -ForegroundColor Yellow
            $warnings | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkYellow }
        }
    } else {
        Write-Host "? BUILD FAILED!" -ForegroundColor Red
        Write-Host ""
        
        # Show errors
        $errors = $buildOutput | Select-String "error CS"
        if ($errors) {
            Write-Host "? Errors:" -ForegroundColor Red
            $errors | Select-Object -First 10 | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        }
        
        # Show full output for debugging
        Write-Host ""
        Write-Host "Full build output saved to build-output.log" -ForegroundColor Yellow
        $buildOutput | Out-File "build-output.log"
        
        exit 1
    }
} catch {
    Write-Host "? Build command failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "?? Build fix completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run backend: cd backend\MedRemind.API && dotnet run" -ForegroundColor White
Write-Host "  2. Run frontend: cd web\MedRemind.Web && dotnet watch run" -ForegroundColor White
Write-Host ""

# Fix remaining GetQueryable issues in RemindersController.cs

$file = "F:\rajibmahata\MedRemind\backend\MedRemind.API\Controllers\RemindersController.cs"
$content = Get-Content $file -Raw

# Replace all GetQueryable().Include().FirstOrDefaultAsync patterns
$content = $content -replace '\.GetQueryable\(\)\s+\.Include\([^\)]+\)\s+\.FirstOrDefaultAsync', '.FirstOrDefaultAsync'

# Replace remaining GetQueryable().FirstOrDefaultAsync
$content = $content -replace '\.GetQueryable\(\)\s+\.FirstOrDefaultAsync', '.FirstOrDefaultAsync'

Set-Content $file -Value $content

Write-Host "Fixed RemindersController.cs" -ForegroundColor Green

# Build
Write-Host "`nBuilding..." -ForegroundColor Cyan
dotnet build "F:\rajibmahata\MedRemind\backend\MedRemind.API\MedRemind.API.csproj"

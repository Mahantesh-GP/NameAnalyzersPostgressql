# ============================================================================
# Quick Password Update Script
# Updates PostgreSQL password in both Functions' local.settings.json
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$Password
)

Write-Host "Updating PostgreSQL password in local.settings.json files..." -ForegroundColor Cyan

$files = @(
    "src\PhoneticAnalyzers.SQLDBFirst.Functions.Search\local.settings.json",
    "src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\local.settings.json"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw | ConvertFrom-Json
        $oldConnectionString = $content.Values."ConnectionStrings__PhoneticDb"
        $newConnectionString = $oldConnectionString -replace "Password=[^;]+", "Password=$Password"
        $content.Values."ConnectionStrings__PhoneticDb" = $newConnectionString
        
        $content | ConvertTo-Json -Depth 10 | Set-Content $file
        Write-Host "✓ Updated $file" -ForegroundColor Green
    } else {
        Write-Host "✗ File not found: $file" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Password updated successfully!" -ForegroundColor Green
Write-Host "New connection string format:" -ForegroundColor Yellow
Write-Host "  Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=$Password" -ForegroundColor Cyan

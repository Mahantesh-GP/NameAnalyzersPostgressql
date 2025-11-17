# Quick start script for Native API
$ErrorActionPreference = 'Stop'

Write-Host "Starting Phonetic Analyzers Native API..." -ForegroundColor Cyan

# Check if database is deployed
$DbHost = if ($env:PGHOST) { $env:PGHOST } else { 'localhost' }
$DbPort = if ($env:PGPORT) { [int]$env:PGPORT } else { 5432 }
$DbName = if ($env:PGDATABASE) { $env:PGDATABASE } else { 'phonetic_native' }
$DbUser = if ($env:PGUSER) { $env:PGUSER } else { 'postgres' }

Write-Host "Checking database connection: $DbHost:$DbPort/$DbName" -ForegroundColor Yellow

try {
    $env:PGPASSWORD = if ($env:PGPASSWORD) { $env:PGPASSWORD } else { 'postgres' }
    $result = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -c "SELECT 1" 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Database not found. Deploying schema..." -ForegroundColor Yellow
        
        # Create database if needed
        & psql -h $DbHost -p $DbPort -U $DbUser -d postgres -c "CREATE DATABASE $DbName" 2>$null
        
        # Deploy schema
        Push-Location "$PSScriptRoot\..\scripts"
        .\run-all.ps1
        Pop-Location
        
        Write-Host "Database deployed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Database connection OK" -ForegroundColor Green
    }
} catch {
    Write-Warning "Could not verify database. Make sure PostgreSQL is running."
}

# Start the API
Write-Host "`nStarting API on http://localhost:5100" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5100/swagger" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop`n" -ForegroundColor Yellow

Push-Location "$PSScriptRoot"
dotnet run
Pop-Location

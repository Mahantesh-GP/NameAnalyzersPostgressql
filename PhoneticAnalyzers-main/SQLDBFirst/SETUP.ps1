# Database-First Setup Script - Simplified Version
# Run this from the SQLDBFirst folder

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  Phonetic Analyzer - Database-First Setup" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

# PostgreSQL path
$pgPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"

# Step 1: Check PostgreSQL
Write-Host "[Step 1/6] Checking PostgreSQL..." -ForegroundColor Green
$version = & $pgPath -U postgres -c "SELECT version();" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  PostgreSQL is accessible" -ForegroundColor Green
} else {
    Write-Host "  Cannot connect. Check password and service." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Create Database
Write-Host "[Step 2/6] Creating Database..." -ForegroundColor Green
$checkDb = & $pgPath -U postgres -t -c "SELECT 1 FROM pg_database WHERE datname='phonetic_db_dbfirst';" 2>&1
if ($checkDb -match "1") {
    Write-Host "  Database already exists" -ForegroundColor Yellow
    $recreate = Read-Host "  Recreate? (yes/no)"
    if ($recreate -eq "yes") {
        & $pgPath -U postgres -c "DROP DATABASE IF EXISTS phonetic_db_dbfirst;" | Out-Null
        & $pgPath -U postgres -c "CREATE DATABASE phonetic_db_dbfirst;" | Out-Null
        Write-Host "  Database recreated" -ForegroundColor Green
    }
} else {
    & $pgPath -U postgres -c "CREATE DATABASE phonetic_db_dbfirst;" | Out-Null
    Write-Host "  Database created" -ForegroundColor Green
}
Write-Host ""

# Step 3: Run SQL Scripts
Write-Host "[Step 3/6] Running SQL Scripts..." -ForegroundColor Green
Set-Location DatabaseScripts

$scripts = @("001_CreateSchema.sql", "002_SeedNicknames.sql", "003_SeedTestData.sql")
foreach ($script in $scripts) {
    Write-Host "  Running $script..." -ForegroundColor Yellow
    & $pgPath -U postgres -d phonetic_db_dbfirst -f $script 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Completed" -ForegroundColor Green
    }
}
Set-Location ..
Write-Host ""

# Step 4: Verify Data
Write-Host "[Step 4/6] Verifying Data..." -ForegroundColor Green
$persons = & $pgPath -U postgres -d phonetic_db_dbfirst -t -c "SELECT COUNT(*) FROM person;" 2>&1
$nicknames = & $pgPath -U postgres -d phonetic_db_dbfirst -t -c "SELECT COUNT(*) FROM nickname_maps;" 2>&1
Write-Host "  Persons: $($persons.Trim())" -ForegroundColor Cyan
Write-Host "  Nicknames: $($nicknames.Trim())" -ForegroundColor Cyan
Write-Host ""

# Step 5: Check Config
Write-Host "[Step 5/6] Checking Configuration..." -ForegroundColor Green
$searchConfig = "src\PhoneticAnalyzers.SQLDBFirst.Functions.Search\local.settings.json"
$ingestionConfig = "src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\local.settings.json"

if (Test-Path $searchConfig) {
    Write-Host "  Search Function config found" -ForegroundColor Green
} else {
    Write-Host "  Search Function config missing" -ForegroundColor Yellow
}

if (Test-Path $ingestionConfig) {
    Write-Host "  Ingestion Function config found" -ForegroundColor Green
} else {
    Write-Host "  Ingestion Function config missing" -ForegroundColor Yellow
}
Write-Host ""

# Step 6: Build Projects
Write-Host "[Step 6/6] Building Projects..." -ForegroundColor Green
Set-Location src

$projects = @(
    "PhoneticAnalyzers.SQLDBFirst.Domain",
    "PhoneticAnalyzers.SQLDBFirst.Infrastructure",
    "PhoneticAnalyzers.SQLDBFirst.Application",
    "PhoneticAnalyzers.SQLDBFirst.Functions.Search",
    "PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion"
)

foreach ($proj in $projects) {
    Write-Host "  Building $proj..." -ForegroundColor Yellow
    dotnet build "$proj\$proj.csproj" --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Success" -ForegroundColor Green
    }
}
Set-Location ..
Write-Host ""

# Summary
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Update password: .\UPDATE-PASSWORD.ps1 -Password YOUR_PASSWORD" -ForegroundColor White
Write-Host "  2. Start Search:    cd src\PhoneticAnalyzers.SQLDBFirst.Functions.Search" -ForegroundColor White
Write-Host "                      func start --port 7072" -ForegroundColor White
Write-Host "  3. Start Ingestion: cd src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion" -ForegroundColor White
Write-Host "                      func start --port 7073" -ForegroundColor White
Write-Host ""

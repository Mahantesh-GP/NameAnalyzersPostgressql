# ============================================================================
# Database-First Setup Script
# This script will set up the complete Database-First environment
# ============================================================================

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "  Phonetic Analyzer - Database-First Setup" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

# Set PostgreSQL path
$pgPath = "C:\Program Files\PostgreSQL\17\bin"
$env:Path = "$pgPath;$env:Path"

# Navigate to SQLDBFirst folder
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

Write-Host "Current Directory: $(Get-Location)" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# Step 1: Check PostgreSQL Connection
# ============================================================================
Write-Host "[Step 1/6] Checking PostgreSQL Connection..." -ForegroundColor Green

try {
    $versionCmd = "SELECT version();"
    $result = & "$pgPath\psql.exe" -U postgres -c $versionCmd 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  PostgreSQL is accessible" -ForegroundColor Green
    } else {
        Write-Host "  Cannot connect to PostgreSQL" -ForegroundColor Red
        Write-Host "  Please ensure PostgreSQL is running and you have the correct password" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "  Error connecting to PostgreSQL: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================================
# Step 2: Create Database
# ============================================================================
Write-Host "[Step 2/6] Creating Database 'phonetic_db_dbfirst'..." -ForegroundColor Green

# Check if database already exists
$dbExistsQuery = "SELECT 1 FROM pg_database WHERE datname='phonetic_db_dbfirst';"
$dbExists = & "$pgPath\psql.exe" -U postgres -t -c $dbExistsQuery 2>&1
if ($dbExists -match "1") {
    Write-Host "  Database 'phonetic_db_dbfirst' already exists" -ForegroundColor Yellow
    $response = Read-Host "  Do you want to drop and recreate it? (yes/no)"
    if ($response -eq "yes") {
        Write-Host "  Dropping existing database..." -ForegroundColor Yellow
        $dropCmd = "DROP DATABASE IF EXISTS phonetic_db_dbfirst;"
        & "$pgPath\psql.exe" -U postgres -c $dropCmd | Out-Null
        $createCmd = "CREATE DATABASE phonetic_db_dbfirst;"
        & "$pgPath\psql.exe" -U postgres -c $createCmd | Out-Null
        Write-Host "  Database recreated successfully" -ForegroundColor Green
    } else {
        Write-Host "  Using existing database" -ForegroundColor Yellow
    }
} else {
    $createCmd = "CREATE DATABASE phonetic_db_dbfirst;"
    & "$pgPath\psql.exe" -U postgres -c $createCmd | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Database created successfully" -ForegroundColor Green
    } else {
        Write-Host "  Failed to create database" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# ============================================================================
# Step 3: Run SQL Scripts
# ============================================================================
Write-Host "[Step 3/6] Running SQL Scripts..." -ForegroundColor Green

$scripts = @(
    "001_CreateSchema.sql",
    "002_SeedNicknames.sql",
    "003_SeedTestData.sql"
)

Set-Location "DatabaseScripts"

foreach ($script in $scripts) {
    Write-Host "  Running $script..." -ForegroundColor Yellow
    & "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -f $script 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ $script completed" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $script failed" -ForegroundColor Red
    }
}

Set-Location ..
Write-Host ""

# ============================================================================
# Step 4: Verify Database Content
# ============================================================================
Write-Host "[Step 4/6] Verifying Database Content..." -ForegroundColor Green

$personCountCmd = "SELECT COUNT(*) FROM person;"
$personCount = & "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -t -c $personCountCmd 2>&1
$nicknameCountCmd = "SELECT COUNT(*) FROM nickname_maps;"
$nicknameCount = & "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -t -c $nicknameCountCmd 2>&1

Write-Host "  Person records: $($personCount.Trim())" -ForegroundColor Cyan
Write-Host "  Nickname mappings: $($nicknameCount.Trim())" -ForegroundColor Cyan

if ($personCount -match "\d+" -and [int]$personCount.Trim() -gt 0) {
    Write-Host "✓ Database content verified" -ForegroundColor Green
} else {
    Write-Host "⚠ Warning: No person records found" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# Step 5: Check Connection String Configuration
# ============================================================================
Write-Host "[Step 5/6] Checking Connection String Configuration..." -ForegroundColor Green

$searchFuncSettings = "src\PhoneticAnalyzers.SQLDBFirst.Functions.Search\local.settings.json"
$ingestionFuncSettings = "src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\local.settings.json"

$connectionString = "Host=localhost;Port=5432;Database=phonetic_db_dbfirst;Username=postgres;Password=your_password_here"

# Check Search Function settings
if (Test-Path $searchFuncSettings) {
    $settings = Get-Content $searchFuncSettings -Raw | ConvertFrom-Json
    if ($settings.Values.PostgreSQLConnection) {
        Write-Host "  ✓ Search Function connection string found" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Search Function connection string not configured" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠ Search Function local.settings.json not found" -ForegroundColor Yellow
}

# Check Ingestion Function settings
if (Test-Path $ingestionFuncSettings) {
    $settings = Get-Content $ingestionFuncSettings -Raw | ConvertFrom-Json
    if ($settings.Values.PostgreSQLConnection) {
        Write-Host "  ✓ Ingestion Function connection string found" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Ingestion Function connection string not configured" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠ Ingestion Function local.settings.json not found" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# Step 6: Build Projects
# ============================================================================
Write-Host "[Step 6/6] Building SQLDBFirst Projects..." -ForegroundColor Green

Set-Location "src"

$projects = @(
    "PhoneticAnalyzers.SQLDBFirst.Domain",
    "PhoneticAnalyzers.SQLDBFirst.Infrastructure",
    "PhoneticAnalyzers.SQLDBFirst.Application",
    "PhoneticAnalyzers.SQLDBFirst.Functions.Search",
    "PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion"
)

foreach ($project in $projects) {
    Write-Host "  Building $project..." -ForegroundColor Yellow
    dotnet build "$project\$project.csproj" --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ $project built successfully" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $project build failed" -ForegroundColor Red
    }
}

Set-Location ..
Write-Host ""

# ============================================================================
# Summary
# ============================================================================
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Update PostgreSQL password in local.settings.json files" -ForegroundColor White
Write-Host "     - src\PhoneticAnalyzers.SQLDBFirst.Functions.Search\local.settings.json" -ForegroundColor Gray
Write-Host "     - src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\local.settings.json" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Start the Search Function:" -ForegroundColor White
Write-Host "     cd src\PhoneticAnalyzers.SQLDBFirst.Functions.Search" -ForegroundColor Gray
Write-Host "     func start --port 7072" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Start the Ingestion Function (in another terminal):" -ForegroundColor White
Write-Host "     cd src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion" -ForegroundColor Gray
Write-Host "     func start --port 7073" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Update WebUI to point to Database-First endpoint:" -ForegroundColor White
Write-Host "     Update WebUI\wwwroot\appsettings.json:" -ForegroundColor Gray
Write-Host '     "ApiBaseUrl": "http://localhost:7072/api"' -ForegroundColor Gray
Write-Host ""
Write-Host "Database Connection String:" -ForegroundColor Yellow
Write-Host "  $connectionString" -ForegroundColor Cyan
Write-Host ""

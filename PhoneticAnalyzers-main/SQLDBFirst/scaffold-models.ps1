# ============================================================================
# Scaffold EF Core Models from PostgreSQL Database
# Database: phonetic_db_dbfirst
# Date: 2025-11-12
# Author: Development Team
# Description: Regenerates C# entity models from existing database schema
# ============================================================================

# Prerequisites:
# - .NET 8 SDK installed
# - Npgsql.EntityFrameworkCore.PostgreSQL package
# - dotnet-ef tools: dotnet tool install --global dotnet-ef

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "EF Core Model Scaffolding" -ForegroundColor Cyan
Write-Host "Database-First Approach" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$DatabaseName = "phonetic_db_dbfirst"
$Host = "localhost"
$Username = "postgres"
$OutputDir = "Models"
$ContextName = "PhoneticDbContext"
$Namespace = "PhoneticAnalyzers.SQLDBFirst.Models"

# Prompt for password securely
$SecurePassword = Read-Host "Enter PostgreSQL password for user '$Username'" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
$Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Build connection string
$ConnectionString = "Host=$Host;Database=$DatabaseName;Username=$Username;Password=$Password"

Write-Host "Connecting to database: $DatabaseName" -ForegroundColor Yellow
Write-Host "Output directory: $OutputDir" -ForegroundColor Yellow
Write-Host "Context name: $ContextName" -ForegroundColor Yellow
Write-Host ""

# Check if dotnet-ef is installed
Write-Host "Checking for dotnet-ef tools..." -ForegroundColor Yellow
$efInstalled = dotnet tool list --global | Select-String "dotnet-ef"

if (-not $efInstalled) {
    Write-Host "dotnet-ef not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    Write-Host "dotnet-ef installed successfully!" -ForegroundColor Green
} else {
    Write-Host "dotnet-ef is already installed" -ForegroundColor Green
}

Write-Host ""

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "Created output directory: $OutputDir" -ForegroundColor Green
}

# Warning about existing files
if (Test-Path "$OutputDir\*.cs") {
    Write-Host "WARNING: Existing .cs files in $OutputDir will be overwritten!" -ForegroundColor Red
    $confirmation = Read-Host "Continue? (Y/N)"
    if ($confirmation -ne 'Y' -and $confirmation -ne 'y') {
        Write-Host "Scaffolding cancelled." -ForegroundColor Yellow
        exit
    }
}

Write-Host ""
Write-Host "Starting scaffold process..." -ForegroundColor Cyan
Write-Host ""

# Scaffold command
# Note: -Force will overwrite existing files
$scaffoldCommand = @"
dotnet ef dbcontext scaffold "$ConnectionString" Npgsql.EntityFrameworkCore.PostgreSQL \
    --output-dir $OutputDir \
    --context $ContextName \
    --context-dir $OutputDir \
    --namespace $Namespace \
    --force \
    --no-onconfiguring \
    --data-annotations
"@

Write-Host "Executing: dotnet ef dbcontext scaffold..." -ForegroundColor Yellow
Write-Host ""

try {
    # Execute scaffold
    dotnet ef dbcontext scaffold $ConnectionString `
        Npgsql.EntityFrameworkCore.PostgreSQL `
        --output-dir $OutputDir `
        --context $ContextName `
        --context-dir $OutputDir `
        --namespace $Namespace `
        --force `
        --no-onconfiguring `
        --data-annotations

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Scaffolding completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        
        # List generated files
        Write-Host "Generated files:" -ForegroundColor Cyan
        Get-ChildItem -Path $OutputDir -Filter "*.cs" | ForEach-Object {
            Write-Host "  ✓ $_" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "Models generated in: $OutputDir" -ForegroundColor Yellow
        Write-Host "DbContext: $OutputDir\$ContextName.cs" -ForegroundColor Yellow
        
        Write-Host ""
        Write-Host "IMPORTANT NOTES:" -ForegroundColor Cyan
        Write-Host "1. Review generated models for accuracy" -ForegroundColor White
        Write-Host "2. Add partial class files for custom logic (won't be overwritten)" -ForegroundColor White
        Write-Host "3. Update connection string in appsettings.json" -ForegroundColor White
        Write-Host "4. Register DbContext in Program.cs/Startup.cs" -ForegroundColor White
        Write-Host ""
        Write-Host "Example DbContext registration:" -ForegroundColor Yellow
        Write-Host "  services.AddDbContext<PhoneticDbContext>(options =>" -ForegroundColor Gray
        Write-Host "      options.UseNpgsql(Configuration.GetConnectionString(""PostgresConnection"")));" -ForegroundColor Gray
        Write-Host ""
        
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "Scaffolding FAILED!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Common issues:" -ForegroundColor Yellow
        Write-Host "1. Database does not exist - Run 001_CreateSchema.sql first" -ForegroundColor White
        Write-Host "2. Incorrect password - Check PostgreSQL credentials" -ForegroundColor White
        Write-Host "3. PostgreSQL not running - Start PostgreSQL service" -ForegroundColor White
        Write-Host "4. Missing NuGet package - Run: dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL" -ForegroundColor White
        exit 1
    }
    
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Error during scaffolding!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Security: Clear password from memory
$Password = $null
[System.GC]::Collect()

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Review generated models in $OutputDir\" -ForegroundColor White
Write-Host "2. Create partial class files for custom business logic" -ForegroundColor White
Write-Host "3. Build your application layer using these models" -ForegroundColor White
Write-Host "4. Implement repositories and services" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

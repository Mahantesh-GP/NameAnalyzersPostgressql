param(
    [string]$DbHost,
    [int]$DbPort,
    [string]$DbName,
    [string]$DbUser,
    [string]$DbPassword
)

$ErrorActionPreference = 'Stop'

# Find psql executable
$psqlPath = $null
if (Get-Command psql -ErrorAction SilentlyContinue) {
    $psqlPath = "psql"
} elseif (Test-Path "C:\Program Files\PostgreSQL\17\bin\psql.exe") {
    $psqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
} elseif (Test-Path "C:\Program Files\PostgreSQL\16\bin\psql.exe") {
    $psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
} else {
    Write-Error "psql not found. Please install PostgreSQL client tools or add psql to PATH."
    exit 1
}

if (-not $DbHost) { $DbHost = 'localhost' }
if (-not $DbPort) { $DbPort = 5432 }
if (-not $DbName) { $DbName = 'phonetic_native' }
if (-not $DbUser) { $DbUser = 'postgres' }
if (-not $DbPassword) { $DbPassword = 'postgres' }

# Set password for psql child processes
$prevPwd = $env:PGPASSWORD
$env:PGPASSWORD = $DbPassword

try {
    Write-Host "Ensuring database '$DbName' exists..." -ForegroundColor Cyan
    try {
        & $psqlPath -h $DbHost -p $DbPort -U $DbUser -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"$DbName\";" | Out-Null
        Write-Host "Database '$DbName' created." -ForegroundColor Green
    } catch {
        Write-Host "Database '$DbName' may already exist. Continuing..." -ForegroundColor Yellow
    }

    $sqlDir = Resolve-Path (Join-Path $PSScriptRoot '..\sql')

    $filesInOrder = @(
        '01_extensions.sql',
        '02_schema.sql',
        '03_indexes.sql',
        '04_functions.sql',
        '05_search.sql',
        '06_staging.sql',
        '07_nickname_tracking.sql',
        '08_apply_nicknames_bulk.sql',
        '09_business_enhancements.sql'
    )

    foreach ($file in $filesInOrder) {
        $path = Join-Path $sqlDir $file
        if (-not (Test-Path $path)) {
            Write-Error "Missing SQL file: $path"
            exit 1
        }
        Write-Host "Applying $file..." -ForegroundColor Cyan
        & $psqlPath -h $DbHost -p $DbPort -U $DbUser -d $DbName -v ON_ERROR_STOP=1 -f $path
    }

    Write-Host "Deployment completed. All SQL scripts applied." -ForegroundColor Green
}
finally {
    $env:PGPASSWORD = $prevPwd
}
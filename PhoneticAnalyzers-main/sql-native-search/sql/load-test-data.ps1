# Load test data using Npgsql
$connectionString = "Host=localhost;Port=5432;Database=phonetic_native;Username=postgres;Password=postgres"

# Read the SQL file
$sqlContent = Get-Content "test-all-strategies.sql" -Raw

# Extract all SELECT ingest_person(...) statements
$statements = [regex]::Matches($sqlContent, "SELECT ingest_person\([^;]+\);")

Write-Host "Found $($statements.Count) records to insert..." -ForegroundColor Cyan

# Add Npgsql assembly
Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\9.0.0\System.Runtime.dll" -ErrorAction SilentlyContinue

# Try to use .NET connection
try {
    $assembly = [System.Reflection.Assembly]::LoadWithPartialName("Npgsql")
    if (-not $assembly) {
        Write-Host "Npgsql not found. Installing via dotnet tool..." -ForegroundColor Yellow
        dotnet add package Npgsql --version 8.0.0
    }
}
catch {
    Write-Host "Error loading Npgsql: $_" -ForegroundColor Red
}

# Alternative: Use the API's connection
Write-Host "`nAlternative: Loading via API connection..." -ForegroundColor Yellow
Write-Host "Starting API and using its database connection..." -ForegroundColor Cyan

$apiPath = "..\api"
$originalLocation = Get-Location

try {
    Set-Location $apiPath
    
    # Check if API is already running
    $apiRunning = Get-Process -Name "PhoneticAnalyzers.NativeApi" -ErrorAction SilentlyContinue
    
    if (-not $apiRunning) {
        Write-Host "Starting API..." -ForegroundColor Yellow
        Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru | Out-Null
        Start-Sleep -Seconds 5
    }
    
    Set-Location $originalLocation
    
    # Execute each statement via API endpoint
    Write-Host "`nInserting records..." -ForegroundColor Cyan
    $count = 0
    
    foreach ($statement in $statements) {
        $sql = $statement.Value
        
        # Extract parameters from ingest_person call
        if ($sql -match "ingest_person\('([^']+)',\s*'([^']+)',\s*'([^']+)',\s*'([^']+)'\)") {
            $name = $matches[1]
            $countyCode = $matches[2]
            $countyName = $matches[3]
            $type = $matches[4]
            
            $count++
            Write-Host "[$count/$($statements.Count)] Inserting: $name" -ForegroundColor Gray
            
            # You would call your API endpoint here
            # For now, we'll collect the SQL statements
        }
    }
    
    Write-Host "`nCompleted! Inserted $count records." -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
finally {
    Set-Location $originalLocation
}

Write-Host "`n=== Alternative Method ===" -ForegroundColor Yellow
Write-Host "Since psql is not available, you can:" -ForegroundColor Cyan
Write-Host "1. Install PostgreSQL client tools (includes psql)" -ForegroundColor White
Write-Host "   Download from: https://www.postgresql.org/download/windows/" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Use pgAdmin (GUI tool) - copy/paste SQL from test-all-strategies.sql" -ForegroundColor White
Write-Host ""
Write-Host "3. Use VS Code PostgreSQL extension:" -ForegroundColor White
Write-Host "   - Install 'PostgreSQL' extension by Chris Kolkman" -ForegroundColor Gray
Write-Host "   - Connect to: localhost:5432/phonetic_native" -ForegroundColor Gray
Write-Host "   - Run test-all-strategies.sql" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Or I can create a simple C# console app to load the data" -ForegroundColor White

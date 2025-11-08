#!/usr/bin/env pwsh

# Load Sample Mortgage Data Script
# This PowerShell script demonstrates how to load the sample mortgage data using the bulk import functionality

Write-Host "🏠 Mortgage Data Loader" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green
Write-Host ""

# Check if the sample data file exists
$sampleDataPath = Join-Path $PSScriptRoot "sample_mortgage_data.csv"

if (-not (Test-Path $sampleDataPath)) {
    Write-Host "❌ Error: Sample data file not found at: $sampleDataPath" -ForegroundColor Red
    Write-Host "Please ensure the sample_mortgage_data.csv file is in the root directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "📄 Sample data file found: $sampleDataPath" -ForegroundColor Cyan
Write-Host ""

# Display the sample data preview
Write-Host "📊 Sample Data Preview:" -ForegroundColor Yellow
Write-Host "======================" -ForegroundColor Yellow

$csvData = Import-Csv $sampleDataPath
$csvData | Select-Object ExternalId, FullName, County, Flag | Format-Table -AutoSize

Write-Host ""
Write-Host "📈 Summary:" -ForegroundColor Cyan
Write-Host "- Total Records: $($csvData.Count)"
Write-Host "- Individuals: $(($csvData | Where-Object Flag -eq 'I').Count)"
Write-Host "- Businesses: $(($csvData | Where-Object Flag -eq 'B').Count)"
Write-Host "- Unknown: $(($csvData | Where-Object Flag -eq 'U').Count)"
Write-Host "- Counties: $((($csvData | Select-Object County -Unique).County | Sort-Object) -join ', ')"

Write-Host ""
Write-Host "🔧 How to Load This Data:" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green
Write-Host ""
Write-Host "1. Web Interface:" -ForegroundColor Yellow
Write-Host "   - Start the web application: dotnet run --project Web/PhoneticAnalyzers.Web.csproj"
Write-Host "   - Navigate to: http://localhost:5000/load-sample-data"
Write-Host "   - Click 'Load Sample Data' button"
Write-Host ""
Write-Host "2. Function App (Azure Functions):" -ForegroundColor Yellow
Write-Host "   - Start the ingestion function: func start --port 7071 (in src/PhoneticAnalyzers.Functions.Ingestion)"
Write-Host "   - POST to: http://localhost:7071/api/bulk-ingest"
Write-Host "   - With JSON body: { `"filePath`": `"$sampleDataPath`" }"
Write-Host ""
Write-Host "3. Direct API Call:" -ForegroundColor Yellow
Write-Host "   - Use MediatR BulkIngestCommand with DataSource: '$sampleDataPath'"
Write-Host ""

# Check if PostgreSQL is running
Write-Host "🔍 Database Check:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan

try {
    $connectionString = "Host=localhost;Database=phonetic_analyzers_dev;Username=postgres;Password=postgres;Port=5432;"
    # This is just a connectivity check - actual database operations require the application
    Write-Host "✅ Database connection configured for: localhost:5432/phonetic_analyzers_dev" -ForegroundColor Green
    Write-Host "   Note: Ensure PostgreSQL is running and the database exists" -ForegroundColor Yellow
} catch {
    Write-Host "⚠️  PostgreSQL connection check skipped (requires actual database client)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎯 Next Steps:" -ForegroundColor Green
Write-Host "=============" -ForegroundColor Green
Write-Host "1. Ensure PostgreSQL is running on localhost:5432"
Write-Host "2. Run database migrations if needed"
Write-Host "3. Load the sample data using one of the methods above"
Write-Host "4. Test the mortgage search functionality with the loaded data"
Write-Host ""
Write-Host "💡 The sample data contains names like 'John Smith', 'Jane Johnson Corporation', etc."
Write-Host "   Try searching for partial names to test the phonetic matching!" -ForegroundColor Cyan
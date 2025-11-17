# Test script for Native API
$ErrorActionPreference = 'Stop'

$baseUrl = "http://localhost:5100"

Write-Host "`n=== Testing Native SQL API ===" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl`n" -ForegroundColor Yellow

# Test 1: Health Check
Write-Host "[1/4] Testing Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/ingest/health" -Method Get
    Write-Host "✓ Health: $($health.status)" -ForegroundColor Green
} catch {
    Write-Host "✗ Health check failed: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Ingest Person
Write-Host "`n[2/4] Testing Single Ingest..." -ForegroundColor Yellow
$ingestBody = @{
    externalId = "TEST-$(Get-Random)"
    fullName = "John Davis"
    county = "TestCounty"
    flag = "I"
} | ConvertTo-Json

try {
    $ingest = Invoke-RestMethod -Uri "$baseUrl/api/ingest" -Method Post -Body $ingestBody -ContentType "application/json"
    Write-Host "✓ Ingested: PersonId=$($ingest.personId), Name=$($ingest.fullName)" -ForegroundColor Green
    $testPersonId = $ingest.personId
} catch {
    Write-Host "✗ Ingest failed: $_" -ForegroundColor Red
    exit 1
}

# Test 3: Search
Write-Host "`n[3/4] Testing Search..." -ForegroundColor Yellow
try {
    $search = Invoke-RestMethod -Uri "$baseUrl/api/search?queryName=john%20davis&maxResults=10" -Method Get
    Write-Host "✓ Search returned $($search.totalResults) results in $($search.executionTimeMs)ms" -ForegroundColor Green
    
    if ($search.results.Count -gt 0) {
        Write-Host "  Top match: $($search.results[0].fullName) (Score: $($search.results[0].similarityScore), Type: $($search.results[0].matchType))" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Search failed: $_" -ForegroundColor Red
    exit 1
}

# Test 4: Get by ID
Write-Host "`n[4/4] Testing Get by ID..." -ForegroundColor Yellow
if ($testPersonId) {
    try {
        $person = Invoke-RestMethod -Uri "$baseUrl/api/search/$testPersonId" -Method Get
        Write-Host "✓ Retrieved: $($person.fullName) (ID: $($person.personId))" -ForegroundColor Green
    } catch {
        Write-Host "✗ Get by ID failed: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n=== All Tests Passed! ===" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Open Swagger UI: $baseUrl/swagger" -ForegroundColor White
Write-Host "  2. Point your UI to this API (see QUICKSTART.md)" -ForegroundColor White
Write-Host "  3. For bulk loading, see QUICKSTART.md bulk section`n" -ForegroundColor White

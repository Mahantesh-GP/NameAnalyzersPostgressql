# Quick Start Guide - Nickname Enrichment

Write-Host "=== Nickname Enrichment Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if appsettings.json exists
if (-not (Test-Path "appsettings.json")) {
    Write-Host "✗ appsettings.json not found!" -ForegroundColor Red
    Write-Host "Creating default configuration..." -ForegroundColor Yellow
    
    $defaultConfig = @{
        Database = @{
            ConnectionString = "Host=localhost;Database=phonetic_native;Username=postgres;Password=postgres"
        }
        LLM = @{
            Provider = "Ollama"
            Endpoint = "http://localhost:11434/api/generate"
            ApiKey = ""
            Model = "llama3.2:latest"
            Temperature = 0.3
        }
    } | ConvertTo-Json -Depth 10
    
    $defaultConfig | Out-File "appsettings.json" -Encoding UTF8
    Write-Host "✓ Created appsettings.json with Ollama configuration" -ForegroundColor Green
}

Write-Host ""
Write-Host "Configuration Options:" -ForegroundColor Yellow
Write-Host "1. Ollama (local, free) - Default"
Write-Host "2. Azure OpenAI (cloud, requires API key)"
Write-Host ""

$choice = Read-Host "Which provider do you want to use? (1/2)"

if ($choice -eq "2") {
    Write-Host ""
    Write-Host "Azure OpenAI Configuration" -ForegroundColor Cyan
    Write-Host "Enter your Azure OpenAI details:" -ForegroundColor Yellow
    
    $resourceName = Read-Host "Azure Resource Name"
    $deploymentName = Read-Host "Deployment Name (e.g., gpt-4)"
    $apiKey = Read-Host "API Key" -AsSecureString
    $apiKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
    
    $endpoint = "https://$resourceName.openai.azure.com/openai/deployments/$deploymentName/chat/completions?api-version=2024-02-15-preview"
    
    $config = @{
        Database = @{
            ConnectionString = "Host=localhost;Database=phonetic_native;Username=postgres;Password=postgres"
        }
        LLM = @{
            Provider = "AzureOpenAI"
            Endpoint = $endpoint
            ApiKey = $apiKeyPlain
            Model = $deploymentName
            Temperature = 0.3
        }
    } | ConvertTo-Json -Depth 10
    
    $config | Out-File "appsettings.json" -Encoding UTF8
    Write-Host "✓ Configuration saved!" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "Using Ollama (local)" -ForegroundColor Green
    Write-Host "Make sure Ollama is running: ollama serve" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Building project..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Build successful!" -ForegroundColor Green
    Write-Host ""
    $run = Read-Host "Do you want to run the enrichment now? (y/n)"
    
    if ($run -eq "y") {
        Write-Host ""
        Write-Host "Starting enrichment..." -ForegroundColor Cyan
        dotnet run
    }
    else {
        Write-Host ""
        Write-Host "To run later, use: dotnet run" -ForegroundColor Yellow
    }
}
else {
    Write-Host ""
    Write-Host "✗ Build failed. Please check the errors above." -ForegroundColor Red
}

# Nickname Enrichment Tool

A tool to enrich the `nickname_map` table using LLM (Large Language Model) for generating nickname variants. Supports both **Ollama** (local) and **Azure OpenAI**.

## Features

- ✅ Extract unique first names from your database
- ✅ Generate nickname variants using LLM
- ✅ Populate `nickname_map` table automatically
- ✅ Support for **Ollama** (local, free)
- ✅ Support for **Azure OpenAI** (cloud, requires API key)
- ✅ Configurable via `appsettings.json`
- ✅ Progress tracking and error handling

## Prerequisites

### For Ollama (Local)
- Install [Ollama](https://ollama.ai/)
- Pull a model: `ollama pull llama3.2:latest`
- Start Ollama: `ollama serve`

### For Azure OpenAI
- Azure OpenAI resource with a deployed model (GPT-4 or GPT-3.5-Turbo)
- API key from Azure Portal
- Deployment endpoint URL

## Configuration

### Option 1: Using Ollama (Local)

Edit `appsettings.json`:
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=phonetic_native;Username=postgres;Password=YOUR_PASSWORD"
  },
  "LLM": {
    "Provider": "Ollama",
    "Endpoint": "http://localhost:11434/api/generate",
    "ApiKey": "",
    "Model": "llama3.2:latest",
    "Temperature": 0.3
  }
}
```

### Option 2: Using Azure OpenAI

Edit `appsettings.json`:
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=phonetic_native;Username=postgres;Password=YOUR_PASSWORD"
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://YOUR-RESOURCE-NAME.openai.azure.com/openai/deployments/YOUR-DEPLOYMENT-NAME/chat/completions?api-version=2024-02-15-preview",
    "ApiKey": "YOUR-AZURE-OPENAI-API-KEY",
    "Model": "gpt-4",
    "Temperature": 0.3
  }
}
```

**Azure Endpoint Format:**
```
https://{resource-name}.openai.azure.com/openai/deployments/{deployment-name}/chat/completions?api-version=2024-02-15-preview
```

Replace:
- `{resource-name}`: Your Azure OpenAI resource name
- `{deployment-name}`: Your model deployment name (e.g., `gpt-4`, `gpt-35-turbo`)

## Usage

### 1. Build the project
```powershell
cd tools\NicknameEnrichment
dotnet build
```

### 2. Run the tool
```powershell
dotnet run
```

### 3. Monitor progress
The tool will:
- Fetch all unique first names from `person_names` table
- Call LLM for each name to get nickname variants
- Insert mappings into `nickname_map` table
- Display progress every 10 names

Example output:
```
=== Nickname Enrichment Tool ===

Provider: AzureOpenAI
Endpoint: https://myresource.openai.azure.com/openai/deployments/gpt-4/...
Model: gpt-4
Temperature: 0.3
Connecting to database...

Fetching unique first names...
Found 1247 unique first names
Processing 'Robert'... Added 5 nicknames
Processing 'William'... Added 6 nicknames
Processing 'Elizabeth'... Added 8 nicknames
...
Progress: 100/1247 (87 enriched)
...
✓ Enrichment completed successfully!
Completed: 1247 names processed, 987 enriched
```

## Performance

- **Ollama (local)**: ~2-5 seconds per name
- **Azure OpenAI**: ~1-2 seconds per name
- **Total time for 1000 names**: 30-90 minutes (with rate limiting)

## Rate Limiting

The tool includes a 500ms delay between requests to avoid overwhelming the LLM. Adjust in `NicknameEnrichmentService.cs`:

```csharp
await Task.Delay(500); // Change to 1000 for slower rate
```

## Troubleshooting

### Error: "Connection refused"
- **Ollama**: Make sure `ollama serve` is running
- **Azure OpenAI**: Check your endpoint URL and API key

### Error: "Model not found"
- **Ollama**: Run `ollama pull llama3.2:latest`
- **Azure OpenAI**: Verify your deployment name in the endpoint URL

### LLM returns empty results
- Lower the temperature (e.g., 0.1 for more deterministic)
- Try a different model
- Check the LLM prompt in `NicknameEnrichmentService.cs`

### Database connection error
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Ensure database `phonetic_native` exists

## After Enrichment

Once `nickname_map` is populated, run the nickname expansion:

```powershell
$env:PGPASSWORD='postgres'
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' -h localhost -U postgres -d phonetic_native -f "..\..\sql-native-search\sql\08_apply_nicknames_bulk.sql"
```

This will generate nickname tokens for all existing persons in the database.

## Cost Estimates (Azure OpenAI)

### GPT-4
- ~$0.03 per 1K input tokens
- ~100 tokens per request
- 1000 names ≈ $3-5

### GPT-3.5-Turbo
- ~$0.001 per 1K input tokens
- ~100 tokens per request
- 1000 names ≈ $0.10-0.20

**Ollama is free!** Recommended for development and testing.

## Example Nickname Mappings

The tool generates mappings like:
```
robert → bob, bobby, robby, rob
william → bill, billy, will, willie, liam
elizabeth → liz, lizzy, beth, betty, eliza
```

These are used during search to expand queries:
- Search "bob smith" → Matches "Robert Smith"
- Search "liz johnson" → Matches "Elizabeth Johnson"

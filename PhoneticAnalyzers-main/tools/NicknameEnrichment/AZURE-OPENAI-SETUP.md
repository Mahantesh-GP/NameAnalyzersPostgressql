# Nickname Enrichment - Azure OpenAI Integration

## ✅ What's Been Added

The Nickname Enrichment tool now supports **both Ollama (local) and Azure OpenAI**!

### New Features:
1. **Dual LLM Provider Support**
   - Ollama (local, free, offline)
   - Azure OpenAI (cloud, requires API key)

2. **Configuration-Based Setup**
   - `appsettings.json` for easy configuration
   - No code changes needed to switch providers

3. **Robust JSON Parsing**
   - Handles markdown-wrapped responses
   - Extracts JSON arrays from various formats
   - Error-tolerant parsing

## 📁 Files Created/Modified

### New Files:
- `appsettings.json` - Default Ollama configuration
- `appsettings.AzureOpenAI.json` - Example Azure OpenAI configuration
- `README.md` - Complete usage documentation
- `setup.ps1` - Interactive setup script

### Modified Files:
- `NicknameEnrichmentService.cs` - Added Azure OpenAI support
- `Program.cs` - Configuration-based initialization
- `NicknameEnrichment.csproj` - Added required packages

## 🚀 Quick Start

### Option 1: Ollama (Local, Free)

```powershell
cd tools\NicknameEnrichment

# Edit appsettings.json (already configured for Ollama)
# Make sure Ollama is running
ollama serve

# Run the tool
dotnet run
```

### Option 2: Azure OpenAI

```powershell
cd tools\NicknameEnrichment

# Edit appsettings.json
notepad appsettings.json
```

Change to:
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=phonetic_native;Username=postgres;Password=YOUR_PASSWORD"
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/openai/deployments/YOUR-DEPLOYMENT/chat/completions?api-version=2024-02-15-preview",
    "ApiKey": "YOUR-API-KEY",
    "Model": "gpt-4",
    "Temperature": 0.3
  }
}
```

Then run:
```powershell
dotnet run
```

## 🔧 Azure OpenAI Setup

### Get Your Endpoint URL:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Click "Keys and Endpoint"
4. Copy the endpoint and key

### Endpoint Format:
```
https://{resource-name}.openai.azure.com/openai/deployments/{deployment-name}/chat/completions?api-version=2024-02-15-preview
```

Example:
```
https://mycompany-openai.openai.azure.com/openai/deployments/gpt-4/chat/completions?api-version=2024-02-15-preview
```

### Models Supported:
- `gpt-4` (best quality, higher cost)
- `gpt-4-turbo` (faster, lower cost)
- `gpt-35-turbo` (fastest, lowest cost)

## 💰 Cost Comparison

### Ollama (Local):
- **Cost**: FREE
- **Speed**: 2-5 seconds per name
- **Requirement**: Local GPU/CPU resources

### Azure OpenAI:
- **GPT-4**: ~$3-5 per 1000 names
- **GPT-3.5-Turbo**: ~$0.10-0.20 per 1000 names
- **Speed**: 1-2 seconds per name
- **Requirement**: Azure subscription + API key

## 📊 Example Output

```
=== Nickname Enrichment Tool ===

Provider: AzureOpenAI
Endpoint: https://mycompany.openai.azure.com/openai/deployments/gpt-4/...
Model: gpt-4
Temperature: 0.3
Connecting to database...

Fetching unique first names...
Found 1247 unique first names
Processing 'Robert'... Added 5 nicknames
Processing 'William'... Added 6 nicknames
Processing 'Elizabeth'... Added 8 nicknames
Processing 'Michael'... Added 4 nicknames
...
Progress: 100/1247 (87 enriched)
...
✓ Enrichment completed successfully!
Completed: 1247 names processed, 987 enriched
```

## 🔍 How It Works

### 1. Extract Names
```sql
SELECT DISTINCT name_token 
FROM person_names 
WHERE token_position = 1 AND is_nickname = FALSE
```

### 2. Call LLM
**Azure OpenAI Request:**
```json
{
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful assistant that provides nickname variants..."
    },
    {
      "role": "user",
      "content": "Given the name 'Robert', provide all common nickname variants..."
    }
  ]
}
```

**Response:**
```json
["bob", "bobby", "robby", "rob"]
```

### 3. Insert Mappings
```sql
INSERT INTO nickname_map (normalized_original, normalized_nickname)
VALUES ('robert', 'bob'), ('robert', 'bobby'), ...
```

### 4. Apply to Existing Data
```powershell
psql -f sql\08_apply_nicknames_bulk.sql
```

## 🛠️ Troubleshooting

### Azure OpenAI Errors

**"Unauthorized" (401)**
- Check your API key
- Verify it's added to request headers

**"Resource not found" (404)**
- Verify endpoint URL format
- Check deployment name matches

**"Rate limit exceeded" (429)**
- Increase delay in code: `await Task.Delay(1000)`
- Use lower tier model (gpt-35-turbo)

### Ollama Errors

**"Connection refused"**
- Start Ollama: `ollama serve`
- Check port 11434 is available

**"Model not found"**
- Pull model: `ollama pull llama3.2:latest`
- List models: `ollama list`

## 📚 Additional Resources

- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Ollama Documentation](https://ollama.ai/docs)
- [PostgreSQL Nickname Map Schema](../../sql-native-search/sql/02_schema.sql)

## ✨ Next Steps

After running the enrichment:

1. **Verify Mappings:**
   ```sql
   SELECT * FROM nickname_map LIMIT 10;
   ```

2. **Apply to Persons:**
   ```powershell
   psql -f ../../sql-native-search/sql/08_apply_nicknames_bulk.sql
   ```

3. **Test Search:**
   ```sql
   SELECT * FROM search_persons('bob smith', 50, 0.6);
   ```

4. **Check UI:**
   - Search "bob" should find "Robert"
   - Match explanation shows "bob → robert (nickname)"

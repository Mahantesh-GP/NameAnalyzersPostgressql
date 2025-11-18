# Nickname Enrichment Troubleshooting Guide

## Common Issues and Solutions

### 1. "Input does not contain any JSON tokens"

**Cause:** LLM returned invalid or empty response

**Solutions:**

#### For Ollama (Local)
```csharp
var llmConfig = new LLMConfiguration
{
    Provider = LLMProvider.Ollama,
    Endpoint = "http://localhost:11434/api/generate",
    Model = "llama3.2:latest",  // or "llama3.1", "mistral"
    Temperature = 0.3
};
```

**Check Ollama is running:**
```powershell
# Test Ollama endpoint
curl http://localhost:11434/api/generate -Method POST -Body '{"model":"llama3.2:latest","prompt":"test","stream":false}' -ContentType "application/json"
```

**Common fixes:**
- ✅ Make sure Ollama is running: `ollama serve`
- ✅ Pull the model: `ollama pull llama3.2:latest`
- ✅ Try a different model: `llama3.1`, `mistral`, `gemma2`
- ✅ Reduce batch size: Use `batchSize: 50` instead of 100

#### For Azure OpenAI
```csharp
var llmConfig = new LLMConfiguration
{
    Provider = LLMProvider.AzureOpenAI,
    Endpoint = "https://YOUR_RESOURCE.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT/chat/completions?api-version=2024-08-01-preview",
    ApiKey = "your-api-key-here",
    Model = "gpt-4o-mini",  // or "gpt-4o"
    Temperature = 0.3
};
```

**Common fixes:**
- ✅ Verify API key is correct
- ✅ Check endpoint URL format
- ✅ Ensure deployment name matches
- ✅ Test with Postman/curl first

---

### 2. Network/Timeout Errors

**Symptoms:**
- `HttpRequestException: The operation was canceled`
- `TaskCanceledException`
- `Connection refused`

**Solutions:**

```csharp
// Service already has 5-minute timeout
// If still timing out, reduce batch size:
await service.EnrichAllNicknamesAsync(batchSize: 25);
```

**For Ollama:**
- Increase Ollama timeout: `OLLAMA_REQUEST_TIMEOUT=300 ollama serve`
- Check firewall settings
- Verify localhost:11434 is accessible

**For Azure OpenAI:**
- Check internet connection
- Verify no proxy/VPN blocking requests
- Check Azure service health status

---

### 3. Empty or No Nicknames Found

**Symptoms:**
- "⚠ No nicknames found in this batch"
- All batches return 0 nicknames

**Possible causes:**
1. **Model not following instructions**
   - Try different model (e.g., gpt-4o instead of gpt-3.5)
   - Reduce batch size to 25-50 names
   
2. **Names don't have common nicknames**
   - Expected for uncommon or non-English names
   - System correctly returns empty arrays

3. **Response format issues**
   - Check console output for "Content preview"
   - If seeing text instead of JSON, model needs better prompting

**Quick test:**
```csharp
// Test with single batch of common names
var testNames = new List<string> { "ROBERT", "WILLIAM", "ELIZABETH" };
var results = await service.GetNicknamesFromLLMBatchAsync(testNames);
Console.WriteLine($"Results: {results.Count}");
```

---

### 4. Database Connection Errors

**Symptoms:**
- `Npgsql.NpgsqlException: Connection refused`
- `Password authentication failed`

**Solutions:**

```csharp
// Verify connection string format
var connectionString = "Host=localhost;Database=phonetic_native;Username=postgres;Password=yourpassword;Port=5432";
```

**Check PostgreSQL:**
```powershell
# Test connection
psql -U postgres -d phonetic_native -c "SELECT COUNT(*) FROM person;"

# Verify normalize_name function exists
psql -U postgres -d phonetic_native -c "\df normalize_name"
```

---

### 5. Performance Issues

**Symptoms:**
- Taking too long (>10 minutes for 5000 names)
- High CPU usage
- Memory issues

**Optimizations:**

#### Reduce Batch Size
```csharp
// For Ollama on older hardware
await service.EnrichAllNicknamesAsync(batchSize: 25);

// For Azure OpenAI (faster)
await service.EnrichAllNicknamesAsync(batchSize: 100);
```

#### Parallel Processing (Advanced)
```csharp
// Process multiple batches in parallel (use with caution)
var tasks = batches.Select(async batch => 
{
    var results = await service.GetNicknamesFromLLMBatchAsync(batch);
    await service.InsertNicknamesBatchAsync(results);
});
await Task.WhenAll(tasks);
```

**Note:** Parallel processing can overwhelm local Ollama instances. Recommended only for Azure OpenAI.

---

## Recommended Settings by Environment

### Local Development (Ollama)
```csharp
var llmConfig = new LLMConfiguration
{
    Provider = LLMProvider.Ollama,
    Endpoint = "http://localhost:11434/api/generate",
    Model = "llama3.2:latest",
    Temperature = 0.3
};

await service.EnrichAllNicknamesAsync(batchSize: 50);
```

**Expected time:** 5-10 minutes for 5000 names (50 batches)

---

### Production (Azure OpenAI)
```csharp
var llmConfig = new LLMConfiguration
{
    Provider = LLMProvider.AzureOpenAI,
    Endpoint = "https://YOUR_RESOURCE.openai.azure.com/openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-08-01-preview",
    ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"),
    Model = "gpt-4o-mini",
    Temperature = 0.3
};

await service.EnrichAllNicknamesAsync(batchSize: 100);
```

**Expected time:** 3-5 minutes for 5000 names (50 batches)  
**Expected cost:** $0.10-0.15 (GPT-4o-mini)

---

## Debugging Steps

### 1. Enable Verbose Logging
The service now shows:
- Provider and model being used
- Endpoint URL
- Each batch being processed
- Sample results from each batch
- Content preview on errors

### 2. Test Single Batch
```csharp
var testBatch = new List<string> { "ROBERT", "WILLIAM", "ELIZABETH" };
var results = await service.GetNicknamesFromLLMBatchAsync(testBatch);

foreach (var (name, nicknames) in results)
{
    Console.WriteLine($"{name}: {string.Join(", ", nicknames)}");
}
```

### 3. Check Response Format
Look for console output like:
```
⚠ Content preview: The nicknames are Robert: Bob, Rob...
```

If you see plain text instead of JSON, the model isn't following instructions.

### 4. Verify Database State
```sql
-- Check how many nicknames exist
SELECT COUNT(*) FROM nickname_map;

-- See sample nicknames
SELECT * FROM nickname_map LIMIT 10;

-- Check for duplicates
SELECT normalized_original, normalized_nickname, COUNT(*) 
FROM nickname_map 
GROUP BY normalized_original, normalized_nickname 
HAVING COUNT(*) > 1;
```

---

## Recovery from Failed Runs

The service uses `ON CONFLICT DO NOTHING`, so it's safe to re-run:

```csharp
// Safe to run multiple times - won't create duplicates
await service.EnrichAllNicknamesAsync(batchSize: 100);
```

To start over completely:
```sql
-- Clear all nicknames
TRUNCATE TABLE nickname_map;

-- Verify
SELECT COUNT(*) FROM nickname_map;  -- Should be 0
```

---

## Getting Help

If issues persist:

1. **Check console output** for error messages and content previews
2. **Test LLM endpoint directly** with curl/Postman
3. **Verify database connectivity** with psql
4. **Try smaller batch size** (25-50 instead of 100)
5. **Try different model** if using Ollama

## Support Information

- **Ollama Models:** https://ollama.com/library
- **Azure OpenAI:** https://learn.microsoft.com/azure/ai-services/openai/
- **PostgreSQL:** https://www.postgresql.org/docs/

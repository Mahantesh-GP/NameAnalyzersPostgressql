# Quick Fix for Azure OpenAI Error

Based on your screenshot showing **"gpt-5-mini"**, here's the issue:

## The Problem
**Model name is wrong**: `gpt-5-mini` doesn't exist

## The Fix

Update your `appsettings.json`:

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=phonetic_native;Username=postgres;Password=yourpassword"
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT/chat/completions?api-version=2024-08-01-preview",
    "ApiKey": "your-api-key",
    "Model": "gpt-4o-mini",
    "Temperature": "0.3"
  }
}
```

## Available Models (November 2025)

| Correct Name | Don't Use |
|--------------|-----------|
| `gpt-4o-mini` ✅ | ~~gpt-5-mini~~ ❌ |
| `gpt-4o` ✅ | ~~gpt-5~~ ❌ |
| `gpt-4-turbo` ✅ | |
| `gpt-3.5-turbo` ✅ | ~~gpt-35-turbo~~ ⚠️ |

**Note:** The "Model" field should match your **deployment name** in Azure, not necessarily the model version.

## Steps to Fix

1. **Check your Azure deployment name:**
   - Go to Azure Portal → Your OpenAI Resource → "Model deployments"
   - Copy the exact deployment name (e.g., "gpt-4o-mini-deployment")

2. **Update appsettings.json:**
   ```json
   "Model": "your-actual-deployment-name"
   ```

3. **Verify endpoint format:**
   ```
   https://{RESOURCE}.openai.azure.com/openai/deployments/{DEPLOYMENT}/chat/completions?api-version=2024-08-01-preview
   ```
   
   Replace:
   - `{RESOURCE}` = your resource name
   - `{DEPLOYMENT}` = your deployment name (same as Model field)

4. **Test again:**
   ```powershell
   dotnet run
   ```

The updated error handling will now show you exactly what Azure OpenAI returns!

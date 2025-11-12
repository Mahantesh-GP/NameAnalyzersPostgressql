# Ollama Enrichment Integration Guide

## Overview
This guide explains how to use your local Ollama instance to automatically enrich names when bulk uploading to the database. This system uses the **multi-provider LLM configuration** already set up in the project.

> **📝 Note**: For complete LLM configuration details including Azure OpenAI and OpenAI providers, see [DEVELOPMENT-SETUP.md](DEVELOPMENT-SETUP.md#llm-integration-multi-provider-support)

## Prerequisites

### 1. Install and Run Ollama
```bash
# Download Ollama from https://ollama.ai
# Install the model (e.g., llama3.2, mistral, etc.)
ollama pull llama3.2

# Start Ollama (it runs on http://localhost:11434 by default)
ollama serve
```

### 2. Verify Ollama is Running
```bash
# Test the endpoint
curl http://localhost:11434/api/tags

# You should see a list of available models
```

## Configuration

### Multi-Provider LLM System
The project uses a sophisticated multi-provider LLM system configured in `appsettings.Development.json`:

```json
{
  "LLM": {
    "DefaultProvider": "OpenSource",
    "GlobalSettings": {
      "TimeoutMs": 30000,
      "RetryAttempts": 3,
      "EnableCaching": true,
      "CacheTtlMinutes": 60
    },
    "Providers": {
      "OpenSource": {
        "Enabled": true,
        "Model": "llama3.2",
        "Endpoint": "http://localhost:11434",
        "Settings": {
          "MaxTokens": 1000,
          "Temperature": 0.3
        },
        "Authentication": {
          "Type": "None"
        }
      }
    }
  }
}
```

### Switching Between Providers
You can switch between Ollama, Azure OpenAI, and OpenAI by changing the `DefaultProvider` setting:

```json
{
  "LLM": {
    "DefaultProvider": "OpenSource"  // Options: "OpenSource", "AzureOpenAI", "OpenAI"
  }
}
```

**Supported Models:**
- `llama3.2` (recommended for name analysis)
- `llama3.1`
- `mistral`
- `mixtral`
- `phi3`
- Any other Ollama-compatible model

## How It Works

### Automatic Enrichment Flow
```
1. Bulk Upload Names → Azure Function
2. Names Ingested into Database
3. Automatic Enrichment Triggered
4. Ollama Analyzes Each Name
5. Results Saved to Database
```

### What Gets Enriched?
- **Cultural Origins**: Identifies the cultural/ethnic origin of names
- **Name Aliases**: Generates common variations and nicknames
- **Gender Analysis**: Determines likely gender associations
- **Phonetic Patterns**: Analyzes pronunciation patterns
- **Confidence Scores**: Provides confidence ratings for each analysis

## API Endpoints

### 1. Batch Ingest with Auto-Enrichment
**Endpoint:** `POST /api/ingest/batch`

**Request Body:**
```json
{
  "persons": [
    {
      "externalId": "EXT001",
      "fullName": "John Smith",
      "county": "Los Angeles",
      "countyId": 1,
      "countyName": "Los Angeles County",
      "flag": "I",
      "expandNicknames": true
    },
    {
      "externalId": "EXT002",
      "fullName": "Maria Garcia",
      "county": "San Diego",
      "countyId": 2,
      "countyName": "San Diego County",
      "flag": "I"
    }
  ]
}
```

**Response:**
```json
{
  "totalProcessed": 2,
  "successful": 2,
  "failed": 0,
  "results": [
    {
      "externalId": "EXT001",
      "personId": 12345,
      "status": "success"
    }
  ],
  "errors": [],
  "enrichment": {
    "enriched": 2,
    "errors": 0,
    "message": "Successfully enriched 2 names using Ollama LLM"
  }
}
```

### 2. Manual Enrichment Endpoint
**Endpoint:** `POST /api/enrich`

**Use Case:** Enrich names that were uploaded previously

**Request Body (Specific Person IDs):**
```json
{
  "personIds": [12345, 12346, 12347],
  "autoEnrichAfterUpload": true
}
```

**Request Body (Auto-select Names Needing Enrichment):**
```json
{
  "limit": 100,
  "enrichmentIntervalDays": 30
}
```

**Response:**
```json
{
  "success": true,
  "totalProcessed": 3,
  "successful": 3,
  "failed": 0,
  "processingTimeSeconds": 15.23,
  "results": [
    {
      "personId": 12345,
      "fullName": "John Smith",
      "status": "success",
      "aliasCount": 8,
      "culturalOrigin": "English",
      "confidence": 0.92
    }
  ],
  "errors": []
}
```

## Usage Examples

### From WebUI (Bulk Upload Form)

The WebUI bulk upload form will automatically trigger enrichment when you upload names. No additional action needed!

### From Postman/cURL

**Example 1: Bulk Upload with Auto-Enrichment**
```bash
curl -X POST http://localhost:7071/api/ingest/batch \
  -H "Content-Type: application/json" \
  -d '{
    "persons": [
      {
        "externalId": "001",
        "fullName": "Ahmed Hassan",
        "county": "Los Angeles",
        "countyId": 1,
        "countyName": "Los Angeles County",
        "flag": "I"
      },
      {
        "externalId": "002",
        "fullName": "Wei Chen",
        "county": "San Francisco",
        "countyId": 2,
        "countyName": "San Francisco County",
        "flag": "I"
      }
    ]
  }'
```

**Example 2: Manual Enrichment**
```bash
curl -X POST http://localhost:7071/api/enrich \
  -H "Content-Type: application/json" \
  -d '{
    "limit": 50,
    "enrichmentIntervalDays": 30
  }'
```

## Performance Considerations

### Processing Speed
- **Ollama (Local)**: ~2-5 seconds per name
- **Batch of 10 names**: ~20-50 seconds
- **Batch of 50 names**: ~2-4 minutes

### Optimization Tips
1. **Use Faster Models**: `phi3` or `llama3.2:1b` for quicker processing
2. **Batch Size**: The system limits auto-enrichment to 50 names per batch upload
3. **Background Processing**: For large datasets (>50 names), use the manual enrichment endpoint separately

### Resource Usage
- **Memory**: Ollama typically uses 4-8GB RAM
- **CPU**: Benefits from multi-core processors
- **GPU**: Optional but significantly speeds up processing

## Troubleshooting

### Ollama Not Responding
```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Restart Ollama
ollama serve
```

### Model Not Found
```bash
# List available models
ollama list

# Pull the required model
ollama pull llama3.2
```

### Enrichment Fails During Batch Upload
- Check Azure Functions logs
- Verify Ollama is running: `http://localhost:11434`
- Ensure model is downloaded: `ollama list`
- Check network connectivity between Functions and Ollama

### Slow Performance
- Use a smaller model: `phi3` or `llama3.2:1b`
- Reduce `MaxTokens` in configuration
- Consider GPU acceleration
- Process enrichment in smaller batches

## Monitoring

### Check Enrichment Status
Query the database to see enrichment timestamps:
```sql
SELECT 
    id,
    full_name,
    last_enrichment_utc,
    CASE 
        WHEN last_enrichment_utc IS NULL THEN 'Never Enriched'
        WHEN last_enrichment_utc < NOW() - INTERVAL '30 days' THEN 'Needs Re-enrichment'
        ELSE 'Recently Enriched'
    END as enrichment_status
FROM person_names
ORDER BY last_enrichment_utc DESC NULLS LAST
LIMIT 100;
```

### Azure Functions Logs
```bash
# View real-time logs
func start --verbose

# Look for enrichment messages
# [Info] Triggering automatic enrichment for 5 newly ingested persons
# [Info] Successfully enriched person 12345: John Smith
```

## Advanced Configuration

### Using Different Models for Different Tasks
Edit `appsettings.Development.json`:

```json
{
  "LLM": {
    "Providers": {
      "OpenSource": {
        "Model": "llama3.2",
        "Settings": {
          "Temperature": 0.3,
          "MaxTokens": 1000
        }
      }
    }
  }
}
```

**Model Recommendations:**
- **High Accuracy**: `llama3.1` or `mixtral`
- **Balanced**: `llama3.2` (default)
- **Fast Processing**: `phi3` or `llama3.2:1b`

### Provider Configuration Reference
For complete provider configuration including:
- **Azure OpenAI**: API key or Managed Identity authentication
- **OpenAI**: API key authentication
- **Ollama/OpenSource**: No authentication required
- **Caching**: 3-tier caching strategy (Memory → Redis → Database)

See the comprehensive guide: [DEVELOPMENT-SETUP.md](DEVELOPMENT-SETUP.md#llm-integration-multi-provider-support)

### Custom Prompts
The LLM service uses specialized prompts for name analysis. To customize, modify:
`PhoneticAnalyzers.Application/Services/LLM/Providers/OpenSourceLLMProvider.cs`

## Cost Comparison

| Provider | Cost per 1000 Names | Speed | Notes |
|----------|-------------------|-------|-------|
| **Ollama (Local)** | **$0** | Moderate | Free, runs locally |
| Azure OpenAI | ~$5-15 | Fast | Requires Azure subscription |
| OpenAI | ~$10-20 | Fast | Requires OpenAI API key |

## Next Steps

1. ✅ Install and start Ollama
2. ✅ Pull a model (`ollama pull llama3.2`)
3. ✅ Start Azure Functions (`func start` in Ingestion folder)
4. ✅ Test bulk upload from WebUI
5. ✅ Verify enrichment results in database

## Support

For issues or questions:
- Check Azure Functions logs
- Verify Ollama status: `http://localhost:11434`
- Review PostgreSQL logs for database errors
- Check the `last_enrichment_utc` column in `person_names` table

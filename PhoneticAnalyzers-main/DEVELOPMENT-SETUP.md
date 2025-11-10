# PhoneticAnalyzers LLM Development Setup

This guide helps you set up the LLM integration for development and testing with your personal Azure subscription and local Ollama.

## 🚀 Quick Start

The PhoneticAnalyzers system now includes a comprehensive LLM integration with:

- **Multi-Provider Support**: Azure OpenAI, OpenAI, and Ollama
- **Smart 3-Tier Caching**: Memory → Redis → Database with automatic promotion
- **Batch Processing**: Upload CSV/JSON files for bulk name enrichment
- **Development-Friendly Configuration**: API keys instead of managed identity

## 🔧 Prerequisites

1. **.NET 8.0 SDK** installed
2. **Azure OpenAI Account** (for production-quality results)
3. **Ollama** (for local testing - optional)
4. **Redis** (optional - for distributed caching)

## 🔑 Configuration

### 1. Azure OpenAI Setup (Recommended)

1. **Create Azure OpenAI Resource**:
   - Go to Azure Portal → Create Resource → Azure OpenAI
   - Choose your subscription and resource group
   - Select a supported region (e.g., East US, West Europe)

2. **Deploy a Model**:
   - In Azure OpenAI Studio, go to "Deployments"
   - Create new deployment with `gpt-4` or `gpt-3.5-turbo`
   - Note the deployment name (e.g., "gpt-4")

3. **Get Your Configuration**:
   - **Endpoint**: `https://your-resource.openai.azure.com/`
   - **API Key**: In Azure Portal → Your OpenAI Resource → Keys and Endpoint
   - **Deployment Name**: From step 2

### 2. OpenAI Setup (Alternative)

1. **Get API Key**: Visit https://platform.openai.com/api-keys
2. **Choose Model**: `gpt-4` or `gpt-3.5-turbo`

### 3. Ollama Setup (Local Testing)

1. **Install Ollama**: Download from https://ollama.ai/
2. **Pull a Model**:
   ```bash
   ollama pull llama3.2:3b
   ```
3. **Start Ollama**: It typically runs on `http://localhost:11434`

## ⚙️ Demo App Configuration

### Update DemoApp/appsettings.Development.json

Create or update the file with your credentials:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "PhoneticAnalyzers.Application.Services.LLM": "Debug"
    }
  },
  
  "LLM": {
    "DefaultProvider": "AzureOpenAI",
    "Providers": {
      "AzureOpenAI": {
        "Authentication": {
          "Type": "ApiKey"
        },
        "Settings": {
          "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
          "ApiKey": "YOUR-API-KEY-HERE",
          "DeploymentName": "gpt-4",
          "ApiVersion": "2024-10-21"
        },
        "Configuration": {
          "MaxTokens": 1000,
          "Temperature": 0.3,
          "TopP": 0.95,
          "FrequencyPenalty": 0.0,
          "PresencePenalty": 0.0
        }
      },
      
      "OpenAI": {
        "Authentication": {
          "Type": "ApiKey"
        },
        "Settings": {
          "ApiKey": "YOUR-OPENAI-API-KEY",
          "Model": "gpt-3.5-turbo"
        },
        "Configuration": {
          "MaxTokens": 1000,
          "Temperature": 0.3
        }
      },
      
      "Ollama": {
        "Authentication": {
          "Type": "None"
        },
        "Settings": {
          "BaseUrl": "http://localhost:11434",
          "Model": "llama3.2:3b"
        },
        "Configuration": {
          "MaxTokens": 1000,
          "Temperature": 0.3
        }
      }
    }
  },

  "Caching": {
    "Memory": {
      "ExpirationMinutes": 30,
      "MaxSizeEntries": 1000
    },
    "Redis": {
      "Enabled": false,
      "ConnectionString": "localhost:6379",
      "ExpirationHours": 24
    },
    "Database": {
      "Enabled": true,
      "ExpirationDays": 30
    }
  }
}
```

### Quick Test Configuration

For fastest setup, use Ollama (no API keys required):

1. **Install & Start Ollama**:
   ```bash
   ollama pull llama3.2:3b
   ollama serve
   ```

2. **Set Default Provider** in appsettings.Development.json:
   ```json
   {
     "LLM": {
       "DefaultProvider": "Ollama"
     }
   }
   ```

## 🏃‍♂️ Running the Demo

1. **Navigate to Project**:
   ```bash
   cd C:\Learnings\PhoneticAnalyzer-short\PhoneticAnalyzers-main
   ```

2. **Run Demo Application**:
   ```bash
   dotnet run --project DemoApp
   ```

3. **What You'll See**:
   ```
   🚀 PhoneticAnalyzers LLM Demo
   ============================
   
   🔧 Testing individual name analysis...
   📝 Analyzing name: 'Muhammad'
   ✅ Generated 8 aliases for 'Muhammad'
   
   🔧 Testing batch processing...
   📊 Processing batch with 3 names...
   ✅ Batch completed:
      📊 Total items: 3 names
      ✅ Successful: 3
      ❌ Failed: 0
      ⏱️  Total time: 2.3s
   
   📊 Cache Performance:
      💾 Memory Cache: 2 entries, 100% hit rate
      🌐 Redis Cache: 0 entries (disabled)
      💽 Database Cache: 0 entries
   ```

## 🧪 Testing Different Providers

### Switch to Azure OpenAI
Update `DefaultProvider` in appsettings.Development.json:
```json
{
  "LLM": {
    "DefaultProvider": "AzureOpenAI"
  }
}
```

### Switch to OpenAI
```json
{
  "LLM": {
    "DefaultProvider": "OpenAI"
  }
}
```

### Switch to Ollama
```json
{
  "LLM": {
    "DefaultProvider": "Ollama"
  }
}
```

## 📈 Performance Tips

### Enable Redis for Better Caching
1. **Install Redis**: Use Docker or Windows Subsystem for Linux
   ```bash
   docker run -d -p 6379:6379 redis:alpine
   ```

2. **Enable in Configuration**:
   ```json
   {
     "Caching": {
       "Redis": {
         "Enabled": true,
         "ConnectionString": "localhost:6379"
       }
     }
   }
   ```

### Batch Processing for Large Datasets
- CSV format: `Name,Surname,Culture`
- JSON format: `[{"firstName":"John","lastName":"Smith","culture":"en-US"}]`
- Automatic concurrent processing with smart throttling

## 🔍 Troubleshooting

### Common Issues

1. **"Provider not found" Error**:
   - Check `DefaultProvider` matches a key in `Providers` section
   - Ensure provider configuration is complete

2. **Azure OpenAI Authentication Error**:
   - Verify API key is correct
   - Check endpoint URL format: `https://YOUR-RESOURCE.openai.azure.com/`
   - Ensure deployment name matches your Azure OpenAI deployment

3. **Ollama Connection Error**:
   - Verify Ollama is running: `curl http://localhost:11434/api/version`
   - Check model is pulled: `ollama list`

4. **High Token Usage**:
   - Use Ollama for development (free)
   - Enable caching to reduce API calls
   - Reduce batch sizes for testing

### Debug Logging

Enable detailed logging in appsettings.Development.json:
```json
{
  "Logging": {
    "LogLevel": {
      "PhoneticAnalyzers.Application.Services.LLM": "Trace",
      "PhoneticAnalyzers.Application.Services.SmartLLMCacheService": "Debug"
    }
  }
}
```

## 🚀 Next Steps

1. **Test Individual Names**: Run the demo and try different names
2. **Upload Batch Files**: Test with your own CSV/JSON data
3. **Monitor Performance**: Check cache hit rates and processing times
4. **Switch Providers**: Compare quality between Azure OpenAI, OpenAI, and Ollama
5. **Production Setup**: Use managed identity and proper secret management

## 📞 LLM Call Patterns

**When LLM calls happen:**
- ✅ **Individual name analysis**: Real-time when `AnalyzeNameAsync()` called
- ✅ **Batch processing**: During CSV/JSON file processing
- ❌ **Not during**: Simple lookups, cached results, or database queries

**Caching Strategy:**
1. **Memory Cache** (30min): Fast access for recent requests
2. **Redis Cache** (24h): Shared cache across instances  
3. **Database Cache** (30 days): Long-term persistent storage
4. **Automatic Promotion**: Popular items promoted to higher cache tiers
namespace PhoneticAnalyzers.Application.Services.LLM;

/// <summary>
/// Development configuration helper for easy LLM setup
/// </summary>
public static class DevelopmentLLMConfig
{
    /// <summary>
    /// Gets development LLM configuration with API keys (not managed identity)
    /// </summary>
    /// <param name="azureOpenAIApiKey">Your Azure OpenAI API key</param>
    /// <param name="azureOpenAIEndpoint">Your Azure OpenAI endpoint</param>
    /// <param name="openAIApiKey">Optional OpenAI API key</param>
    /// <returns>Development-ready LLM configuration</returns>
    public static LLMConfiguration GetDevelopmentConfig(
        string? azureOpenAIApiKey = null, 
        string? azureOpenAIEndpoint = null,
        string? openAIApiKey = null)
    {
        return new LLMConfiguration
        {
            DefaultProvider = "AzureOpenAI",
            GlobalSettings = new LLMGlobalSettings
            {
                TimeoutMs = 30000,
                RetryAttempts = 2,
                EnableCaching = true,
                CacheTtlMinutes = 30, // Shorter cache for development
                EnableTelemetry = true
            },
            Providers = new Dictionary<string, LLMProviderConfiguration>
            {
                ["AzureOpenAI"] = new LLMProviderConfiguration
                {
                    Enabled = !string.IsNullOrEmpty(azureOpenAIApiKey),
                    Model = "gpt-4o-mini", // Cheaper model for development
                    Endpoint = azureOpenAIEndpoint ?? "https://your-resource.openai.azure.com/",
                    Settings = new Dictionary<string, object>
                    {
                        ["DeploymentName"] = "gpt-4o-mini", // or "gpt-35-turbo"
                        ["ApiVersion"] = "2024-02-15-preview",
                        ["MaxTokens"] = 1000, // Lower for cost control
                        ["Temperature"] = 0.1,
                        ["UseSystemAssignedIdentity"] = false, // KEY: Use API key in development
                        ["ApiKey"] = azureOpenAIApiKey ?? ""
                    },
                    Authentication = new LLMAuthenticationConfiguration
                    {
                        Type = "ApiKey"
                    }
                },
                ["OpenAI"] = new LLMProviderConfiguration
                {
                    Enabled = !string.IsNullOrEmpty(openAIApiKey),
                    Model = "gpt-4o-mini", // Cheaper model
                    Settings = new Dictionary<string, object>
                    {
                        ["MaxTokens"] = 1000,
                        ["Temperature"] = 0.1,
                        ["BaseUrl"] = "https://api.openai.com/v1",
                        ["ApiKey"] = openAIApiKey ?? ""
                    },
                    Authentication = new LLMAuthenticationConfiguration
                    {
                        Type = "ApiKey"
                    }
                },
                ["OpenSource"] = new LLMProviderConfiguration
                {
                    Enabled = true, // Always enable Ollama for development
                    Model = "llama3.2:3b", // Small, fast model
                    Endpoint = "http://localhost:11434",
                    Settings = new Dictionary<string, object>
                    {
                        ["MaxTokens"] = 1000,
                        ["Temperature"] = 0.1
                    },
                    Authentication = new LLMAuthenticationConfiguration
                    {
                        Type = "None"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Gets production LLM configuration with managed identity
    /// </summary>
    /// <returns>Production-ready LLM configuration</returns>
    public static LLMConfiguration GetProductionConfig()
    {
        return new LLMConfiguration
        {
            DefaultProvider = "AzureOpenAI",
            GlobalSettings = new LLMGlobalSettings
            {
                TimeoutMs = 60000, // Longer timeout for production
                RetryAttempts = 3,
                EnableCaching = true,
                CacheTtlMinutes = 120, // Longer cache for production
                EnableTelemetry = true
            },
            Providers = new Dictionary<string, LLMProviderConfiguration>
            {
                ["AzureOpenAI"] = new LLMProviderConfiguration
                {
                    Enabled = true,
                    Model = "gpt-4", // Full model for production
                    Settings = new Dictionary<string, object>
                    {
                        ["DeploymentName"] = "gpt-4",
                        ["ApiVersion"] = "2024-02-15-preview",
                        ["MaxTokens"] = 4000,
                        ["Temperature"] = 0.1,
                        ["UseSystemAssignedIdentity"] = true // Managed Identity in production
                    },
                    Authentication = new LLMAuthenticationConfiguration
                    {
                        Type = "ManagedIdentity"
                    }
                },
                ["OpenAI"] = new LLMProviderConfiguration
                {
                    Enabled = false, // Disabled in production
                    Model = "gpt-4",
                    Settings = new Dictionary<string, object>
                    {
                        ["MaxTokens"] = 4000,
                        ["Temperature"] = 0.1
                    }
                }
            }
        };
    }
}
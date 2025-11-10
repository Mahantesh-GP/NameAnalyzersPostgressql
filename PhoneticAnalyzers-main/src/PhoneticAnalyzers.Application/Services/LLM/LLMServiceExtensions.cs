using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhoneticAnalyzers.Application.Services.LLM.Providers;
using PhoneticAnalyzers.Domain.Repositories;

namespace PhoneticAnalyzers.Application.Services.LLM;

/// <summary>
/// Extension methods for registering LLM services
/// </summary>
public static class LLMServiceExtensions
{
    /// <summary>
    /// Adds LLM services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddLLMServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure LLM settings - support both old and new configuration section names
        var llmSection = configuration.GetSection("LLM");
        if (!llmSection.Exists())
        {
            llmSection = configuration.GetSection("LLMConfiguration");
        }
        services.Configure<LLMConfiguration>(llmSection);

        // Add HTTP client for LLM providers that need it
        services.AddHttpClient<OpenAIProvider>();
        services.AddHttpClient<OpenSourceLLMProvider>();

        // Register LLM providers conditionally based on configuration
        RegisterLLMProviders(services, llmSection);

        // Register smart cache service
        services.AddScoped<ILLMCacheService, SmartLLMCacheService>();

        // Register main LLM processing service
        services.AddScoped<ILLMNameProcessingService, LLMNameProcessingService>();

        // Register batch enrichment service
        services.AddScoped<IBatchEnrichmentService, BatchEnrichmentService>();

        return services;
    }

    /// <summary>
    /// Adds Redis distributed caching for LLM responses
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddLLMRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "PhoneticAnalyzers";
                
                // Configure Redis options
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
                {
                    EndPoints = { connectionString },
                    AbortOnConnectFail = false,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000,
                    ReconnectRetryPolicy = new StackExchange.Redis.LinearRetry(1000),
                    KeepAlive = 60
                };
            });
        }
        else
        {
            // Fallback to in-memory distributed cache if Redis is not configured
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Creates a default provider configuration
    /// </summary>
    /// <returns>Default provider configuration</returns>
    private static LLMProviderConfiguration CreateDefaultProviderConfig()
    {
        return new LLMProviderConfiguration
        {
            Enabled = false,
            Settings = new Dictionary<string, object>(),
            Model = "gpt-4",
            Authentication = new LLMAuthenticationConfiguration()
        };
    }

    /// <summary>
    /// Gets the default LLM configuration for development/testing
    /// </summary>
    /// <returns>Default LLM configuration</returns>
    public static LLMConfiguration GetDefaultLLMConfiguration()
    {
        return new LLMConfiguration
        {
            DefaultProvider = "AzureOpenAI",
            GlobalSettings = new LLMGlobalSettings
            {
                TimeoutMs = 30000,
                RetryAttempts = 3,
                EnableCaching = true,
                CacheTtlMinutes = 60,
                EnableTelemetry = true
            },
            Providers = new Dictionary<string, LLMProviderConfiguration>
            {
                ["AzureOpenAI"] = new LLMProviderConfiguration
                {
                    Enabled = true,
                    Model = "gpt-4",
                    Endpoint = "https://your-azure-openai.openai.azure.com/",
                    Settings = new Dictionary<string, object>
                    {
                        ["DeploymentName"] = "gpt-4",
                        ["ApiVersion"] = "2024-02-15-preview",
                        ["MaxTokens"] = 4096,
                        ["Temperature"] = 0.1,
                        ["UseSystemAssignedIdentity"] = true
                    },
                    Authentication = new LLMAuthenticationConfiguration
                    {
                        Type = "ManagedIdentity"
                    }
                },
                ["OpenAI"] = new LLMProviderConfiguration
                {
                    Enabled = false,
                    Model = "gpt-4",
                    Settings = new Dictionary<string, object>
                    {
                        ["MaxTokens"] = 4096,
                        ["Temperature"] = 0.1,
                        ["BaseUrl"] = "https://api.openai.com/v1"
                    },
                    Authentication = new LLMAuthenticationConfiguration
                    {
                        Type = "ApiKey"
                    }
                },
                ["OpenSource"] = new LLMProviderConfiguration
                {
                    Enabled = false,
                    Model = "llama2",
                    Endpoint = "http://localhost:11434",
                    Settings = new Dictionary<string, object>
                    {
                        ["MaxTokens"] = 4096,
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
    /// Registers LLM providers conditionally based on configuration
    /// </summary>
    private static void RegisterLLMProviders(IServiceCollection services, IConfigurationSection llmSection)
    {
        var providers = llmSection.GetSection("Providers");

        // Only register providers that exist in configuration and have valid settings
        // TODO: Fix Azure OpenAI conditional registration
        // Temporarily disabled to allow OpenSource provider testing
        /*
        var azureSection = providers.GetSection("AzureOpenAI");
        var endpointValue = azureSection.GetSection("Settings:Endpoint").Value;
        var hasValidAzureEndpoint = !string.IsNullOrEmpty(endpointValue) && 
                                   !endpointValue.Contains("YOUR-RESOURCE");
        if (azureSection.Exists() && hasValidAzureEndpoint)
        {
            services.AddScoped<ILLMProvider, AzureOpenAIProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AzureOpenAIProvider>>();
                var llmConfig = serviceProvider.GetRequiredService<IOptions<LLMConfiguration>>().Value;
                
                var azureConfig = llmConfig.Providers.ContainsKey("AzureOpenAI") 
                    ? llmConfig.Providers["AzureOpenAI"] 
                    : CreateDefaultProviderConfig();
                    
                return new AzureOpenAIProvider(logger, azureConfig);
            });
        }
        */

        // TODO: Fix OpenAI conditional registration
        // Temporarily disabled to allow OpenSource provider testing
        /*
        var openAISection = providers.GetSection("OpenAI");
        var apiKeyValue = openAISection.GetSection("Settings:ApiKey").Value;
        var hasValidOpenAIKey = !string.IsNullOrEmpty(apiKeyValue) && 
                               !apiKeyValue.Contains("YOUR-OPENAI-API-KEY");
        if (openAISection.Exists() && hasValidOpenAIKey)
        {
            services.AddScoped<ILLMProvider, OpenAIProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<OpenAIProvider>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(OpenAIProvider));
                var llmConfig = serviceProvider.GetRequiredService<IOptions<LLMConfiguration>>().Value;
                
                var openAIConfig = llmConfig.Providers.ContainsKey("OpenAI") 
                    ? llmConfig.Providers["OpenAI"] 
                    : CreateDefaultProviderConfig();
                    
                return new OpenAIProvider(logger, openAIConfig, httpClient);
            });
        }
        */

        if (providers.GetSection("OpenSource").Exists())
        {
            services.AddScoped<ILLMProvider, OpenSourceLLMProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<OpenSourceLLMProvider>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(OpenSourceLLMProvider));
                var llmConfig = serviceProvider.GetRequiredService<IOptions<LLMConfiguration>>().Value;
                
                var openSourceConfig = llmConfig.Providers.ContainsKey("OpenSource") 
                    ? llmConfig.Providers["OpenSource"] 
                    : CreateDefaultProviderConfig();
                    
                return new OpenSourceLLMProvider(logger, openSourceConfig, httpClient);
            });
        }
    }
}
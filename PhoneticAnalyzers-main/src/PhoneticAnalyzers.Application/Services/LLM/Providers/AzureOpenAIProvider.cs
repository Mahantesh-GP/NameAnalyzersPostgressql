using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services.LLM.Providers
{
    /// <summary>
    /// Azure OpenAI implementation of ILLMProvider
    /// </summary>
    public class AzureOpenAIProvider : BaseLLMProvider
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <inheritdoc />
        public override string ProviderType => "AzureOpenAI";

        /// <inheritdoc />
        public override string ModelName => _deploymentName;

        /// <inheritdoc />
        public override bool IsAvailable => ValidateConfiguration();

        public AzureOpenAIProvider(ILogger<AzureOpenAIProvider> logger, LLMProviderConfiguration config)
            : base(logger, config)
        {
            _deploymentName = config.Model;
            _client = CreateOpenAIClient(config);
        }

        /// <summary>
        /// Creates and configures the OpenAI client
        /// </summary>
        private OpenAIClient CreateOpenAIClient(LLMProviderConfiguration config)
        {
            var endpoint = config.Endpoint ?? 
                throw new ArgumentException("Azure OpenAI endpoint is required", nameof(config.Endpoint));

            var uri = new Uri(endpoint);
            TokenCredential credential;

            switch (config.Authentication.Type?.ToLowerInvariant())
            {
                case "managedidentity":
                    credential = new ManagedIdentityCredential(config.Authentication.ClientId);
                    _logger.LogInformation("Using Managed Identity authentication for Azure OpenAI");
                    break;

                case "serviceprincipal":
                    credential = new ClientSecretCredential(
                        config.Authentication.TenantId,
                        config.Authentication.ClientId,
                        Environment.GetEnvironmentVariable($"AZURE_CLIENT_SECRET_{config.Authentication.ClientId}") ??
                        throw new InvalidOperationException("Client secret not found in environment variables"));
                    _logger.LogInformation("Using Service Principal authentication for Azure OpenAI");
                    break;

                case "apikey":
                    var apiKey = Environment.GetEnvironmentVariable($"AZURE_OPENAI_KEY_{_deploymentName}") ??
                        throw new InvalidOperationException("API key not found in environment variables");
                    return new OpenAIClient(uri, new AzureKeyCredential(apiKey));

                default:
                    credential = new DefaultAzureCredential();
                    _logger.LogInformation("Using DefaultAzureCredential for Azure OpenAI authentication");
                    break;
            }

            return new OpenAIClient(uri, credential);
        }

        /// <inheritdoc />
        protected override async Task<LLMNameAnalysisResult> AnalyzeNameInternalAsync(LLMNameAnalysisRequest request, CancellationToken cancellationToken)
        {
            var systemPrompt = CreateNameAnalysisSystemPrompt();
            var userPrompt = CreateNameAnalysisUserPrompt(request);

                var messages = new ChatRequestMessage[]
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            };
            var chatOptions = new ChatCompletionsOptions(_deploymentName, messages)
            {
                Temperature = 0.3f,
                MaxTokens = 2000,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            try
            {
                var response = await _client.GetChatCompletionsAsync(chatOptions, cancellationToken);
                var result = response.Value;

                var content = result.Choices.FirstOrDefault()?.Message.Content;
                if (string.IsNullOrEmpty(content))
                {
                    throw new LLMProviderException(ProviderType, ModelName, "NameAnalysis", "Empty response received");
                }

                var analysisData = JsonSerializer.Deserialize<NameAnalysisResponse>(content, JsonOptions);
                if (analysisData == null)
                {
                    throw new LLMProviderException(ProviderType, ModelName, "NameAnalysis", "Failed to deserialize response");
                }

                return new LLMNameAnalysisResult
                {
                    OriginalName = request.Name,
                    Aliases = analysisData.Aliases.Select(MapToLLMNameAlias).ToList(),
                    CulturalContexts = analysisData.CulturalContexts.Select(MapToLLMCulturalContext).ToList(),
                    Metadata = CreateMetadata(
                        (long)(DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds,
                        result.Usage?.TotalTokens,
                        CalculateCost(result.Usage?.TotalTokens ?? 0)
                    )
                };
            }
            catch (RequestFailedException ex)
            {
                HandleAzureOpenAIException(ex, "NameAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMPhoneticAnalysisResult> GeneratePhoneticVariationsInternalAsync(LLMPhoneticAnalysisRequest request, CancellationToken cancellationToken)
        {
            var systemPrompt = CreatePhoneticAnalysisSystemPrompt();
            var userPrompt = CreatePhoneticAnalysisUserPrompt(request);

                var messages = new ChatRequestMessage[]
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            };
            var chatOptions = new ChatCompletionsOptions(_deploymentName, messages)
            {
                Temperature = 0.2f,
                MaxTokens = 1500,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            try
            {
                var response = await _client.GetChatCompletionsAsync(chatOptions, cancellationToken);
                var result = response.Value;

                var content = result.Choices.FirstOrDefault()?.Message.Content;
                if (string.IsNullOrEmpty(content))
                {
                    throw new LLMProviderException(ProviderType, ModelName, "PhoneticAnalysis", "Empty response received");
                }

                var analysisData = JsonSerializer.Deserialize<PhoneticAnalysisResponse>(content, JsonOptions);
                if (analysisData == null)
                {
                    throw new LLMProviderException(ProviderType, ModelName, "PhoneticAnalysis", "Failed to deserialize response");
                }

                return new LLMPhoneticAnalysisResult
                {
                    OriginalName = request.Name,
                    Variations = analysisData.Variations.Select(MapToLLMPhoneticVariation).ToList(),
                    Transliterations = analysisData.Transliterations.Select(MapToLLMTransliteration).ToList(),
                    Metadata = CreateMetadata(
                        (long)(DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds,
                        result.Usage?.TotalTokens,
                        CalculateCost(result.Usage?.TotalTokens ?? 0)
                    )
                };
            }
            catch (RequestFailedException ex)
            {
                HandleAzureOpenAIException(ex, "PhoneticAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMCulturalAnalysisResult> AnalyzeCulturalContextInternalAsync(LLMCulturalAnalysisRequest request, CancellationToken cancellationToken)
        {
            var systemPrompt = CreateCulturalAnalysisSystemPrompt();
            var userPrompt = CreateCulturalAnalysisUserPrompt(request);

                var messages = new ChatRequestMessage[]
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            };
            var chatOptions = new ChatCompletionsOptions(_deploymentName, messages)
            {
                Temperature = 0.3f,
                MaxTokens = 1000,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            try
            {
                var response = await _client.GetChatCompletionsAsync(chatOptions, cancellationToken);
                var result = response.Value;

                var content = result.Choices.FirstOrDefault()?.Message.Content;
                if (string.IsNullOrEmpty(content))
                {
                    throw new LLMProviderException(ProviderType, ModelName, "CulturalAnalysis", "Empty response received");
                }

                var analysisData = JsonSerializer.Deserialize<CulturalAnalysisResponse>(content, JsonOptions);
                if (analysisData == null)
                {
                    throw new LLMProviderException(ProviderType, ModelName, "CulturalAnalysis", "Failed to deserialize response");
                }

                return new LLMCulturalAnalysisResult
                {
                    OriginalName = request.Name,
                    CulturalOrigins = analysisData.CulturalOrigins.Select(MapToLLMCulturalContext).ToList(),
                    Etymology = analysisData.Etymology,
                    HistoricalContext = analysisData.HistoricalContext,
                    Metadata = CreateMetadata(
                        (long)(DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds,
                        result.Usage?.TotalTokens,
                        CalculateCost(result.Usage?.TotalTokens ?? 0)
                    )
                };
            }
            catch (RequestFailedException ex)
            {
                HandleAzureOpenAIException(ex, "CulturalAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMProviderHealthResult> CheckHealthInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                var messages = new ChatRequestMessage[]
                {
                    new ChatRequestUserMessage("Hello, this is a health check.")
                };
                var testOptions = new ChatCompletionsOptions(_deploymentName, messages)
                {
                    MaxTokens = 10,
                    Temperature = 0f
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await _client.GetChatCompletionsAsync(testOptions, cancellationToken);
                stopwatch.Stop();

                return new LLMProviderHealthResult
                {
                    IsHealthy = true,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Message = "Azure OpenAI provider is healthy",
                    LastSuccessfulCheck = DateTime.UtcNow,
                    AvailableModels = new[] { _deploymentName }
                };
            }
            catch (RequestFailedException ex)
            {
                return new LLMProviderHealthResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = 0,
                    Message = $"Azure OpenAI provider is unhealthy: {ex.Message}",
                    LastSuccessfulCheck = null,
                    AvailableModels = null
                };
            }
        }

        /// <inheritdoc />
        protected override bool ValidateProviderSpecificConfiguration()
        {
            return !string.IsNullOrWhiteSpace(_config.Endpoint) &&
                   !string.IsNullOrWhiteSpace(_deploymentName) &&
                   _config.Authentication != null;
        }

        /// <summary>
        /// Handles Azure OpenAI specific exceptions
        /// </summary>
        private void HandleAzureOpenAIException(RequestFailedException ex, string operation)
        {
            if (ex.Status == 429)
            {
                var retryAfter = TimeSpan.FromMinutes(1); // Default retry after 1 minute
                try
                {
                    var response = ex.GetRawResponse();
                    if (response?.Headers.TryGetValue("retry-after", out var retryHeaderValue) == true)
                    {
                        if (int.TryParse(retryHeaderValue, out var retrySeconds))
                        {
                            retryAfter = TimeSpan.FromSeconds(retrySeconds);
                        }
                    }
                }
                catch
                {
                    // Use default retry time if header parsing fails
                }
                
                throw new LLMProviderRateLimitException(ProviderType, ModelName, operation, retryAfter);
            }

            throw new LLMProviderException(ProviderType, ModelName, operation, ex.Message, ex);
        }

        /// <summary>
        /// Calculates estimated cost based on token usage
        /// </summary>
        private decimal? CalculateCost(int tokens)
        {
            // Rough estimation - actual costs vary by model and region
            var costPerToken = GetConfigValue("CostPerToken", 0.0000015m);
            return tokens * costPerToken;
        }

        #region Prompt Creation Methods

        private string CreateNameAnalysisSystemPrompt()
        {
            return @"You are an expert linguist specializing in name analysis across cultures and languages. 
Your task is to analyze names and generate comprehensive aliases, nicknames, and cultural variations.

Respond with valid JSON in this exact format:
{
  ""aliases"": [
    {
      ""alias"": ""string"",
      ""confidence"": 0.0-1.0,
      ""aliasType"": ""nickname|formal|diminutive|transliteration|cultural_variant"",
      ""culturalContext"": ""string or null"",
      ""source"": ""reasoning""
    }
  ],
  ""culturalContexts"": [
    {
      ""culture"": ""string"",
      ""confidence"": 0.0-1.0,
      ""language"": ""string or null"",
      ""region"": ""string or null"",
      ""notes"": ""string or null""
    }
  ]
}

Focus on accuracy and cultural sensitivity. Provide confidence scores based on linguistic evidence.";
        }

        private string CreateNameAnalysisUserPrompt(LLMNameAnalysisRequest request)
        {
            var prompt = $"Analyze the name '{request.Name}'";
            
            if (!string.IsNullOrWhiteSpace(request.CulturalHint))
            {
                prompt += $" with cultural context: {request.CulturalHint}";
            }

            prompt += $". Generate up to {request.MaxAliases} aliases including:";

            if (request.IncludeNicknames)
                prompt += "\n- Common nicknames and diminutives";
            if (request.IncludePhonetic)
                prompt += "\n- Phonetic variations and similar-sounding names";
            if (request.IncludeTransliterations)
                prompt += "\n- Transliterations to different scripts";

            return prompt;
        }

        private string CreatePhoneticAnalysisSystemPrompt()
        {
            return @"You are a phonetic analysis expert. Generate phonetic variations and transliterations for names.

Respond with valid JSON in this exact format:
{
  ""variations"": [
    {
      ""variation"": ""string"",
      ""variationType"": ""soundex|metaphone|phonetic_approximation|accent_variation"",
      ""confidence"": 0.0-1.0,
      ""algorithm"": ""string or null""
    }
  ],
  ""transliterations"": [
    {
      ""text"": ""string"",
      ""targetLanguage"": ""string"",
      ""confidence"": 0.0-1.0,
      ""method"": ""string or null""
    }
  ]
}

Focus on accurate phonetic representation and common pronunciation variations.";
        }

        private string CreatePhoneticAnalysisUserPrompt(LLMPhoneticAnalysisRequest request)
        {
            var prompt = $"Generate phonetic variations for the name '{request.Name}'";

            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
            {
                prompt += $" from {request.SourceLanguage} language";
            }

            if (request.TargetLanguages.Any())
            {
                prompt += $". Include transliterations to: {string.Join(", ", request.TargetLanguages)}";
            }

            if (request.IncludePhoneticCodes)
            {
                prompt += ". Include standard phonetic algorithm results (Soundex, Metaphone, etc.)";
            }

            return prompt;
        }

        private string CreateCulturalAnalysisSystemPrompt()
        {
            return @"You are a cultural anthropologist and linguist specializing in name etymology and cultural origins.

Respond with valid JSON in this exact format:
{
  ""culturalOrigins"": [
    {
      ""culture"": ""string"",
      ""confidence"": 0.0-1.0,
      ""language"": ""string or null"",
      ""region"": ""string or null"",
      ""notes"": ""string or null""
    }
  ],
  ""etymology"": ""string or null"",
  ""historicalContext"": ""string or null""
}

Provide accurate cultural and linguistic analysis based on established research.";
        }

        private string CreateCulturalAnalysisUserPrompt(LLMCulturalAnalysisRequest request)
        {
            var prompt = $"Analyze the cultural origins and etymology of the name '{request.Name}'";

            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
            {
                prompt += $". Additional context: {request.AdditionalContext}";
            }

            if (request.IncludeHistoricalContext)
            {
                prompt += ". Include historical context and usage patterns.";
            }

            return prompt;
        }

        #endregion

        #region Response Mapping Methods

        private LLMNameAlias MapToLLMNameAlias(NameAliasResponse alias)
        {
            return new LLMNameAlias
            {
                Alias = alias.Alias,
                Confidence = alias.Confidence,
                AliasType = alias.AliasType,
                CulturalContext = alias.CulturalContext,
                Source = alias.Source
            };
        }

        private LLMCulturalContext MapToLLMCulturalContext(CulturalContextResponse context)
        {
            return new LLMCulturalContext
            {
                Culture = context.Culture,
                Confidence = context.Confidence,
                Language = context.Language,
                Region = context.Region,
                Notes = context.Notes
            };
        }

        private LLMPhoneticVariation MapToLLMPhoneticVariation(PhoneticVariationResponse variation)
        {
            return new LLMPhoneticVariation
            {
                Variation = variation.Variation,
                VariationType = variation.VariationType,
                Confidence = variation.Confidence,
                Algorithm = variation.Algorithm
            };
        }

        private LLMTransliteration MapToLLMTransliteration(TransliterationResponse transliteration)
        {
            return new LLMTransliteration
            {
                Text = transliteration.Text,
                TargetLanguage = transliteration.TargetLanguage,
                Confidence = transliteration.Confidence,
                Method = transliteration.Method
            };
        }

        #endregion

        #region Response Models

        private class NameAnalysisResponse
        {
            public List<NameAliasResponse> Aliases { get; set; } = new();
            public List<CulturalContextResponse> CulturalContexts { get; set; } = new();
        }

        private class NameAliasResponse
        {
            public string Alias { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string AliasType { get; set; } = string.Empty;
            public string? CulturalContext { get; set; }
            public string? Source { get; set; }
        }

        private class CulturalContextResponse
        {
            public string Culture { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string? Language { get; set; }
            public string? Region { get; set; }
            public string? Notes { get; set; }
        }

        private class PhoneticAnalysisResponse
        {
            public List<PhoneticVariationResponse> Variations { get; set; } = new();
            public List<TransliterationResponse> Transliterations { get; set; } = new();
        }

        private class PhoneticVariationResponse
        {
            public string Variation { get; set; } = string.Empty;
            public string VariationType { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string? Algorithm { get; set; }
        }

        private class TransliterationResponse
        {
            public string Text { get; set; } = string.Empty;
            public string TargetLanguage { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string? Method { get; set; }
        }

        private class CulturalAnalysisResponse
        {
            public List<CulturalContextResponse> CulturalOrigins { get; set; } = new();
            public string? Etymology { get; set; }
            public string? HistoricalContext { get; set; }
        }

        #endregion
    }
}
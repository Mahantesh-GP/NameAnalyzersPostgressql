using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services.LLM.Providers
{
    /// <summary>
    /// OpenAI (non-Azure) implementation of ILLMProvider
    /// </summary>
    public class OpenAIProvider : BaseLLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <inheritdoc />
        public override string ProviderType => "OpenAI";

        /// <inheritdoc />
        public override string ModelName => _config.Model;

        /// <inheritdoc />
        public override bool IsAvailable => ValidateConfiguration();

        public OpenAIProvider(ILogger<OpenAIProvider> logger, LLMProviderConfiguration config, HttpClient httpClient)
            : base(logger, config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = Environment.GetEnvironmentVariable($"OPENAI_API_KEY") ??
                     throw new InvalidOperationException("OpenAI API key not found in environment variables");
            _baseUrl = config.Endpoint ?? "https://api.openai.com/v1";
            
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PhoneticAnalyzers/1.0");
            _httpClient.Timeout = TimeSpan.FromMilliseconds(GetConfigValue("TimeoutMs", 30000));
        }

        /// <inheritdoc />
        protected override async Task<LLMNameAnalysisResult> AnalyzeNameInternalAsync(LLMNameAnalysisRequest request, CancellationToken cancellationToken)
        {
            var messages = new[]
            {
                new { role = "system", content = CreateNameAnalysisSystemPrompt() },
                new { role = "user", content = CreateNameAnalysisUserPrompt(request) }
            };

            var requestBody = new
            {
                model = _config.Model,
                messages = messages,
                temperature = 0.3,
                max_tokens = 2000,
                response_format = new { type = "json_object" }
            };

            try
            {
                var response = await SendChatCompletionAsync(requestBody, cancellationToken);
                var content = response.Choices.FirstOrDefault()?.Message?.Content;

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
                        0, // Will be calculated in the actual implementation
                        response.Usage?.TotalTokens,
                        CalculateCost(response.Usage?.TotalTokens ?? 0)
                    )
                };
            }
            catch (HttpRequestException ex)
            {
                HandleOpenAIException(ex, "NameAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMPhoneticAnalysisResult> GeneratePhoneticVariationsInternalAsync(LLMPhoneticAnalysisRequest request, CancellationToken cancellationToken)
        {
            var messages = new[]
            {
                new { role = "system", content = CreatePhoneticAnalysisSystemPrompt() },
                new { role = "user", content = CreatePhoneticAnalysisUserPrompt(request) }
            };

            var requestBody = new
            {
                model = _config.Model,
                messages = messages,
                temperature = 0.2,
                max_tokens = 1500,
                response_format = new { type = "json_object" }
            };

            try
            {
                var response = await SendChatCompletionAsync(requestBody, cancellationToken);
                var content = response.Choices.FirstOrDefault()?.Message?.Content;

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
                        0, // Will be calculated in the actual implementation
                        response.Usage?.TotalTokens,
                        CalculateCost(response.Usage?.TotalTokens ?? 0)
                    )
                };
            }
            catch (HttpRequestException ex)
            {
                HandleOpenAIException(ex, "PhoneticAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMCulturalAnalysisResult> AnalyzeCulturalContextInternalAsync(LLMCulturalAnalysisRequest request, CancellationToken cancellationToken)
        {
            var messages = new[]
            {
                new { role = "system", content = CreateCulturalAnalysisSystemPrompt() },
                new { role = "user", content = CreateCulturalAnalysisUserPrompt(request) }
            };

            var requestBody = new
            {
                model = _config.Model,
                messages = messages,
                temperature = 0.3,
                max_tokens = 1000,
                response_format = new { type = "json_object" }
            };

            try
            {
                var response = await SendChatCompletionAsync(requestBody, cancellationToken);
                var content = response.Choices.FirstOrDefault()?.Message?.Content;

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
                        0, // Will be calculated in the actual implementation
                        response.Usage?.TotalTokens,
                        CalculateCost(response.Usage?.TotalTokens ?? 0)
                    )
                };
            }
            catch (HttpRequestException ex)
            {
                HandleOpenAIException(ex, "CulturalAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMProviderHealthResult> CheckHealthInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "user", content = "Hello, this is a health check." }
                    },
                    max_tokens = 10,
                    temperature = 0.0
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await SendChatCompletionAsync(requestBody, cancellationToken);
                stopwatch.Stop();

                return new LLMProviderHealthResult
                {
                    IsHealthy = true,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Message = "OpenAI provider is healthy",
                    LastSuccessfulCheck = DateTime.UtcNow,
                    AvailableModels = new[] { _config.Model }
                };
            }
            catch (Exception ex)
            {
                return new LLMProviderHealthResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = 0,
                    Message = $"OpenAI provider is unhealthy: {ex.Message}",
                    LastSuccessfulCheck = null,
                    AvailableModels = null
                };
            }
        }

        /// <inheritdoc />
        protected override bool ValidateProviderSpecificConfiguration()
        {
            return !string.IsNullOrWhiteSpace(_config.Model) &&
                   !string.IsNullOrWhiteSpace(_apiKey);
        }

        /// <summary>
        /// Sends a chat completion request to OpenAI API
        /// </summary>
        private async Task<ChatCompletionResponse> SendChatCompletionAsync(object requestBody, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var statusCode = (int)response.StatusCode;
                
                if (statusCode == 429)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(1);
                    throw new LLMProviderRateLimitException(ProviderType, ModelName, "API Request", retryAfter);
                }
                
                throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                    $"HTTP {statusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, JsonOptions);
            
            return result ?? throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                "Failed to deserialize API response");
        }

        /// <summary>
        /// Handles OpenAI specific exceptions
        /// </summary>
        private void HandleOpenAIException(Exception ex, string operation)
        {
            if (ex is HttpRequestException httpEx && httpEx.Message.Contains("429"))
            {
                throw new LLMProviderRateLimitException(ProviderType, ModelName, operation, TimeSpan.FromMinutes(1));
            }

            throw new LLMProviderException(ProviderType, ModelName, operation, ex.Message, ex);
        }

        /// <summary>
        /// Calculates estimated cost based on token usage
        /// </summary>
        private decimal? CalculateCost(int tokens)
        {
            // GPT-4 pricing (approximate, varies by model)
            var costPerToken = _config.Model.ToLowerInvariant() switch
            {
                var model when model.Contains("gpt-4") => 0.00003m,
                var model when model.Contains("gpt-3.5") => 0.0000015m,
                _ => 0.00002m
            };
            
            return tokens * costPerToken;
        }

        #region Prompt Creation Methods (Shared with AzureOpenAIProvider)

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

        private class ChatCompletionResponse
        {
            public List<Choice> Choices { get; set; } = new();
            public Usage? Usage { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string? Content { get; set; }
        }

        private class Usage
        {
            public int TotalTokens { get; set; }
        }

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

        public override void Dispose()
        {
            _httpClient?.Dispose();
            base.Dispose();
        }
    }
}
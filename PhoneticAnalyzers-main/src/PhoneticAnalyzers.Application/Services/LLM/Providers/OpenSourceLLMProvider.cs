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
    /// Open-source LLM provider for local models (Ollama, LM Studio, etc.)
    /// </summary>
    public class OpenSourceLLMProvider : BaseLLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiFormat;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <inheritdoc />
        public override string ProviderType => "OpenSource";

        /// <inheritdoc />
        public override string ModelName => _config.Model;

        /// <inheritdoc />
        public override bool IsAvailable => ValidateConfiguration();

        public OpenSourceLLMProvider(ILogger<OpenSourceLLMProvider> logger, LLMProviderConfiguration config, HttpClient httpClient)
            : base(logger, config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = config.Endpoint ?? "http://localhost:11434"; // Default Ollama endpoint
            _apiFormat = GetConfigValue("ApiFormat", "ollama"); // ollama, openai-compatible, custom
            
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PhoneticAnalyzers/1.0");
            _httpClient.Timeout = TimeSpan.FromMilliseconds(GetConfigValue("TimeoutMs", 60000)); // Longer timeout for local models
            
            // Add API key if provided (for some open-source API servers)
            var apiKey = GetConfigValue<string?>("ApiKey", null);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMNameAnalysisResult> AnalyzeNameInternalAsync(LLMNameAnalysisRequest request, CancellationToken cancellationToken)
        {
            var prompt = CreateComprehensiveNameAnalysisPrompt(request);

            try
            {
                var response = await SendCompletionRequestAsync(prompt, cancellationToken);
                var analysisData = ParseNameAnalysisResponse(response);

                return new LLMNameAnalysisResult
                {
                    OriginalName = request.Name,
                    Aliases = analysisData.Aliases,
                    CulturalContexts = analysisData.CulturalContexts,
                    Metadata = CreateMetadata(0) // Processing time calculated in base class
                };
            }
            catch (HttpRequestException ex)
            {
                HandleOpenSourceException(ex, "NameAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMPhoneticAnalysisResult> GeneratePhoneticVariationsInternalAsync(LLMPhoneticAnalysisRequest request, CancellationToken cancellationToken)
        {
            var prompt = CreatePhoneticAnalysisPrompt(request);

            try
            {
                var response = await SendCompletionRequestAsync(prompt, cancellationToken);
                var analysisData = ParsePhoneticAnalysisResponse(response);

                return new LLMPhoneticAnalysisResult
                {
                    OriginalName = request.Name,
                    Variations = analysisData.Variations,
                    Transliterations = analysisData.Transliterations,
                    Metadata = CreateMetadata(0)
                };
            }
            catch (HttpRequestException ex)
            {
                HandleOpenSourceException(ex, "PhoneticAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMCulturalAnalysisResult> AnalyzeCulturalContextInternalAsync(LLMCulturalAnalysisRequest request, CancellationToken cancellationToken)
        {
            var prompt = CreateCulturalAnalysisPrompt(request);

            try
            {
                var response = await SendCompletionRequestAsync(prompt, cancellationToken);
                var analysisData = ParseCulturalAnalysisResponse(response);

                return new LLMCulturalAnalysisResult
                {
                    OriginalName = request.Name,
                    CulturalOrigins = analysisData.CulturalOrigins,
                    Etymology = analysisData.Etymology,
                    HistoricalContext = analysisData.HistoricalContext,
                    Metadata = CreateMetadata(0)
                };
            }
            catch (HttpRequestException ex)
            {
                HandleOpenSourceException(ex, "CulturalAnalysis");
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<LLMProviderHealthResult> CheckHealthInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                var prompt = "Hello, this is a health check. Please respond with 'Health check successful'.";
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var response = await SendCompletionRequestAsync(prompt, cancellationToken);
                stopwatch.Stop();

                var isHealthy = !string.IsNullOrWhiteSpace(response) && 
                               response.ToLowerInvariant().Contains("health");

                return new LLMProviderHealthResult
                {
                    IsHealthy = isHealthy,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Message = isHealthy ? "Open-source LLM provider is healthy" : "Unexpected health check response",
                    LastSuccessfulCheck = isHealthy ? DateTime.UtcNow : null,
                    AvailableModels = new[] { _config.Model }
                };
            }
            catch (Exception ex)
            {
                return new LLMProviderHealthResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = 0,
                    Message = $"Open-source LLM provider is unhealthy: {ex.Message}",
                    LastSuccessfulCheck = null,
                    AvailableModels = null
                };
            }
        }

        /// <inheritdoc />
        protected override bool ValidateProviderSpecificConfiguration()
        {
            return !string.IsNullOrWhiteSpace(_config.Model) &&
                   !string.IsNullOrWhiteSpace(_baseUrl) &&
                   Uri.TryCreate(_baseUrl, UriKind.Absolute, out _);
        }

        /// <summary>
        /// Sends a completion request to the open-source LLM
        /// </summary>
        private async Task<string> SendCompletionRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            return _apiFormat.ToLowerInvariant() switch
            {
                "ollama" => await SendOllamaRequestAsync(prompt, cancellationToken),
                "openai-compatible" => await SendOpenAICompatibleRequestAsync(prompt, cancellationToken),
                "custom" => await SendCustomRequestAsync(prompt, cancellationToken),
                _ => throw new LLMProviderException(ProviderType, ModelName, "Configuration", 
                    $"Unsupported API format: {_apiFormat}")
            };
        }

        /// <summary>
        /// Sends request using Ollama API format
        /// </summary>
        private async Task<string> SendOllamaRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                model = _config.Model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = GetConfigValue("Temperature", 0.3),
                    top_p = GetConfigValue("TopP", 0.9),
                    num_predict = GetConfigValue("MaxTokens", 2000)
                }
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                    $"HTTP {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, JsonOptions);
            
            return result?.Response ?? throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                "Empty response from Ollama API");
        }

        /// <summary>
        /// Sends request using OpenAI-compatible API format
        /// </summary>
        private async Task<string> SendOpenAICompatibleRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            var messages = new[]
            {
                new { role = "user", content = prompt }
            };

            var requestBody = new
            {
                model = _config.Model,
                messages = messages,
                temperature = GetConfigValue("Temperature", 0.3),
                max_tokens = GetConfigValue("MaxTokens", 2000)
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = GetConfigValue("ChatEndpoint", "chat/completions");
            var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                    $"HTTP {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OpenAICompatibleResponse>(responseJson, JsonOptions);
            
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? 
                throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                    "Empty response from OpenAI-compatible API");
        }

        /// <summary>
        /// Sends request using custom API format
        /// </summary>
        private async Task<string> SendCustomRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            // This can be extended based on specific custom API requirements
            var customEndpoint = GetConfigValue("CustomEndpoint", "completion");
            var customPayload = GetConfigValue<Dictionary<string, object>>("CustomPayload", new());
            
            // Add the prompt to the custom payload
            customPayload["prompt"] = prompt;
            customPayload["model"] = _config.Model;

            var json = JsonSerializer.Serialize(customPayload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/{customEndpoint}", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                    $"HTTP {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseField = GetConfigValue("ResponseField", "text");
            
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson, JsonOptions);
            
            return result?.GetValueOrDefault(responseField)?.ToString() ?? 
                throw new LLMProviderException(ProviderType, ModelName, "API Request", 
                    "Empty response from custom API");
        }

        /// <summary>
        /// Handles open-source LLM exceptions
        /// </summary>
        private void HandleOpenSourceException(Exception ex, string operation)
        {
            if (ex is HttpRequestException httpEx)
            {
                if (httpEx.Message.Contains("timeout"))
                {
                    throw new LLMProviderException(ProviderType, ModelName, operation, 
                        "Request timeout - consider increasing timeout or using a smaller model", httpEx);
                }
                
                if (httpEx.Message.Contains("connection"))
                {
                    throw new LLMProviderUnavailableException(ProviderType, ModelName, 
                        "Cannot connect to LLM service - ensure it's running and accessible");
                }
            }

            throw new LLMProviderException(ProviderType, ModelName, operation, ex.Message, ex);
        }

        #region Prompt Creation Methods

        private string CreateComprehensiveNameAnalysisPrompt(LLMNameAnalysisRequest request)
        {
            var prompt = $@"You are an expert linguist. Analyze the name '{request.Name}' and provide a JSON response with aliases and cultural contexts.

Requirements:
- Generate up to {request.MaxAliases} name aliases
- Include confidence scores (0.0-1.0)
- Categorize alias types: nickname, formal, diminutive, transliteration, cultural_variant
- Identify cultural origins with confidence scores

Respond ONLY with valid JSON in this exact format:
{{
  ""aliases"": [
    {{
      ""alias"": ""example_alias"",
      ""confidence"": 0.85,
      ""aliasType"": ""nickname"",
      ""culturalContext"": ""English"",
      ""source"": ""common diminutive""
    }}
  ],
  ""culturalContexts"": [
    {{
      ""culture"": ""English"",
      ""confidence"": 0.9,
      ""language"": ""English"",
      ""region"": ""Anglo-Saxon"",
      ""notes"": ""Common in English-speaking countries""
    }}
  ]
}}";

            if (!string.IsNullOrWhiteSpace(request.CulturalHint))
            {
                prompt += $"\n\nCultural hint: {request.CulturalHint}";
            }

            return prompt;
        }

        private string CreatePhoneticAnalysisPrompt(LLMPhoneticAnalysisRequest request)
        {
            var prompt = $@"Generate phonetic variations and transliterations for the name '{request.Name}'.

Respond ONLY with valid JSON:
{{
  ""variations"": [
    {{
      ""variation"": ""phonetic_variant"",
      ""variationType"": ""phonetic_approximation"",
      ""confidence"": 0.8,
      ""algorithm"": ""manual_analysis""
    }}
  ],
  ""transliterations"": [
    {{
      ""text"": ""transliterated_text"",
      ""targetLanguage"": ""target_script"",
      ""confidence"": 0.85,
      ""method"": ""standard_transliteration""
    }}
  ]
}}";

            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
            {
                prompt += $"\n\nSource language: {request.SourceLanguage}";
            }

            return prompt;
        }

        private string CreateCulturalAnalysisPrompt(LLMCulturalAnalysisRequest request)
        {
            var prompt = $@"Analyze the cultural origins and etymology of the name '{request.Name}'.

Respond ONLY with valid JSON:
{{
  ""culturalOrigins"": [
    {{
      ""culture"": ""culture_name"",
      ""confidence"": 0.9,
      ""language"": ""language_name"",
      ""region"": ""geographic_region"",
      ""notes"": ""additional_context""
    }}
  ],
  ""etymology"": ""name_meaning_and_origin"",
  ""historicalContext"": ""historical_usage_patterns""
}}";

            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
            {
                prompt += $"\n\nAdditional context: {request.AdditionalContext}";
            }

            return prompt;
        }

        #endregion

        #region Response Parsing Methods

        private (List<LLMNameAlias> Aliases, List<LLMCulturalContext> CulturalContexts) ParseNameAnalysisResponse(string response)
        {
            try
            {
                // Try to extract JSON from response (in case model adds extra text)
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd >= jsonStart)
                {
                    var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var analysisData = JsonSerializer.Deserialize<NameAnalysisResponse>(jsonContent, JsonOptions);
                    
                    if (analysisData != null)
                    {
                        return (
                            analysisData.Aliases.Select(MapToLLMNameAlias).ToList(),
                            analysisData.CulturalContexts.Select(MapToLLMCulturalContext).ToList()
                        );
                    }
                }
                
                // Fallback: create basic analysis from response text
                return CreateFallbackNameAnalysis(response);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON response, using fallback analysis");
                return CreateFallbackNameAnalysis(response);
            }
        }

        private (List<LLMPhoneticVariation> Variations, List<LLMTransliteration> Transliterations) ParsePhoneticAnalysisResponse(string response)
        {
            try
            {
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd >= jsonStart)
                {
                    var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var analysisData = JsonSerializer.Deserialize<PhoneticAnalysisResponse>(jsonContent, JsonOptions);
                    
                    if (analysisData != null)
                    {
                        return (
                            analysisData.Variations.Select(MapToLLMPhoneticVariation).ToList(),
                            analysisData.Transliterations.Select(MapToLLMTransliteration).ToList()
                        );
                    }
                }
                
                return CreateFallbackPhoneticAnalysis(response);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON response, using fallback analysis");
                return CreateFallbackPhoneticAnalysis(response);
            }
        }

        private (List<LLMCulturalContext> CulturalOrigins, string? Etymology, string? HistoricalContext) ParseCulturalAnalysisResponse(string response)
        {
            try
            {
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd >= jsonStart)
                {
                    var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var analysisData = JsonSerializer.Deserialize<CulturalAnalysisResponse>(jsonContent, JsonOptions);
                    
                    if (analysisData != null)
                    {
                        return (
                            analysisData.CulturalOrigins.Select(MapToLLMCulturalContext).ToList(),
                            analysisData.Etymology,
                            analysisData.HistoricalContext
                        );
                    }
                }
                
                return CreateFallbackCulturalAnalysis(response);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON response, using fallback analysis");
                return CreateFallbackCulturalAnalysis(response);
            }
        }

        #endregion

        #region Fallback Analysis Methods

        private (List<LLMNameAlias> Aliases, List<LLMCulturalContext> CulturalContexts) CreateFallbackNameAnalysis(string response)
        {
            var aliases = new List<LLMNameAlias>
            {
                new LLMNameAlias
                {
                    Alias = "LLM-generated",
                    Confidence = 0.5,
                    AliasType = "generated",
                    CulturalContext = "Unknown",
                    Source = "Fallback analysis from unparseable response"
                }
            };

            var contexts = new List<LLMCulturalContext>
            {
                new LLMCulturalContext
                {
                    Culture = "Unknown",
                    Confidence = 0.3,
                    Language = null,
                    Region = null,
                    Notes = "Could not parse cultural context from response"
                }
            };

            return (aliases, contexts);
        }

        private (List<LLMPhoneticVariation> Variations, List<LLMTransliteration> Transliterations) CreateFallbackPhoneticAnalysis(string response)
        {
            var variations = new List<LLMPhoneticVariation>
            {
                new LLMPhoneticVariation
                {
                    Variation = "phonetic-fallback",
                    VariationType = "fallback",
                    Confidence = 0.3,
                    Algorithm = "fallback"
                }
            };

            var transliterations = new List<LLMTransliteration>();
            return (variations, transliterations);
        }

        private (List<LLMCulturalContext> CulturalOrigins, string? Etymology, string? HistoricalContext) CreateFallbackCulturalAnalysis(string response)
        {
            var origins = new List<LLMCulturalContext>
            {
                new LLMCulturalContext
                {
                    Culture = "Unknown",
                    Confidence = 0.3,
                    Language = null,
                    Region = null,
                    Notes = "Fallback analysis - could not parse response"
                }
            };

            return (origins, "Etymology unavailable", "Historical context unavailable");
        }

        #endregion

        #region Response Mapping Methods

        private LLMNameAlias MapToLLMNameAlias(NameAliasResponse alias)
        {
            return new LLMNameAlias
            {
                Alias = alias.Alias,
                Confidence = Math.Max(0, Math.Min(1, alias.Confidence)), // Clamp to 0-1
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
                Confidence = Math.Max(0, Math.Min(1, context.Confidence)), // Clamp to 0-1
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
                Confidence = Math.Max(0, Math.Min(1, variation.Confidence)), // Clamp to 0-1
                Algorithm = variation.Algorithm
            };
        }

        private LLMTransliteration MapToLLMTransliteration(TransliterationResponse transliteration)
        {
            return new LLMTransliteration
            {
                Text = transliteration.Text,
                TargetLanguage = transliteration.TargetLanguage,
                Confidence = Math.Max(0, Math.Min(1, transliteration.Confidence)), // Clamp to 0-1
                Method = transliteration.Method
            };
        }

        #endregion

        #region Response Models

        // Ollama API response format
        private class OllamaResponse
        {
            public string? Response { get; set; }
        }

        // OpenAI-compatible API response format
        private class OpenAICompatibleResponse
        {
            public List<Choice>? Choices { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string? Content { get; set; }
        }

        // Analysis response models (same as other providers)
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
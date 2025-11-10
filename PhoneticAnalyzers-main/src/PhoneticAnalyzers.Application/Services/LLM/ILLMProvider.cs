using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services.LLM
{
    /// <summary>
    /// Defines a contract for Language Model providers with support for multiple LLM services
    /// (OpenAI, Azure OpenAI, Anthropic, open-source models, etc.)
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// Gets the provider type (OpenAI, AzureOpenAI, Anthropic, etc.)
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Gets the model name being used
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Indicates if the provider is available and configured
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Analyzes a name and generates cultural variants, nicknames, and aliases
        /// </summary>
        /// <param name="request">The name analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Name analysis results with cultural context and aliases</returns>
        Task<LLMNameAnalysisResult> AnalyzeNameAsync(LLMNameAnalysisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates phonetic variations and transliterations for a name
        /// </summary>
        /// <param name="request">The phonetic analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Phonetic analysis results with variations</returns>
        Task<LLMPhoneticAnalysisResult> GeneratePhoneticVariationsAsync(LLMPhoneticAnalysisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines the cultural origin and context of a name
        /// </summary>
        /// <param name="request">The cultural analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cultural analysis results with confidence scores</returns>
        Task<LLMCulturalAnalysisResult> AnalyzeCulturalContextAsync(LLMCulturalAnalysisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates provider configuration and connectivity
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check results</returns>
        Task<LLMProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request model for name analysis
    /// </summary>
    public class LLMNameAnalysisRequest
    {
        /// <summary>
        /// The name to analyze
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Optional cultural context hint
        /// </summary>
        public string? CulturalHint { get; init; }

        /// <summary>
        /// Maximum number of aliases to generate
        /// </summary>
        public int MaxAliases { get; init; } = 10;

        /// <summary>
        /// Include phonetic variations
        /// </summary>
        public bool IncludePhonetic { get; init; } = true;

        /// <summary>
        /// Include nickname variations
        /// </summary>
        public bool IncludeNicknames { get; init; } = true;

        /// <summary>
        /// Include transliterations
        /// </summary>
        public bool IncludeTransliterations { get; init; } = true;
    }

    /// <summary>
    /// Result model for name analysis
    /// </summary>
    public class LLMNameAnalysisResult
    {
        /// <summary>
        /// The original name analyzed
        /// </summary>
        public required string OriginalName { get; init; }

        /// <summary>
        /// Generated aliases with confidence scores
        /// </summary>
        public required IReadOnlyList<LLMNameAlias> Aliases { get; init; }

        /// <summary>
        /// Detected cultural contexts
        /// </summary>
        public required IReadOnlyList<LLMCulturalContext> CulturalContexts { get; init; }

        /// <summary>
        /// Analysis metadata
        /// </summary>
        public required LLMAnalysisMetadata Metadata { get; init; }
    }

    /// <summary>
    /// Request model for phonetic analysis
    /// </summary>
    public class LLMPhoneticAnalysisRequest
    {
        /// <summary>
        /// The name to analyze phonetically
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Source language/script if known
        /// </summary>
        public string? SourceLanguage { get; init; }

        /// <summary>
        /// Target languages for transliteration
        /// </summary>
        public IReadOnlyList<string> TargetLanguages { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Include phonetic codes (Soundex, Metaphone, etc.)
        /// </summary>
        public bool IncludePhoneticCodes { get; init; } = true;
    }

    /// <summary>
    /// Result model for phonetic analysis
    /// </summary>
    public class LLMPhoneticAnalysisResult
    {
        /// <summary>
        /// The original name analyzed
        /// </summary>
        public required string OriginalName { get; init; }

        /// <summary>
        /// Phonetic variations
        /// </summary>
        public required IReadOnlyList<LLMPhoneticVariation> Variations { get; init; }

        /// <summary>
        /// Transliterations to different scripts
        /// </summary>
        public required IReadOnlyList<LLMTransliteration> Transliterations { get; init; }

        /// <summary>
        /// Analysis metadata
        /// </summary>
        public required LLMAnalysisMetadata Metadata { get; init; }
    }

    /// <summary>
    /// Request model for cultural analysis
    /// </summary>
    public class LLMCulturalAnalysisRequest
    {
        /// <summary>
        /// The name to analyze culturally
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Additional context (surname, location, etc.)
        /// </summary>
        public string? AdditionalContext { get; init; }

        /// <summary>
        /// Include historical context
        /// </summary>
        public bool IncludeHistoricalContext { get; init; } = false;
    }

    /// <summary>
    /// Result model for cultural analysis
    /// </summary>
    public class LLMCulturalAnalysisResult
    {
        /// <summary>
        /// The original name analyzed
        /// </summary>
        public required string OriginalName { get; init; }

        /// <summary>
        /// Detected cultural origins
        /// </summary>
        public required IReadOnlyList<LLMCulturalContext> CulturalOrigins { get; init; }

        /// <summary>
        /// Name meaning and etymology if available
        /// </summary>
        public string? Etymology { get; init; }

        /// <summary>
        /// Historical context if requested
        /// </summary>
        public string? HistoricalContext { get; init; }

        /// <summary>
        /// Analysis metadata
        /// </summary>
        public required LLMAnalysisMetadata Metadata { get; init; }
    }

    /// <summary>
    /// Represents a name alias with confidence scoring
    /// </summary>
    public class LLMNameAlias
    {
        /// <summary>
        /// The alias text
        /// </summary>
        public required string Alias { get; init; }

        /// <summary>
        /// Confidence score (0.0 - 1.0)
        /// </summary>
        public required double Confidence { get; init; }

        /// <summary>
        /// Type of alias (nickname, formal, transliteration, etc.)
        /// </summary>
        public required string AliasType { get; init; }

        /// <summary>
        /// Cultural context for this alias
        /// </summary>
        public string? CulturalContext { get; init; }

        /// <summary>
        /// Source or reasoning for the alias
        /// </summary>
        public string? Source { get; init; }
    }

    /// <summary>
    /// Represents cultural context information
    /// </summary>
    public class LLMCulturalContext
    {
        /// <summary>
        /// Culture or region identifier
        /// </summary>
        public required string Culture { get; init; }

        /// <summary>
        /// Confidence score for this cultural association
        /// </summary>
        public required double Confidence { get; init; }

        /// <summary>
        /// Language associated with this culture
        /// </summary>
        public string? Language { get; init; }

        /// <summary>
        /// Geographic region
        /// </summary>
        public string? Region { get; init; }

        /// <summary>
        /// Additional cultural notes
        /// </summary>
        public string? Notes { get; init; }
    }

    /// <summary>
    /// Represents a phonetic variation
    /// </summary>
    public class LLMPhoneticVariation
    {
        /// <summary>
        /// The phonetic variation
        /// </summary>
        public required string Variation { get; init; }

        /// <summary>
        /// Type of phonetic variation
        /// </summary>
        public required string VariationType { get; init; }

        /// <summary>
        /// Confidence score
        /// </summary>
        public required double Confidence { get; init; }

        /// <summary>
        /// Phonetic algorithm used (if applicable)
        /// </summary>
        public string? Algorithm { get; init; }
    }

    /// <summary>
    /// Represents a transliteration
    /// </summary>
    public class LLMTransliteration
    {
        /// <summary>
        /// The transliterated text
        /// </summary>
        public required string Text { get; init; }

        /// <summary>
        /// Target language/script
        /// </summary>
        public required string TargetLanguage { get; init; }

        /// <summary>
        /// Confidence score
        /// </summary>
        public required double Confidence { get; init; }

        /// <summary>
        /// Transliteration method used
        /// </summary>
        public string? Method { get; init; }
    }

    /// <summary>
    /// Analysis metadata
    /// </summary>
    public class LLMAnalysisMetadata
    {
        /// <summary>
        /// Provider that performed the analysis
        /// </summary>
        public required string Provider { get; init; }

        /// <summary>
        /// Model used for analysis
        /// </summary>
        public required string Model { get; init; }

        /// <summary>
        /// Analysis timestamp
        /// </summary>
        public required DateTime AnalyzedAt { get; init; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public required long ProcessingTimeMs { get; init; }

        /// <summary>
        /// Token usage (if applicable)
        /// </summary>
        public int? TokensUsed { get; init; }

        /// <summary>
        /// Cost (if applicable)
        /// </summary>
        public decimal? Cost { get; init; }
    }

    /// <summary>
    /// Health check result for LLM provider
    /// </summary>
    public class LLMProviderHealthResult
    {
        /// <summary>
        /// Is the provider healthy
        /// </summary>
        public required bool IsHealthy { get; init; }

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public required long ResponseTimeMs { get; init; }

        /// <summary>
        /// Health check message
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Last successful check timestamp
        /// </summary>
        public DateTime? LastSuccessfulCheck { get; init; }

        /// <summary>
        /// Available models (if retrievable)
        /// </summary>
        public IReadOnlyList<string>? AvailableModels { get; init; }
    }
}
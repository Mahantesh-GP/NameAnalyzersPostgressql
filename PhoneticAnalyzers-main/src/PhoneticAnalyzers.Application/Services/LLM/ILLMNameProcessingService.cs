using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services.LLM
{
    /// <summary>
    /// Main service interface for LLM-based name processing with provider abstraction
    /// </summary>
    public interface ILLMNameProcessingService
    {
        /// <summary>
        /// Gets the currently active LLM provider
        /// </summary>
        string CurrentProvider { get; }

        /// <summary>
        /// Gets all available providers
        /// </summary>
        IReadOnlyList<string> AvailableProviders { get; }

        /// <summary>
        /// Switches to a different LLM provider
        /// </summary>
        /// <param name="providerType">The provider type to switch to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if switch was successful</returns>
        Task<bool> SwitchProviderAsync(string providerType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs comprehensive name analysis using the current LLM provider
        /// </summary>
        /// <param name="request">The analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive name analysis results</returns>
        Task<ComprehensiveNameAnalysisResult> AnalyzeNameAsync(ComprehensiveNameAnalysisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates aliases for a batch of names efficiently
        /// </summary>
        /// <param name="request">The batch analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Batch analysis results</returns>
        Task<BatchNameAnalysisResult> AnalyzeNamesAsync(BatchNameAnalysisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates name similarity and suggests corrections
        /// </summary>
        /// <param name="request">The similarity analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Similarity analysis results</returns>
        Task<NameSimilarityAnalysisResult> AnalyzeSimilarityAsync(NameSimilarityAnalysisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets health status of all LLM providers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health status of all providers</returns>
        Task<LLMServiceHealthResult> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration for LLM providers
    /// </summary>
    public class LLMConfiguration
    {
        /// <summary>
        /// Default provider to use
        /// </summary>
        public string DefaultProvider { get; set; } = "AzureOpenAI";

        /// <summary>
        /// Provider-specific configurations
        /// </summary>
        public Dictionary<string, LLMProviderConfiguration> Providers { get; set; } = new();

        /// <summary>
        /// Global settings
        /// </summary>
        public LLMGlobalSettings GlobalSettings { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a specific LLM provider
    /// </summary>
    public class LLMProviderConfiguration
    {
        /// <summary>
        /// Is this provider enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Provider-specific settings
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new();

        /// <summary>
        /// Model to use for this provider
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint (for custom deployments)
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Authentication configuration
        /// </summary>
        public LLMAuthenticationConfiguration Authentication { get; set; } = new();

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public LLMRateLimitConfiguration RateLimit { get; set; } = new();
    }

    /// <summary>
    /// Authentication configuration for LLM providers
    /// </summary>
    public class LLMAuthenticationConfiguration
    {
        /// <summary>
        /// Authentication type (ManagedIdentity, ApiKey, ServicePrincipal)
        /// </summary>
        public string Type { get; set; } = "ManagedIdentity";

        /// <summary>
        /// Key Vault secret name for API key (if using ApiKey auth)
        /// </summary>
        public string? KeyVaultSecretName { get; set; }

        /// <summary>
        /// Client ID for Service Principal authentication
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Tenant ID for Service Principal authentication
        /// </summary>
        public string? TenantId { get; set; }
    }

    /// <summary>
    /// Rate limiting configuration for LLM providers
    /// </summary>
    public class LLMRateLimitConfiguration
    {
        /// <summary>
        /// Maximum requests per minute
        /// </summary>
        public int RequestsPerMinute { get; set; } = 60;

        /// <summary>
        /// Maximum tokens per minute
        /// </summary>
        public int TokensPerMinute { get; set; } = 10000;

        /// <summary>
        /// Maximum concurrent requests
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 5;
    }

    /// <summary>
    /// Global LLM settings
    /// </summary>
    public class LLMGlobalSettings
    {
        /// <summary>
        /// Request timeout in milliseconds
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Retry attempts for failed requests
        /// </summary>
        public int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Enable caching of LLM responses
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Cache TTL in minutes
        /// </summary>
        public int CacheTtlMinutes { get; set; } = 60;

        /// <summary>
        /// Enable telemetry and logging
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;
    }

    /// <summary>
    /// Comprehensive name analysis request
    /// </summary>
    public class ComprehensiveNameAnalysisRequest
    {
        /// <summary>
        /// The name to analyze
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Optional surname for context
        /// </summary>
        public string? Surname { get; init; }

        /// <summary>
        /// Cultural hints
        /// </summary>
        public IReadOnlyList<string> CulturalHints { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Analysis options
        /// </summary>
        public NameAnalysisOptions Options { get; init; } = new();

        /// <summary>
        /// Request metadata
        /// </summary>
        public NameAnalysisRequestMetadata Metadata { get; init; } = new();
    }

    /// <summary>
    /// Name analysis options
    /// </summary>
    public class NameAnalysisOptions
    {
        /// <summary>
        /// Include phonetic analysis
        /// </summary>
        public bool IncludePhonetic { get; init; } = true;

        /// <summary>
        /// Include cultural analysis
        /// </summary>
        public bool IncludeCultural { get; init; } = true;

        /// <summary>
        /// Include nickname generation
        /// </summary>
        public bool IncludeNicknames { get; init; } = true;

        /// <summary>
        /// Include transliterations
        /// </summary>
        public bool IncludeTransliterations { get; init; } = true;

        /// <summary>
        /// Maximum aliases to generate
        /// </summary>
        public int MaxAliases { get; init; } = 10;

        /// <summary>
        /// Minimum confidence threshold
        /// </summary>
        public double MinConfidenceThreshold { get; init; } = 0.5;
    }

    /// <summary>
    /// Request metadata
    /// </summary>
    public class NameAnalysisRequestMetadata
    {
        /// <summary>
        /// Request ID for tracking
        /// </summary>
        public string RequestId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// User ID or system making the request
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// Request timestamp
        /// </summary>
        public DateTime RequestedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Additional context
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; init; } = new();
    }

    /// <summary>
    /// Comprehensive name analysis result
    /// </summary>
    public class ComprehensiveNameAnalysisResult
    {
        /// <summary>
        /// Original request
        /// </summary>
        public required ComprehensiveNameAnalysisRequest Request { get; init; }

        /// <summary>
        /// LLM name analysis results
        /// </summary>
        public LLMNameAnalysisResult? NameAnalysis { get; init; }

        /// <summary>
        /// LLM phonetic analysis results
        /// </summary>
        public LLMPhoneticAnalysisResult? PhoneticAnalysis { get; init; }

        /// <summary>
        /// LLM cultural analysis results
        /// </summary>
        public LLMCulturalAnalysisResult? CulturalAnalysis { get; init; }

        /// <summary>
        /// Combined aliases from all analyses
        /// </summary>
        public required IReadOnlyList<LLMNameAlias> CombinedAliases { get; init; }

        /// <summary>
        /// Analysis summary and recommendations
        /// </summary>
        public required AnalysisSummary Summary { get; init; }

        /// <summary>
        /// Processing metadata
        /// </summary>
        public required AnalysisResultMetadata Metadata { get; init; }
    }

    /// <summary>
    /// Batch name analysis request
    /// </summary>
    public class BatchNameAnalysisRequest
    {
        /// <summary>
        /// Names to analyze
        /// </summary>
        public required IReadOnlyList<ComprehensiveNameAnalysisRequest> Names { get; init; }

        /// <summary>
        /// Batch processing options
        /// </summary>
        public BatchProcessingOptions Options { get; init; } = new();

        /// <summary>
        /// Progress callback for tracking
        /// </summary>
        public IProgress<BatchProcessingProgress>? ProgressCallback { get; init; }
    }

    /// <summary>
    /// Batch processing options
    /// </summary>
    public class BatchProcessingOptions
    {
        /// <summary>
        /// Maximum concurrent operations
        /// </summary>
        public int MaxConcurrency { get; init; } = 3;

        /// <summary>
        /// Continue on individual failures
        /// </summary>
        public bool ContinueOnError { get; init; } = true;

        /// <summary>
        /// Batch size for chunked processing
        /// </summary>
        public int BatchSize { get; init; } = 10;
    }

    /// <summary>
    /// Batch processing progress
    /// </summary>
    public class BatchProcessingProgress
    {
        /// <summary>
        /// Total items to process
        /// </summary>
        public required int TotalItems { get; init; }

        /// <summary>
        /// Items processed so far
        /// </summary>
        public required int ProcessedItems { get; init; }

        /// <summary>
        /// Items failed
        /// </summary>
        public required int FailedItems { get; init; }

        /// <summary>
        /// Current item being processed
        /// </summary>
        public string? CurrentItem { get; init; }

        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; init; }
    }

    /// <summary>
    /// Batch name analysis result
    /// </summary>
    public class BatchNameAnalysisResult
    {
        /// <summary>
        /// Original request
        /// </summary>
        public required BatchNameAnalysisRequest Request { get; init; }

        /// <summary>
        /// Individual analysis results
        /// </summary>
        public required IReadOnlyList<ComprehensiveNameAnalysisResult> Results { get; init; }

        /// <summary>
        /// Failed analyses
        /// </summary>
        public required IReadOnlyList<FailedNameAnalysis> Failures { get; init; }

        /// <summary>
        /// Batch processing metadata
        /// </summary>
        public required BatchProcessingMetadata Metadata { get; init; }
    }

    /// <summary>
    /// Failed name analysis information
    /// </summary>
    public class FailedNameAnalysis
    {
        /// <summary>
        /// The request that failed
        /// </summary>
        public required ComprehensiveNameAnalysisRequest Request { get; init; }

        /// <summary>
        /// Error message
        /// </summary>
        public required string ErrorMessage { get; init; }

        /// <summary>
        /// Exception details if available
        /// </summary>
        public string? ExceptionDetails { get; init; }

        /// <summary>
        /// Failure timestamp
        /// </summary>
        public required DateTime FailedAt { get; init; }
    }

    /// <summary>
    /// Name similarity analysis request
    /// </summary>
    public class NameSimilarityAnalysisRequest
    {
        /// <summary>
        /// Primary name
        /// </summary>
        public required string PrimaryName { get; init; }

        /// <summary>
        /// Names to compare against
        /// </summary>
        public required IReadOnlyList<string> ComparisonNames { get; init; }

        /// <summary>
        /// Similarity threshold
        /// </summary>
        public double SimilarityThreshold { get; init; } = 0.7;

        /// <summary>
        /// Cultural context for comparison
        /// </summary>
        public string? CulturalContext { get; init; }
    }

    /// <summary>
    /// Name similarity analysis result
    /// </summary>
    public class NameSimilarityAnalysisResult
    {
        /// <summary>
        /// Original request
        /// </summary>
        public required NameSimilarityAnalysisRequest Request { get; init; }

        /// <summary>
        /// Similarity scores and analysis
        /// </summary>
        public required IReadOnlyList<NameSimilarityScore> Similarities { get; init; }

        /// <summary>
        /// Suggested corrections or alternatives
        /// </summary>
        public required IReadOnlyList<string> Suggestions { get; init; }

        /// <summary>
        /// Analysis metadata
        /// </summary>
        public required LLMAnalysisMetadata Metadata { get; init; }
    }

    /// <summary>
    /// Name similarity score
    /// </summary>
    public class NameSimilarityScore
    {
        /// <summary>
        /// The compared name
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Similarity score (0.0 - 1.0)
        /// </summary>
        public required double Score { get; init; }

        /// <summary>
        /// Type of similarity (phonetic, lexical, cultural, etc.)
        /// </summary>
        public required string SimilarityType { get; init; }

        /// <summary>
        /// Explanation of the similarity
        /// </summary>
        public string? Explanation { get; init; }
    }

    /// <summary>
    /// Analysis summary
    /// </summary>
    public class AnalysisSummary
    {
        /// <summary>
        /// Primary cultural origin determined
        /// </summary>
        public string? PrimaryCulture { get; init; }

        /// <summary>
        /// Confidence in cultural determination
        /// </summary>
        public double CulturalConfidence { get; init; }

        /// <summary>
        /// Most common alias type found
        /// </summary>
        public string? MostCommonAliasType { get; init; }

        /// <summary>
        /// Number of high-confidence aliases
        /// </summary>
        public int HighConfidenceAliasCount { get; init; }

        /// <summary>
        /// Recommendations for further processing
        /// </summary>
        public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Analysis result metadata
    /// </summary>
    public class AnalysisResultMetadata
    {
        /// <summary>
        /// Provider used for analysis
        /// </summary>
        public required string Provider { get; init; }

        /// <summary>
        /// Total processing time
        /// </summary>
        public required TimeSpan ProcessingTime { get; init; }

        /// <summary>
        /// Total tokens used
        /// </summary>
        public int? TotalTokensUsed { get; init; }

        /// <summary>
        /// Estimated cost
        /// </summary>
        public decimal? EstimatedCost { get; init; }

        /// <summary>
        /// Cache hit rate
        /// </summary>
        public double? CacheHitRate { get; init; }
    }

    /// <summary>
    /// Batch processing metadata
    /// </summary>
    public class BatchProcessingMetadata
    {
        /// <summary>
        /// Total processing time
        /// </summary>
        public required TimeSpan TotalProcessingTime { get; init; }

        /// <summary>
        /// Average processing time per item
        /// </summary>
        public required TimeSpan AverageProcessingTime { get; init; }

        /// <summary>
        /// Success rate
        /// </summary>
        public required double SuccessRate { get; init; }

        /// <summary>
        /// Total cost if available
        /// </summary>
        public decimal? TotalCost { get; init; }

        /// <summary>
        /// Provider used
        /// </summary>
        public required string Provider { get; init; }
    }

    /// <summary>
    /// LLM service health result
    /// </summary>
    public class LLMServiceHealthResult
    {
        /// <summary>
        /// Overall service health
        /// </summary>
        public required bool IsHealthy { get; init; }

        /// <summary>
        /// Currently active provider
        /// </summary>
        public required string ActiveProvider { get; init; }

        /// <summary>
        /// Health status of all providers
        /// </summary>
        public required Dictionary<string, LLMProviderHealthResult> ProviderHealth { get; init; }

        /// <summary>
        /// Service-level statistics
        /// </summary>
        public LLMServiceStatistics? Statistics { get; init; }
    }

    /// <summary>
    /// LLM service statistics
    /// </summary>
    public class LLMServiceStatistics
    {
        /// <summary>
        /// Total requests processed
        /// </summary>
        public long TotalRequests { get; init; }

        /// <summary>
        /// Total failures
        /// </summary>
        public long TotalFailures { get; init; }

        /// <summary>
        /// Average response time
        /// </summary>
        public double AverageResponseTimeMs { get; init; }

        /// <summary>
        /// Cache hit rate
        /// </summary>
        public double CacheHitRate { get; init; }

        /// <summary>
        /// Total cost (if tracked)
        /// </summary>
        public decimal? TotalCost { get; init; }
    }
}
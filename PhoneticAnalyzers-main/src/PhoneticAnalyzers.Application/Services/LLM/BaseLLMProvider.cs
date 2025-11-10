using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services.LLM
{
    /// <summary>
    /// Base class for LLM providers with common functionality
    /// </summary>
    public abstract class BaseLLMProvider : ILLMProvider
    {
        protected readonly ILogger<BaseLLMProvider> _logger;
        protected readonly LLMProviderConfiguration _config;

        /// <inheritdoc />
        public abstract string ProviderType { get; }

        /// <inheritdoc />
        public abstract string ModelName { get; }

        /// <inheritdoc />
        public abstract bool IsAvailable { get; }

        protected BaseLLMProvider(ILogger<BaseLLMProvider> logger, LLMProviderConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public async Task<LLMNameAnalysisResult> AnalyzeNameAsync(LLMNameAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be null or empty", nameof(request));

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Starting name analysis for '{Name}' using provider '{Provider}'", 
                    request.Name, ProviderType);

                var result = await AnalyzeNameInternalAsync(request, cancellationToken);
                
                stopwatch.Stop();
                
                _logger.LogInformation("Completed name analysis for '{Name}' in {ElapsedMs}ms, generated {AliasCount} aliases",
                    request.Name, stopwatch.ElapsedMilliseconds, result.Aliases.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to analyze name '{Name}' using provider '{Provider}' after {ElapsedMs}ms",
                    request.Name, ProviderType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LLMPhoneticAnalysisResult> GeneratePhoneticVariationsAsync(LLMPhoneticAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be null or empty", nameof(request));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting phonetic analysis for '{Name}' using provider '{Provider}'",
                    request.Name, ProviderType);

                var result = await GeneratePhoneticVariationsInternalAsync(request, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation("Completed phonetic analysis for '{Name}' in {ElapsedMs}ms, generated {VariationCount} variations",
                    request.Name, stopwatch.ElapsedMilliseconds, result.Variations.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to generate phonetic variations for '{Name}' using provider '{Provider}' after {ElapsedMs}ms",
                    request.Name, ProviderType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LLMCulturalAnalysisResult> AnalyzeCulturalContextAsync(LLMCulturalAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be null or empty", nameof(request));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting cultural analysis for '{Name}' using provider '{Provider}'",
                    request.Name, ProviderType);

                var result = await AnalyzeCulturalContextInternalAsync(request, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation("Completed cultural analysis for '{Name}' in {ElapsedMs}ms, found {OriginCount} cultural origins",
                    request.Name, stopwatch.ElapsedMilliseconds, result.CulturalOrigins.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to analyze cultural context for '{Name}' using provider '{Provider}' after {ElapsedMs}ms",
                    request.Name, ProviderType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LLMProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Performing health check for provider '{Provider}'", ProviderType);

                var result = await CheckHealthInternalAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation("Health check completed for provider '{Provider}' in {ElapsedMs}ms, status: {IsHealthy}",
                    ProviderType, stopwatch.ElapsedMilliseconds, result.IsHealthy);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Health check failed for provider '{Provider}' after {ElapsedMs}ms",
                    ProviderType, stopwatch.ElapsedMilliseconds);

                return new LLMProviderHealthResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Message = $"Health check failed: {ex.Message}",
                    LastSuccessfulCheck = null,
                    AvailableModels = null
                };
            }
        }

        /// <summary>
        /// Provider-specific implementation of name analysis
        /// </summary>
        protected abstract Task<LLMNameAnalysisResult> AnalyzeNameInternalAsync(LLMNameAnalysisRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Provider-specific implementation of phonetic analysis
        /// </summary>
        protected abstract Task<LLMPhoneticAnalysisResult> GeneratePhoneticVariationsInternalAsync(LLMPhoneticAnalysisRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Provider-specific implementation of cultural analysis
        /// </summary>
        protected abstract Task<LLMCulturalAnalysisResult> AnalyzeCulturalContextInternalAsync(LLMCulturalAnalysisRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Provider-specific implementation of health check
        /// </summary>
        protected abstract Task<LLMProviderHealthResult> CheckHealthInternalAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Creates standard metadata for analysis results
        /// </summary>
        protected LLMAnalysisMetadata CreateMetadata(long processingTimeMs, int? tokensUsed = null, decimal? cost = null)
        {
            return new LLMAnalysisMetadata
            {
                Provider = ProviderType,
                Model = ModelName,
                AnalyzedAt = DateTime.UtcNow,
                ProcessingTimeMs = processingTimeMs,
                TokensUsed = tokensUsed,
                Cost = cost
            };
        }

        /// <summary>
        /// Validates configuration settings
        /// </summary>
        protected virtual bool ValidateConfiguration()
        {
            try
            {
                return _config.Enabled &&
                       !string.IsNullOrWhiteSpace(_config.Model) &&
                       ValidateProviderSpecificConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration validation failed for provider '{Provider}'", ProviderType);
                return false;
            }
        }

        /// <summary>
        /// Provider-specific configuration validation
        /// </summary>
        protected abstract bool ValidateProviderSpecificConfiguration();

        /// <summary>
        /// Gets a configuration value with type conversion
        /// </summary>
        protected T GetConfigValue<T>(string key, T? defaultValue = default)
        {
            if (_config.Settings.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T))!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert config value for key '{Key}' to type {Type}, using default",
                        key, typeof(T).Name);
                }
            }

            return defaultValue!;
        }

        /// <summary>
        /// Creates a timeout CancellationToken based on configuration
        /// </summary>
        protected CancellationToken CreateTimeoutToken(CancellationToken cancellationToken = default)
        {
            var timeoutMs = GetConfigValue("TimeoutMs", 30000);
            using var timeoutToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            
            return CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, 
                timeoutToken.Token
            ).Token;
        }

        /// <summary>
        /// Logs performance metrics
        /// </summary>
        protected void LogPerformanceMetrics(string operation, string name, long elapsedMs, int? tokensUsed = null, decimal? cost = null)
        {
            var metricsData = new
            {
                Provider = ProviderType,
                Model = ModelName,
                Operation = operation,
                Name = name,
                ElapsedMs = elapsedMs,
                TokensUsed = tokensUsed,
                Cost = cost
            };

            _logger.LogInformation("Performance metrics: {@Metrics}", metricsData);
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public virtual void Dispose()
        {
            // Base implementation - override if needed
        }
    }

    /// <summary>
    /// Exception thrown when LLM provider operations fail
    /// </summary>
    public class LLMProviderException : Exception
    {
        /// <summary>
        /// The provider that threw the exception
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// The model being used
        /// </summary>
        public string Model { get; }

        /// <summary>
        /// The operation that failed
        /// </summary>
        public string Operation { get; }

        public LLMProviderException(string provider, string model, string operation, string message)
            : base($"LLM Provider '{provider}' model '{model}' failed during '{operation}': {message}")
        {
            Provider = provider;
            Model = model;
            Operation = operation;
        }

        public LLMProviderException(string provider, string model, string operation, string message, Exception innerException)
            : base($"LLM Provider '{provider}' model '{model}' failed during '{operation}': {message}", innerException)
        {
            Provider = provider;
            Model = model;
            Operation = operation;
        }
    }

    /// <summary>
    /// Exception thrown when LLM provider is not available or configured
    /// </summary>
    public class LLMProviderUnavailableException : LLMProviderException
    {
        public LLMProviderUnavailableException(string provider, string model, string reason)
            : base(provider, model, "Availability Check", $"Provider is not available: {reason}")
        {
        }
    }

    /// <summary>
    /// Exception thrown when LLM provider rate limits are exceeded
    /// </summary>
    public class LLMProviderRateLimitException : LLMProviderException
    {
        /// <summary>
        /// Time to wait before retrying
        /// </summary>
        public TimeSpan RetryAfter { get; }

        public LLMProviderRateLimitException(string provider, string model, string operation, TimeSpan retryAfter)
            : base(provider, model, operation, $"Rate limit exceeded, retry after {retryAfter}")
        {
            RetryAfter = retryAfter;
        }
    }
}
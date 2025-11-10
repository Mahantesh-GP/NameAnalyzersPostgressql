using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhoneticAnalyzers.Application.Services.LLM.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services.LLM
{
    /// <summary>
    /// Main implementation of ILLMNameProcessingService with provider management
    /// </summary>
    public class LLMNameProcessingService : ILLMNameProcessingService, IDisposable
    {
        private readonly ILogger<LLMNameProcessingService> _logger;
        private readonly IMemoryCache _cache;
        private readonly ILLMCacheService _smartCache;
        private readonly LLMConfiguration _config;
        private readonly ConcurrentDictionary<string, ILLMProvider> _providers;
        private readonly SemaphoreSlim _providerSwitchLock;
        
        private ILLMProvider? _currentProvider;
        private readonly object _currentProviderLock = new();

        /// <inheritdoc />
        public string CurrentProvider => _currentProvider?.ProviderType ?? "None";

        /// <inheritdoc />
        public IReadOnlyList<string> AvailableProviders => _providers.Keys.ToList();

        public LLMNameProcessingService(
            ILogger<LLMNameProcessingService> logger,
            IMemoryCache cache,
            ILLMCacheService smartCache,
            IOptions<LLMConfiguration> config,
            IEnumerable<ILLMProvider> providers)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _smartCache = smartCache ?? throw new ArgumentNullException(nameof(smartCache));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            
            _providers = new ConcurrentDictionary<string, ILLMProvider>();
            _providerSwitchLock = new SemaphoreSlim(1, 1);

            // Register all provided LLM providers
            foreach (var provider in providers ?? Enumerable.Empty<ILLMProvider>())
            {
                _providers.TryAdd(provider.ProviderType, provider);
                _logger.LogInformation("Registered LLM provider: {Provider} (Available: {IsAvailable})", 
                    provider.ProviderType, provider.IsAvailable);
            }

            // Set initial provider
            InitializeDefaultProvider();
        }

        /// <inheritdoc />
        public async Task<bool> SwitchProviderAsync(string providerType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(providerType))
                throw new ArgumentException("Provider type cannot be null or empty", nameof(providerType));

            await _providerSwitchLock.WaitAsync(cancellationToken);
            try
            {
                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    _logger.LogWarning("Attempted to switch to unknown provider: {ProviderType}", providerType);
                    return false;
                }

                if (!provider.IsAvailable)
                {
                    _logger.LogWarning("Attempted to switch to unavailable provider: {ProviderType}", providerType);
                    return false;
                }

                // Test provider health before switching
                var healthResult = await provider.CheckHealthAsync(cancellationToken);
                if (!healthResult.IsHealthy)
                {
                    _logger.LogWarning("Provider {ProviderType} failed health check: {Message}", 
                        providerType, healthResult.Message);
                    return false;
                }

                lock (_currentProviderLock)
                {
                    _currentProvider = provider;
                }

                _logger.LogInformation("Successfully switched to LLM provider: {ProviderType}", providerType);
                return true;
            }
            finally
            {
                _providerSwitchLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<ComprehensiveNameAnalysisResult> AnalyzeNameAsync(ComprehensiveNameAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var provider = GetCurrentProvider();
            var cacheKey = CreateCacheKey("name-analysis", request.Name, request.CulturalHints, request.Options);
            
            // Check cache first
            if (_config.GlobalSettings.EnableCaching && 
                _cache.TryGetValue(cacheKey, out ComprehensiveNameAnalysisResult? cachedResult))
            {
                _logger.LogDebug("Retrieved cached name analysis for '{Name}'", request.Name);
                return cachedResult!;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Starting comprehensive name analysis for '{Name}' using provider '{Provider}'",
                    request.Name, provider.ProviderType);

                var tasks = new List<Task>();
                LLMNameAnalysisResult? nameAnalysis = null;
                LLMPhoneticAnalysisResult? phoneticAnalysis = null;
                LLMCulturalAnalysisResult? culturalAnalysis = null;

                // Execute analyses based on options
                if (request.Options.IncludeCultural)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var culturalRequest = new LLMCulturalAnalysisRequest
                        {
                            Name = request.Name,
                            AdditionalContext = request.Surname,
                            IncludeHistoricalContext = true
                        };
                        culturalAnalysis = await provider.AnalyzeCulturalContextAsync(culturalRequest, cancellationToken);
                    }, cancellationToken));
                }

                if (request.Options.IncludePhonetic)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var phoneticRequest = new LLMPhoneticAnalysisRequest
                        {
                            Name = request.Name,
                            SourceLanguage = request.CulturalHints.FirstOrDefault(),
                            TargetLanguages = new[] { "Latin", "Cyrillic", "Arabic" },
                            IncludePhoneticCodes = true
                        };
                        phoneticAnalysis = await provider.GeneratePhoneticVariationsAsync(phoneticRequest, cancellationToken);
                    }, cancellationToken));
                }

                // Always perform name analysis with smart caching
                var nameRequest = new LLMNameAnalysisRequest
                {
                    Name = request.Name,
                    CulturalHint = request.CulturalHints.FirstOrDefault(),
                    MaxAliases = request.Options.MaxAliases,
                    IncludePhonetic = request.Options.IncludePhonetic,
                    IncludeNicknames = request.Options.IncludeNicknames,
                    IncludeTransliterations = request.Options.IncludeTransliterations
                };

                // Try to get from smart cache first
                var smartCacheKey = _smartCache.GenerateCacheKey(nameRequest, provider.ProviderType);
                nameAnalysis = await _smartCache.GetCachedResponseAsync(smartCacheKey, cancellationToken);
                
                if (nameAnalysis == null)
                {
                    // Not in cache, call the LLM provider
                    nameAnalysis = await provider.AnalyzeNameAsync(nameRequest, cancellationToken);
                    
                    // Cache the response
                    if (nameAnalysis != null)
                    {
                        var cacheTtl = TimeSpan.FromMinutes(_config.GlobalSettings.CacheTtlMinutes);
                        await _smartCache.SetCachedResponseAsync(smartCacheKey, nameAnalysis, cacheTtl, cancellationToken);
                    }
                }

                // Wait for all other analyses to complete
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                }

                stopwatch.Stop();

                // Combine results
                var combinedAliases = CombineAliases(nameAnalysis, phoneticAnalysis, culturalAnalysis, request.Options);
                var summary = CreateAnalysisSummary(nameAnalysis, phoneticAnalysis, culturalAnalysis, combinedAliases);
                
                var result = new ComprehensiveNameAnalysisResult
                {
                    Request = request,
                    NameAnalysis = nameAnalysis,
                    PhoneticAnalysis = phoneticAnalysis,
                    CulturalAnalysis = culturalAnalysis,
                    CombinedAliases = combinedAliases,
                    Summary = summary,
                    Metadata = new AnalysisResultMetadata
                    {
                        Provider = provider.ProviderType,
                        ProcessingTime = stopwatch.Elapsed,
                        TotalTokensUsed = CalculateTotalTokens(nameAnalysis, phoneticAnalysis, culturalAnalysis),
                        EstimatedCost = CalculateTotalCost(nameAnalysis, phoneticAnalysis, culturalAnalysis),
                        CacheHitRate = 0.0 // First analysis, no cache hit
                    }
                };

                // Cache the result
                if (_config.GlobalSettings.EnableCaching)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.GlobalSettings.CacheTtlMinutes),
                        Priority = CacheItemPriority.Normal
                    };
                    _cache.Set(cacheKey, result, cacheOptions);
                    _logger.LogDebug("Cached name analysis result for '{Name}'", request.Name);
                }

                _logger.LogInformation("Completed comprehensive name analysis for '{Name}' in {ElapsedMs}ms, generated {AliasCount} total aliases",
                    request.Name, stopwatch.ElapsedMilliseconds, combinedAliases.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to analyze name '{Name}' using provider '{Provider}' after {ElapsedMs}ms",
                    request.Name, provider.ProviderType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<BatchNameAnalysisResult> AnalyzeNamesAsync(BatchNameAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var provider = GetCurrentProvider();
            var results = new List<ComprehensiveNameAnalysisResult>();
            var failures = new List<FailedNameAnalysis>();
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Starting batch analysis of {Count} names using provider '{Provider}' with max concurrency {MaxConcurrency}",
                request.Names.Count, provider.ProviderType, request.Options.MaxConcurrency);

            var semaphore = new SemaphoreSlim(request.Options.MaxConcurrency, request.Options.MaxConcurrency);
            var processedCount = 0;
            var failedCount = 0;

            var tasks = request.Names.Select(async nameRequest =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Report progress
                    request.ProgressCallback?.Report(new BatchProcessingProgress
                    {
                        TotalItems = request.Names.Count,
                        ProcessedItems = processedCount,
                        FailedItems = failedCount,
                        CurrentItem = nameRequest.Name,
                        EstimatedTimeRemaining = EstimateRemainingTime(stopwatch.Elapsed, processedCount, request.Names.Count)
                    });

                    var result = await AnalyzeNameAsync(nameRequest, cancellationToken);
                    lock (results)
                    {
                        results.Add(result);
                    }
                    
                    Interlocked.Increment(ref processedCount);
                }
                catch (Exception ex)
                {
                    if (!request.Options.ContinueOnError)
                        throw;

                    var failure = new FailedNameAnalysis
                    {
                        Request = nameRequest,
                        ErrorMessage = ex.Message,
                        ExceptionDetails = ex.ToString(),
                        FailedAt = DateTime.UtcNow
                    };

                    lock (failures)
                    {
                        failures.Add(failure);
                    }
                    
                    Interlocked.Increment(ref failedCount);
                    _logger.LogWarning(ex, "Failed to analyze name '{Name}' in batch processing", nameRequest.Name);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Final progress report
            request.ProgressCallback?.Report(new BatchProcessingProgress
            {
                TotalItems = request.Names.Count,
                ProcessedItems = processedCount,
                FailedItems = failedCount,
                CurrentItem = null,
                EstimatedTimeRemaining = TimeSpan.Zero
            });

            var batchResult = new BatchNameAnalysisResult
            {
                Request = request,
                Results = results.AsReadOnly(),
                Failures = failures.AsReadOnly(),
                Metadata = new BatchProcessingMetadata
                {
                    TotalProcessingTime = stopwatch.Elapsed,
                    AverageProcessingTime = results.Any() ? 
                        TimeSpan.FromMilliseconds(stopwatch.Elapsed.TotalMilliseconds / results.Count) : 
                        TimeSpan.Zero,
                    SuccessRate = request.Names.Count > 0 ? 
                        (double)results.Count / request.Names.Count : 
                        0.0,
                    TotalCost = results.Sum(r => r.Metadata.EstimatedCost),
                    Provider = provider.ProviderType
                }
            };

            _logger.LogInformation("Completed batch analysis: {Successful}/{Total} successful, {Failed} failed, in {ElapsedMs}ms",
                results.Count, request.Names.Count, failures.Count, stopwatch.ElapsedMilliseconds);

            return batchResult;
        }

        /// <inheritdoc />
        public async Task<NameSimilarityAnalysisResult> AnalyzeSimilarityAsync(NameSimilarityAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var provider = GetCurrentProvider();
            
            // For simplicity, we'll use the cultural analysis to determine similarity
            // In a more sophisticated implementation, you could create a specialized similarity analysis
            var culturalRequest = new LLMCulturalAnalysisRequest
            {
                Name = request.PrimaryName,
                AdditionalContext = $"Compare with: {string.Join(", ", request.ComparisonNames)}",
                IncludeHistoricalContext = false
            };

            try
            {
                var culturalResult = await provider.AnalyzeCulturalContextAsync(culturalRequest, cancellationToken);
                
                // Generate similarity scores (simplified implementation)
                var similarities = request.ComparisonNames.Select(name => new NameSimilarityScore
                {
                    Name = name,
                    Score = CalculateBasicSimilarity(request.PrimaryName, name),
                    SimilarityType = "lexical",
                    Explanation = $"Basic similarity calculation between '{request.PrimaryName}' and '{name}'"
                }).ToList();

                // Filter by threshold
                similarities = similarities.Where(s => s.Score >= request.SimilarityThreshold).ToList();

                var suggestions = similarities
                    .OrderByDescending(s => s.Score)
                    .Take(5)
                    .Select(s => s.Name)
                    .ToList();

                return new NameSimilarityAnalysisResult
                {
                    Request = request,
                    Similarities = similarities,
                    Suggestions = suggestions,
                    Metadata = culturalResult.Metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze similarity for name '{Name}'", request.PrimaryName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LLMServiceHealthResult> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var providerHealthTasks = _providers.Select(async kvp =>
            {
                try
                {
                    var health = await kvp.Value.CheckHealthAsync(cancellationToken);
                    return new KeyValuePair<string, LLMProviderHealthResult>(kvp.Key, health);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for provider {Provider}", kvp.Key);
                    return new KeyValuePair<string, LLMProviderHealthResult>(kvp.Key, new LLMProviderHealthResult
                    {
                        IsHealthy = false,
                        ResponseTimeMs = 0,
                        Message = $"Health check exception: {ex.Message}",
                        LastSuccessfulCheck = null,
                        AvailableModels = null
                    });
                }
            });

            var providerHealthResults = await Task.WhenAll(providerHealthTasks);
            var providerHealth = providerHealthResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var isOverallHealthy = providerHealth.Values.Any(h => h.IsHealthy);
            var activeProvider = _currentProvider?.ProviderType ?? "None";

            return new LLMServiceHealthResult
            {
                IsHealthy = isOverallHealthy,
                ActiveProvider = activeProvider,
                ProviderHealth = providerHealth,
                Statistics = new LLMServiceStatistics
                {
                    TotalRequests = 0, // Would be tracked in a real implementation
                    TotalFailures = 0,
                    AverageResponseTimeMs = providerHealth.Values.Where(h => h.IsHealthy).DefaultIfEmpty().Average(h => h?.ResponseTimeMs ?? 0),
                    CacheHitRate = 0.0, // Would be calculated from cache statistics
                    TotalCost = null
                }
            };
        }

        /// <summary>
        /// Gets the current provider, throwing if none is available
        /// </summary>
        private ILLMProvider GetCurrentProvider()
        {
            lock (_currentProviderLock)
            {
                if (_currentProvider == null)
                    throw new InvalidOperationException("No LLM provider is currently available");
                
                return _currentProvider;
            }
        }

        /// <summary>
        /// Initializes the default provider based on configuration
        /// </summary>
        private void InitializeDefaultProvider()
        {
            var defaultProviderType = _config.DefaultProvider;
            
            if (_providers.TryGetValue(defaultProviderType, out var provider) && provider.IsAvailable)
            {
                lock (_currentProviderLock)
                {
                    _currentProvider = provider;
                }
                _logger.LogInformation("Initialized default LLM provider: {Provider}", defaultProviderType);
            }
            else
            {
                // Fall back to first available provider
                var availableProvider = _providers.Values.FirstOrDefault(p => p.IsAvailable);
                if (availableProvider != null)
                {
                    lock (_currentProviderLock)
                    {
                        _currentProvider = availableProvider;
                    }
                    _logger.LogInformation("Default provider '{DefaultProvider}' not available, using fallback: {Provider}", 
                        defaultProviderType, availableProvider.ProviderType);
                }
                else
                {
                    _logger.LogWarning("No LLM providers are currently available");
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Creates a cache key for the given parameters
        /// </summary>
        private string CreateCacheKey(string operation, string name, IReadOnlyList<string> culturalHints, NameAnalysisOptions options)
        {
            var hints = string.Join(",", culturalHints.OrderBy(h => h));
            var optionsHash = $"{options.IncludePhonetic}-{options.IncludeCultural}-{options.IncludeNicknames}-{options.IncludeTransliterations}-{options.MaxAliases}-{options.MinConfidenceThreshold}";
            return $"llm:{operation}:{name.ToLowerInvariant()}:{hints}:{optionsHash}";
        }

        /// <summary>
        /// Combines aliases from different analyses
        /// </summary>
        private List<LLMNameAlias> CombineAliases(
            LLMNameAnalysisResult? nameAnalysis,
            LLMPhoneticAnalysisResult? phoneticAnalysis, 
            LLMCulturalAnalysisResult? culturalAnalysis,
            NameAnalysisOptions options)
        {
            var combined = new List<LLMNameAlias>();

            if (nameAnalysis?.Aliases != null)
                combined.AddRange(nameAnalysis.Aliases);

            if (phoneticAnalysis?.Variations != null)
            {
                combined.AddRange(phoneticAnalysis.Variations.Select(v => new LLMNameAlias
                {
                    Alias = v.Variation,
                    Confidence = v.Confidence,
                    AliasType = "phonetic",
                    CulturalContext = null,
                    Source = $"Phonetic variation ({v.VariationType})"
                }));
            }

            if (phoneticAnalysis?.Transliterations != null)
            {
                combined.AddRange(phoneticAnalysis.Transliterations.Select(t => new LLMNameAlias
                {
                    Alias = t.Text,
                    Confidence = t.Confidence,
                    AliasType = "transliteration",
                    CulturalContext = t.TargetLanguage,
                    Source = $"Transliteration to {t.TargetLanguage}"
                }));
            }

            // Remove duplicates and filter by confidence
            return combined
                .GroupBy(a => a.Alias.ToLowerInvariant())
                .Select(g => g.OrderByDescending(a => a.Confidence).First())
                .Where(a => a.Confidence >= options.MinConfidenceThreshold)
                .OrderByDescending(a => a.Confidence)
                .Take(options.MaxAliases)
                .ToList();
        }

        /// <summary>
        /// Creates an analysis summary
        /// </summary>
        private AnalysisSummary CreateAnalysisSummary(
            LLMNameAnalysisResult? nameAnalysis,
            LLMPhoneticAnalysisResult? phoneticAnalysis,
            LLMCulturalAnalysisResult? culturalAnalysis,
            List<LLMNameAlias> combinedAliases)
        {
            var primaryCulture = culturalAnalysis?.CulturalOrigins
                .OrderByDescending(c => c.Confidence)
                .FirstOrDefault();

            var aliasTypeGroups = combinedAliases
                .GroupBy(a => a.AliasType)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var highConfidenceCount = combinedAliases.Count(a => a.Confidence >= 0.8);

            var recommendations = new List<string>();
            
            if (highConfidenceCount == 0)
                recommendations.Add("Consider providing additional cultural context for better alias generation");
            
            if (combinedAliases.Count < 3)
                recommendations.Add("Increase max aliases setting to get more variations");
            
            if (primaryCulture == null)
                recommendations.Add("Cultural analysis could not determine origin - consider providing cultural hints");

            return new AnalysisSummary
            {
                PrimaryCulture = primaryCulture?.Culture,
                CulturalConfidence = primaryCulture?.Confidence ?? 0.0,
                MostCommonAliasType = aliasTypeGroups?.Key,
                HighConfidenceAliasCount = highConfidenceCount,
                Recommendations = recommendations
            };
        }

        /// <summary>
        /// Calculates total tokens used across all analyses
        /// </summary>
        private int? CalculateTotalTokens(LLMNameAnalysisResult? nameAnalysis, LLMPhoneticAnalysisResult? phoneticAnalysis, LLMCulturalAnalysisResult? culturalAnalysis)
        {
            var tokens = 0;
            
            if (nameAnalysis?.Metadata.TokensUsed.HasValue == true)
                tokens += nameAnalysis.Metadata.TokensUsed.Value;
                
            if (phoneticAnalysis?.Metadata.TokensUsed.HasValue == true)
                tokens += phoneticAnalysis.Metadata.TokensUsed.Value;
                
            if (culturalAnalysis?.Metadata.TokensUsed.HasValue == true)
                tokens += culturalAnalysis.Metadata.TokensUsed.Value;
            
            return tokens > 0 ? tokens : null;
        }

        /// <summary>
        /// Calculates total cost across all analyses
        /// </summary>
        private decimal? CalculateTotalCost(LLMNameAnalysisResult? nameAnalysis, LLMPhoneticAnalysisResult? phoneticAnalysis, LLMCulturalAnalysisResult? culturalAnalysis)
        {
            var costs = 0m;
            
            if (nameAnalysis?.Metadata.Cost.HasValue == true)
                costs += nameAnalysis.Metadata.Cost.Value;
                
            if (phoneticAnalysis?.Metadata.Cost.HasValue == true)
                costs += phoneticAnalysis.Metadata.Cost.Value;
                
            if (culturalAnalysis?.Metadata.Cost.HasValue == true)
                costs += culturalAnalysis.Metadata.Cost.Value;
            
            return costs > 0 ? costs : null;
        }

        /// <summary>
        /// Estimates remaining processing time
        /// </summary>
        private TimeSpan? EstimateRemainingTime(TimeSpan elapsed, int processed, int total)
        {
            if (processed == 0 || processed >= total)
                return null;

            var avgTimePerItem = elapsed.TotalMilliseconds / processed;
            var remainingItems = total - processed;
            return TimeSpan.FromMilliseconds(avgTimePerItem * remainingItems);
        }

        /// <summary>
        /// Calculates basic string similarity
        /// </summary>
        private double CalculateBasicSimilarity(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                return 0.0;

            // Simple Levenshtein distance-based similarity
            var distance = CalculateLevenshteinDistance(name1.ToLowerInvariant(), name2.ToLowerInvariant());
            var maxLength = Math.Max(name1.Length, name2.Length);
            return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// Calculates Levenshtein distance between two strings
        /// </summary>
        private int CalculateLevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;
            
            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        #endregion

        public void Dispose()
        {
            _providerSwitchLock?.Dispose();
            
            foreach (var provider in _providers.Values)
            {
                if (provider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
            }
        }
    }
}
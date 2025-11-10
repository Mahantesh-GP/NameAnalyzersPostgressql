using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PhoneticAnalyzers.Application.Services.LLM;

/// <summary>
/// Multi-tier LLM caching service with in-memory, Redis, and database layers
/// </summary>
public class SmartLLMCacheService : ILLMCacheService
{
    private readonly ILogger<SmartLLMCacheService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly INameAliasCacheRepository _persistentCache;
    private readonly IOptions<LLMConfiguration> _config;
    
    // Cache statistics tracking
    private long _memoryCacheHits = 0;
    private long _distributedCacheHits = 0;
    private long _persistentCacheHits = 0;
    private long _totalRequests = 0;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the SmartLLMCacheService
    /// </summary>
    public SmartLLMCacheService(
        ILogger<SmartLLMCacheService> logger,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        INameAliasCacheRepository persistentCache,
        IOptions<LLMConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _persistentCache = persistentCache ?? throw new ArgumentNullException(nameof(persistentCache));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public async Task<LLMNameAnalysisResult?> GetCachedResponseAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalRequests);

        if (!_config.Value.GlobalSettings.EnableCaching)
        {
            return null;
        }

        try
        {
            // Tier 1: Check memory cache (fastest)
            if (_memoryCache.TryGetValue(cacheKey, out LLMNameAnalysisResult? cachedResult) && cachedResult != null)
            {
                Interlocked.Increment(ref _memoryCacheHits);
                _logger.LogDebug("Cache hit in memory cache for key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            // Tier 2: Check distributed cache (Redis)
            try
            {
                var distributedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    var deserializedResult = JsonSerializer.Deserialize<LLMNameAnalysisResult>(distributedValue, _jsonOptions);
                    if (deserializedResult != null)
                    {
                        Interlocked.Increment(ref _distributedCacheHits);
                        _logger.LogDebug("Cache hit in distributed cache for key: {CacheKey}", cacheKey);

                        // Promote to memory cache for faster future access
                        var memoryTtl = TimeSpan.FromMinutes(Math.Min(_config.Value.GlobalSettings.CacheTtlMinutes / 2, 30));
                        _memoryCache.Set(cacheKey, deserializedResult, memoryTtl);

                        return deserializedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve from distributed cache for key: {CacheKey}", cacheKey);
            }

            // Tier 3: Check persistent database cache (slowest but most durable)
            try
            {
                var hash = GenerateHash(cacheKey);
                var inputQuery = NormalizedName.Create(hash);
                var locale = Locale.Create("en"); // Default locale for LLM cache

                var persistentEntry = await _persistentCache.GetAsync(inputQuery, locale, cancellationToken);
                if (persistentEntry != null && !persistentEntry.IsExpired())
                {
                    var persistentValue = persistentEntry.CachedAliases;
                    var deserializedResult = JsonSerializer.Deserialize<LLMNameAnalysisResult>(persistentValue, _jsonOptions);
                    
                    if (deserializedResult != null)
                    {
                        Interlocked.Increment(ref _persistentCacheHits);
                        _logger.LogDebug("Cache hit in persistent cache for key: {CacheKey}", cacheKey);

                        // Record cache hit
                        persistentEntry.RecordHit();
                        await _persistentCache.UpdateAsync(persistentEntry, cancellationToken);

                        // Promote to higher-tier caches
                        var distributedTtl = TimeSpan.FromMinutes(_config.Value.GlobalSettings.CacheTtlMinutes);
                        var memoryTtl = TimeSpan.FromMinutes(Math.Min(_config.Value.GlobalSettings.CacheTtlMinutes / 2, 30));

                        // Promote to distributed cache
                        try
                        {
                            var jsonValue = JsonSerializer.Serialize(deserializedResult, _jsonOptions);
                            var distributedOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = distributedTtl
                            };
                            await _distributedCache.SetStringAsync(cacheKey, jsonValue, distributedOptions, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to promote to distributed cache for key: {CacheKey}", cacheKey);
                        }

                        // Promote to memory cache
                        _memoryCache.Set(cacheKey, deserializedResult, memoryTtl);

                        return deserializedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve from persistent cache for key: {CacheKey}", cacheKey);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached response for key: {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetCachedResponseAsync(string cacheKey, LLMNameAnalysisResult response, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_config.Value.GlobalSettings.EnableCaching || response == null)
        {
            return;
        }

        try
        {
            var jsonValue = JsonSerializer.Serialize(response, _jsonOptions);

            // Set in all cache tiers

            // Tier 1: Memory cache (shortest TTL for memory efficiency)
            var memoryTtl = TimeSpan.FromMinutes(Math.Min(ttl.TotalMinutes / 2, 30));
            _memoryCache.Set(cacheKey, response, memoryTtl);

            // Tier 2: Distributed cache (Redis) - medium TTL
            try
            {
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                };
                await _distributedCache.SetStringAsync(cacheKey, jsonValue, distributedOptions, cancellationToken);
                _logger.LogDebug("Cached response in distributed cache for key: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache in distributed cache for key: {CacheKey}", cacheKey);
            }

            // Tier 3: Persistent database cache (longest TTL)
            try
            {
                var hash = GenerateHash(cacheKey);
                var inputQuery = NormalizedName.Create(hash);
                var locale = Locale.Create("en"); // Default locale for LLM cache
                
                // Check if entry exists
                var existingEntry = await _persistentCache.GetAsync(inputQuery, locale, cancellationToken);
                if (existingEntry != null)
                {
                    // Update existing entry
                    existingEntry.RecordHit();
                    existingEntry.ExtendExpiration((int)ttl.TotalHours);
                    await _persistentCache.UpdateAsync(existingEntry, cancellationToken);
                }
                else
                {
                    // Create new entry
                    var persistentEntry = NameAliasCache.Create(inputQuery, locale, jsonValue, (int)ttl.TotalHours);
                    await _persistentCache.AddAsync(persistentEntry, cancellationToken);
                }

                _logger.LogDebug("Cached response in persistent cache for key: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache in persistent cache for key: {CacheKey}", cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached response for key: {CacheKey}", cacheKey);
        }
    }

    /// <inheritdoc />
    public async Task<LLMCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get persistent cache statistics
            var persistentStats = await _persistentCache.GetStatisticsAsync(cancellationToken);

            var totalRequests = Interlocked.Read(ref _totalRequests);
            var totalHits = Interlocked.Read(ref _memoryCacheHits) + 
                           Interlocked.Read(ref _distributedCacheHits) + 
                           Interlocked.Read(ref _persistentCacheHits);

            return new LLMCacheStatistics
            {
                TotalEntries = (int)persistentStats.TotalEntries, 
                MemoryCacheEntries = 0, // Memory cache doesn't expose count easily
                PersistentCacheEntries = (int)persistentStats.TotalEntries,
                TotalHits = totalHits,
                MemoryCacheHits = Interlocked.Read(ref _memoryCacheHits),
                PersistentCacheHits = Interlocked.Read(ref _persistentCacheHits),
                HitRatio = totalRequests > 0 ? (double)totalHits / totalRequests : 0.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(1), // Cached responses are fast
                EstimatedSizeBytes = persistentStats.ApproximateSizeBytes,
                ExpiredEntries = (int)persistentStats.ExpiredEntries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return new LLMCacheStatistics();
        }
    }

    /// <inheritdoc />
    public async Task<int> InvalidateCacheAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var invalidated = 0;

        try
        {
            // For memory cache, we can't easily pattern match, so we skip it
            // Memory cache entries will naturally expire

            // For distributed cache (Redis), pattern invalidation depends on Redis implementation
            // This is a simplified approach - in production you might use Redis KEYS command (carefully)

            // For persistent cache, we can clear all entries (simplified approach)
            if (pattern == "*" || string.IsNullOrEmpty(pattern))
            {
                invalidated = await _persistentCache.ClearAllAsync(cancellationToken);
                _logger.LogInformation("Invalidated {Count} cache entries", invalidated);
            }

            return invalidated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache with pattern: {Pattern}", pattern);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<int> ClearExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cleared = await _persistentCache.DeleteExpiredAsync(cancellationToken);
            _logger.LogInformation("Cleared {Count} expired cache entries", cleared);
            return cleared;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing expired cache entries");
            return 0;
        }
    }

    /// <inheritdoc />
    public string GenerateCacheKey(LLMNameAnalysisRequest request, string provider)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(provider)) throw new ArgumentException("Provider cannot be null or empty", nameof(provider));

        // Create a deterministic cache key based on request parameters
        var keyComponents = new StringBuilder();
        keyComponents.Append($"llm:{provider}:");
        keyComponents.Append($"name:{request.Name}:");
        
        if (!string.IsNullOrEmpty(request.CulturalHint))
            keyComponents.Append($"culture:{request.CulturalHint}:");

        keyComponents.Append($"maxAliases:{request.MaxAliases}:");
        keyComponents.Append($"includePhonetic:{request.IncludePhonetic}:");
        keyComponents.Append($"includeNicknames:{request.IncludeNicknames}:");
        keyComponents.Append($"includeTransliterations:{request.IncludeTransliterations}:");

        var keyString = keyComponents.ToString();
        
        // Hash the key to ensure consistent length and avoid special characters
        return $"llm_cache:{GenerateHash(keyString)}";
    }

    /// <summary>
    /// Generates a SHA256 hash of the input string
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>The hash as a hexadecimal string</returns>
    private string GenerateHash(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
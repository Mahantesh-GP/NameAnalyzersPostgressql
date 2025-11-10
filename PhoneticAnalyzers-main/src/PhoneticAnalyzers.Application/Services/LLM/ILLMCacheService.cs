using PhoneticAnalyzers.Application.Services.LLM;

namespace PhoneticAnalyzers.Application.Services.LLM;

/// <summary>
/// Interface for LLM response caching service with multi-tier caching strategy
/// </summary>
public interface ILLMCacheService
{
    /// <summary>
    /// Gets a cached LLM response
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The cached response if found, null otherwise</returns>
    Task<LLMNameAnalysisResult?> GetCachedResponseAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached LLM response
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="response">The LLM response to cache</param>
    /// <param name="ttl">The time-to-live for the cache entry</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task SetCachedResponseAsync(string cacheKey, LLMNameAnalysisResult response, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Cache statistics</returns>
    Task<LLMCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries by pattern
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Number of entries invalidated</returns>
    Task<int> InvalidateCacheAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears expired cache entries
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Number of entries cleared</returns>
    Task<int> ClearExpiredEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a cache key for an LLM request
    /// </summary>
    /// <param name="request">The LLM request</param>
    /// <param name="provider">The provider name</param>
    /// <returns>The cache key</returns>
    string GenerateCacheKey(LLMNameAnalysisRequest request, string provider);
}

/// <summary>
/// LLM cache statistics
/// </summary>
public class LLMCacheStatistics
{
    /// <summary>
    /// Total cache entries across all tiers
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Memory cache entries
    /// </summary>
    public int MemoryCacheEntries { get; set; }

    /// <summary>
    /// Persistent cache entries
    /// </summary>
    public int PersistentCacheEntries { get; set; }

    /// <summary>
    /// Total cache hits
    /// </summary>
    public long TotalHits { get; set; }

    /// <summary>
    /// Memory cache hits
    /// </summary>
    public long MemoryCacheHits { get; set; }

    /// <summary>
    /// Persistent cache hits
    /// </summary>
    public long PersistentCacheHits { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double HitRatio { get; set; }

    /// <summary>
    /// Average response time from cache
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// Estimated cache size in bytes
    /// </summary>
    public long EstimatedSizeBytes { get; set; }

    /// <summary>
    /// Number of expired entries
    /// </summary>
    public int ExpiredEntries { get; set; }
}
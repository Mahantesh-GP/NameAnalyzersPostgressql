using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Repositories;

/// <summary>
/// Repository interface for NameAliasCache entities
/// </summary>
public interface INameAliasCacheRepository
{
    /// <summary>
    /// Adds a new cache entry to the repository
    /// </summary>
    /// <param name="cacheEntry">The cache entry to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The added cache entry</returns>
    Task<NameAliasCache> AddAsync(NameAliasCache cacheEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cache entry by query and locale
    /// </summary>
    /// <param name="inputQuery">The normalized input query</param>
    /// <param name="locale">The locale</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The cache entry if found and not expired, null otherwise</returns>
    Task<NameAliasCache?> GetAsync(
        NormalizedName inputQuery,
        Locale locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing cache entry (records hit)
    /// </summary>
    /// <param name="cacheEntry">The cache entry to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated cache entry</returns>
    Task<NameAliasCache> UpdateAsync(NameAliasCache cacheEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired cache entries
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of entries deleted</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Cache statistics including total entries, hit rate, etc.</returns>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of entries cleared</returns>
    Task<int> ClearAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents cache statistics
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets the total number of cache entries
    /// </summary>
    public long TotalEntries { get; init; }

    /// <summary>
    /// Gets the number of expired entries
    /// </summary>
    public long ExpiredEntries { get; init; }

    /// <summary>
    /// Gets the total number of cache hits
    /// </summary>
    public long TotalHits { get; init; }

    /// <summary>
    /// Gets the average hit count per entry
    /// </summary>
    public decimal AverageHitsPerEntry { get; init; }

    /// <summary>
    /// Gets the oldest entry age in hours
    /// </summary>
    public double OldestEntryAgeHours { get; init; }

    /// <summary>
    /// Gets the cache size in bytes (approximate)
    /// </summary>
    public long ApproximateSizeBytes { get; init; }
}
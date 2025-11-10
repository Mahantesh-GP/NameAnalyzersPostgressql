using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;
using System.Text.Json;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for NameAliasCache entities
/// </summary>
public sealed class NameAliasCacheRepository : INameAliasCacheRepository
{
    private readonly PhoneticAnalyzersDbContext _context;

    /// <summary>


    /// Initializes a new instance of the NameAliasCacheRepository class


    /// </summary>


    /// <param name="context">The database context</param>


    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>


    public NameAliasCacheRepository(PhoneticAnalyzersDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>


    /// Gets a name alias cache entry by its unique identifier


    /// </summary>


    /// <param name="id">The cache entry identifier</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The cache entry if found, null otherwise</returns>


    public async Task<NameAliasCache?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.NameAliasCache
            .FirstOrDefaultAsync(nac => nac.Id == id, cancellationToken);
    }

    /// <summary>


    /// Gets a cache entry by normalized name and locale


    /// </summary>


    /// <param name="normalizedName">The normalized name to search for</param>


    /// <param name="locale">The locale to search for</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The cache entry if found, null otherwise</returns>


    public async Task<NameAliasCache?> GetAsync(NormalizedName inputQuery, Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(locale);

        return await _context.NameAliasCache
            .FirstOrDefaultAsync(nac => 
                nac.InputQuery.Value == inputQuery.Value && 
                nac.Locale.Code == locale.Code && 
                nac.ExpiresUtc > DateTime.UtcNow, 
                cancellationToken);
    }

    /// <summary>


    /// Gets cached aliases for a specific name and locale


    /// </summary>


    /// <param name="name">The name to get cached aliases for</param>


    /// <param name="locale">The locale to filter by</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The cached aliases if found, null otherwise</returns>


    public async Task<NameAliasCache?> GetCachedAliasesAsync(string inputQuery, Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputQuery);
        ArgumentNullException.ThrowIfNull(locale);

        var normalizedQuery = NormalizedName.Create(inputQuery);
        return await GetAsync(normalizedQuery, locale, cancellationToken);
    }

    /// <summary>


    /// Gets a cache entry by query hash and locale


    /// </summary>


    /// <param name="queryHash">The query hash to search for</param>


    /// <param name="locale">The locale to search for</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The cache entry if found, null otherwise</returns>


    public async Task<NameAliasCache?> GetByQueryHashAsync(string queryHash, Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryHash);
        ArgumentNullException.ThrowIfNull(locale);

        return await _context.NameAliasCache
            .FirstOrDefaultAsync(nac => 
                nac.QueryHash == queryHash && 
                nac.Locale.Code == locale.Code && 
                nac.ExpiresUtc > DateTime.UtcNow, 
                cancellationToken);
    }

    /// <summary>


    /// Gets all active (non-expired) cache entries, optionally filtered by locale


    /// </summary>


    /// <param name="locale">Optional locale filter</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of active cache entries</returns>


    public async Task<IReadOnlyList<NameAliasCache>> GetActiveEntriesAsync(Locale? locale = null, CancellationToken cancellationToken = default)
    {
        var query = _context.NameAliasCache.Where(nac => nac.ExpiresUtc > DateTime.UtcNow);

        if (locale != null)
        {
            query = query.Where(nac => nac.Locale.Code == locale.Code);
        }

        return await query
            .OrderByDescending(nac => nac.LastAccessedUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets all expired cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of expired cache entries</returns>


    public async Task<IReadOnlyList<NameAliasCache>> GetExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NameAliasCache
            .Where(nac => nac.ExpiresUtc <= DateTime.UtcNow)
            .OrderBy(nac => nac.ExpiresUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets cache entries with the highest hit counts


    /// </summary>


    /// <param name="limit">Maximum number of entries to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of most accessed cache entries ordered by hit count</returns>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NameAliasCache>> GetMostAccessedEntriesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        return await _context.NameAliasCache
            .Where(nac => nac.ExpiresUtc > DateTime.UtcNow)
            .OrderByDescending(nac => nac.HitCount)
            .ThenByDescending(nac => nac.LastAccessedUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets recently created cache entries


    /// </summary>


    /// <param name="limit">Maximum number of entries to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of recent cache entries ordered by creation date</returns>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NameAliasCache>> GetRecentEntriesAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _context.NameAliasCache
            .Where(nac => nac.CreatedUtc >= cutoffDate)
            .OrderByDescending(nac => nac.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Adds a new cache entry to the repository


    /// </summary>


    /// <param name="nameAliasCache">The cache entry to add</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The added cache entry with updated identifier</returns>


    /// <exception cref="ArgumentNullException">Thrown when nameAliasCache is null</exception>


    public async Task<NameAliasCache> AddAsync(NameAliasCache nameAliasCache, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAliasCache);

        _context.NameAliasCache.Add(nameAliasCache);
        await _context.SaveChangesAsync(cancellationToken);
        return nameAliasCache;
    }

    /// <summary>


    /// Updates an existing cache entry in the repository


    /// </summary>


    /// <param name="nameAliasCache">The cache entry to update</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The updated cache entry</returns>


    /// <exception cref="ArgumentNullException">Thrown when nameAliasCache is null</exception>


    public async Task<NameAliasCache> UpdateAsync(NameAliasCache nameAliasCache, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAliasCache);

        _context.NameAliasCache.Update(nameAliasCache);
        await _context.SaveChangesAsync(cancellationToken);
        return nameAliasCache;
    }

    /// <summary>


    /// Deletes a cache entry from the repository


    /// </summary>


    /// <param name="nameAliasCache">The cache entry to delete</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nameAliasCache is null</exception>


    public async Task DeleteAsync(NameAliasCache nameAliasCache, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAliasCache);

        _context.NameAliasCache.Remove(nameAliasCache);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>


    /// Deletes all expired cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of deleted entries</returns>


    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredEntries = await GetExpiredEntriesAsync(cancellationToken);
        
        if (expiredEntries.Count > 0)
        {
            _context.NameAliasCache.RemoveRange(expiredEntries);
            await _context.SaveChangesAsync(cancellationToken);
            return expiredEntries.Count;
        }

        return 0;
    }

    /// <summary>


    /// Deletes expired cache entries from the repository


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    public async Task DeleteExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        await DeleteExpiredAsync(cancellationToken);
    }

    /// <summary>


    /// Increments the hit count for a specific cache entry


    /// </summary>


    /// <param name="id">The cache entry identifier</param>


    /// <param name="cancellationToken">Cancellation token</param>


    public async Task IncrementHitCountAsync(long cacheId, CancellationToken cancellationToken = default)
    {
        var cacheEntry = await _context.NameAliasCache
            .FirstOrDefaultAsync(nac => nac.Id == cacheId, cancellationToken);

        if (cacheEntry != null)
        {
            cacheEntry.RecordHit();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>


    /// Checks if a cache entry exists for the specified query hash and locale


    /// </summary>


    /// <param name="queryHash">The query hash to check</param>


    /// <param name="locale">The locale to check</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>True if the cache entry exists, false otherwise</returns>


    public async Task<bool> ExistsByQueryAsync(string inputQuery, Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputQuery);
        ArgumentNullException.ThrowIfNull(locale);

        return await _context.NameAliasCache
            .AnyAsync(nac => 
                nac.InputQuery == inputQuery && 
                nac.Locale.Code == locale.Code && 
                nac.ExpiresUtc > DateTime.UtcNow, 
                cancellationToken);
    }

    /// <summary>


    /// Gets the count of active (non-expired) cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of active cache entries</returns>


    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NameAliasCache
            .CountAsync(nac => nac.ExpiresUtc > DateTime.UtcNow, cancellationToken);
    }

    /// <summary>


    /// Gets the count of expired cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of expired cache entries</returns>


    public async Task<int> GetExpiredCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NameAliasCache
            .CountAsync(nac => nac.ExpiresUtc <= DateTime.UtcNow, cancellationToken);
    }

    /// <summary>


    /// Gets the total number of hits across all cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The total hit count</returns>


    public async Task<long> GetTotalHitsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NameAliasCache
            .Where(nac => nac.ExpiresUtc > DateTime.UtcNow)
            .SumAsync(nac => (long)nac.HitCount, cancellationToken);
    }

    /// <summary>


    /// Gets the average hit count across all cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The average hit count, or null if no entries exist</returns>


    public async Task<double> GetAverageHitCountAsync(CancellationToken cancellationToken = default)
    {
        var activeEntries = await _context.NameAliasCache
            .Where(nac => nac.ExpiresUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        return activeEntries.Count > 0 ? activeEntries.Average(nac => nac.HitCount) : 0.0;
    }

    /// <summary>


    /// Cleans up old cache entries, keeping only the most recent ones


    /// </summary>


    /// <param name="maxEntries">Maximum number of entries to keep</param>


    /// <param name="cancellationToken">Cancellation token</param>


    public async Task CleanupOldEntriesAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysOld);

        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldEntries = await _context.NameAliasCache
            .Where(nac => nac.CreatedUtc < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldEntries.Count > 0)
        {
            _context.NameAliasCache.RemoveRange(oldEntries);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>


    /// Gets cache entries with the lowest hit counts


    /// </summary>


    /// <param name="limit">Maximum number of entries to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of low-hit cache entries ordered by hit count</returns>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NameAliasCache>> GetLowHitEntriesAsync(int maxHitCount = 1, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxHitCount);

        return await _context.NameAliasCache
            .Where(nac => nac.HitCount <= maxHitCount && nac.ExpiresUtc > DateTime.UtcNow)
            .OrderBy(nac => nac.HitCount)
            .ThenBy(nac => nac.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Deletes all expired cache entries


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of deleted entries</returns>


    public async Task<int> DeleteExpiredAsync(int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var totalDeleted = 0;
        int batchDeleted;

        do
        {
            var expiredBatch = await _context.NameAliasCache
                .Where(nac => nac.ExpiresUtc <= DateTime.UtcNow)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            batchDeleted = expiredBatch.Count;
            
            if (batchDeleted > 0)
            {
                _context.NameAliasCache.RemoveRange(expiredBatch);
                await _context.SaveChangesAsync(cancellationToken);
                totalDeleted += batchDeleted;
            }

        } while (batchDeleted == batchSize && !cancellationToken.IsCancellationRequested);
        
        return totalDeleted;
    }

    /// <summary>


    /// Gets comprehensive statistics about the cache


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>Cache statistics including counts, hit rates, and performance metrics</returns>


    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalEntries = await _context.NameAliasCache.LongCountAsync(cancellationToken);
        var expiredEntries = await _context.NameAliasCache
            .LongCountAsync(nac => nac.ExpiresUtc <= DateTime.UtcNow, cancellationToken);

        var totalHits = await _context.NameAliasCache
            .SumAsync(nac => (long)nac.HitCount, cancellationToken);

        var averageHits = totalEntries > 0 
            ? await _context.NameAliasCache.AverageAsync(nac => nac.HitCount, cancellationToken)
            : 0.0;

        var oldestEntry = await _context.NameAliasCache
            .OrderBy(nac => nac.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var oldestAge = oldestEntry != null 
            ? (DateTime.UtcNow - oldestEntry.CreatedUtc).TotalHours
            : 0.0;

        // Approximate size calculation (very rough estimate)
        var approximateSize = totalEntries * 1024; // Assume 1KB per entry

        return new CacheStatistics
        {
            TotalEntries = totalEntries,
            ExpiredEntries = expiredEntries,
            TotalHits = totalHits,
            AverageHitsPerEntry = (decimal)averageHits,
            OldestEntryAgeHours = oldestAge,
            ApproximateSizeBytes = approximateSize
        };
    }

    /// <summary>


    /// Clears all cache entries from the repository


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of deleted entries</returns>


    public async Task<int> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var allEntries = await _context.NameAliasCache.ToListAsync(cancellationToken);
        
        if (allEntries.Count > 0)
        {
            _context.NameAliasCache.RemoveRange(allEntries);
            await _context.SaveChangesAsync(cancellationToken);
            return allEntries.Count;
        }

        return 0;
    }
}

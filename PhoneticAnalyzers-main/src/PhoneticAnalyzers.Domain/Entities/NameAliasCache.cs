using PhoneticAnalyzers.Domain.Common;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Entities;

/// <summary>
/// Represents a cache entry for LLM-generated name aliases with TTL
/// </summary>
public sealed class NameAliasCache : BaseEntity
{
    /// <summary>
    /// Gets the normalized input query
    /// </summary>
    public NormalizedName InputQuery { get; private set; }

    /// <summary>
    /// Gets the locale context for the query
    /// </summary>
    public Locale Locale { get; private set; }

    /// <summary>
    /// Gets the cached aliases (JSON serialized)
    /// </summary>
    public string CachedAliases { get; private set; }



    /// <summary>
    /// Gets the cache expiration timestamp
    /// </summary>
    public DateTime ExpiresUtc { get; private set; }

    /// <summary>
    /// Gets the number of times this cache entry was used
    /// </summary>
    public int HitCount { get; private set; }

    /// <summary>
    /// Gets the last access timestamp
    /// </summary>
    public DateTime LastAccessedUtc { get; private set; }

    /// <summary>
    /// Gets the hash of the input for quick lookups
    /// </summary>
    public string QueryHash { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private NameAliasCache()
    {
        InputQuery = null!;
        Locale = null!;
        CachedAliases = string.Empty;
        QueryHash = string.Empty;
    }

    /// <summary>
    /// Creates a new name alias cache entry
    /// </summary>
    /// <param name="inputQuery">The normalized input query</param>
    /// <param name="locale">The locale context</param>
    /// <param name="cachedAliases">The cached aliases (JSON)</param>
    /// <param name="ttlHours">The time-to-live in hours</param>
    /// <returns>A new NameAliasCache instance</returns>
    public static NameAliasCache Create(
        NormalizedName inputQuery,
        Locale locale,
        string cachedAliases,
        int ttlHours = 24)
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(locale);

        if (string.IsNullOrWhiteSpace(cachedAliases))
            throw new ArgumentException("Cached aliases cannot be null or whitespace.", nameof(cachedAliases));

        if (ttlHours <= 0)
            throw new ArgumentException("TTL must be greater than zero.", nameof(ttlHours));

        var now = DateTime.UtcNow;
        var queryHash = GenerateQueryHash(inputQuery.Value, locale.Code);

        var cacheEntry = new NameAliasCache
        {
            InputQuery = inputQuery,
            Locale = locale,
            CachedAliases = cachedAliases,
            ExpiresUtc = now.AddHours(ttlHours),
            HitCount = 0,
            LastAccessedUtc = now,
            QueryHash = queryHash
        };

        cacheEntry.SetCreatedTimestamp(now);
        return cacheEntry;
    }

    /// <summary>
    /// Records a cache hit
    /// </summary>
    public void RecordHit()
    {
        HitCount++;
        LastAccessedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the cache entry has expired
    /// </summary>
    /// <returns>True if expired</returns>
    public bool IsExpired() => DateTime.UtcNow > ExpiresUtc;

    /// <summary>
    /// Extends the cache expiration time
    /// </summary>
    /// <param name="additionalHours">Additional hours to extend</param>
    public void ExtendExpiration(int additionalHours)
    {
        if (additionalHours <= 0)
            throw new ArgumentException("Additional hours must be greater than zero.", nameof(additionalHours));

        ExpiresUtc = ExpiresUtc.AddHours(additionalHours);
    }

    /// <summary>
    /// Generates a hash for quick query lookups
    /// </summary>
    /// <param name="query">The normalized query</param>
    /// <param name="locale">The locale code</param>
    /// <returns>A hash string</returns>
    private static string GenerateQueryHash(string query, string locale)
    {
        var input = $"{query}|{locale}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16]; // First 16 characters for performance
    }
}
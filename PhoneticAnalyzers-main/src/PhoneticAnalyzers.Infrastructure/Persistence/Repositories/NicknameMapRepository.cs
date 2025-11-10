using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for NicknameMap entities
/// </summary>
public sealed class NicknameMapRepository : INicknameMapRepository
{
    private readonly PhoneticAnalyzersDbContext _context;

    /// <summary>


    /// Initializes a new instance of the NicknameMapRepository class


    /// </summary>


    /// <param name="context">The database context</param>


    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>


    public NicknameMapRepository(PhoneticAnalyzersDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>


    /// Gets a nickname map by its unique identifier


    /// </summary>


    /// <param name="id">The nickname map identifier</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The nickname map if found, null otherwise</returns>


    public async Task<NicknameMap?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.NicknameMaps
            .FirstOrDefaultAsync(nm => nm.Id == id, cancellationToken);
    }

    /// <summary>


    /// Gets all nicknames for a given canonical name with optional locale filtering


    /// </summary>


    /// <param name="canonicalName">The canonical name to find nicknames for</param>


    /// <param name="locale">Optional locale filter</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of nicknames for the specified canonical name</returns>


    /// <exception cref="ArgumentException">Thrown when canonicalName is null or whitespace</exception>


    public async Task<IReadOnlyList<NicknameMap>> GetNicknamesAsync(string canonicalName, Locale? locale = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);

        var normalizedName = canonicalName.ToUpperInvariant().Trim();
        var query = _context.NicknameMaps.Where(nm => nm.NormalizedCanonicalName == normalizedName);

        if (locale != null)
        {
            query = query.Where(nm => nm.Locale.Code == locale.Code);
        }

        return await query
            .OrderByDescending(nm => nm.Confidence)
            .ThenBy(nm => nm.Nickname)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets all canonical names for a given nickname with optional locale filtering


    /// </summary>


    /// <param name="nickname">The nickname to find canonical names for</param>


    /// <param name="locale">Optional locale filter</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of canonical names for the specified nickname</returns>


    /// <exception cref="ArgumentException">Thrown when nickname is null or whitespace</exception>


    public async Task<IReadOnlyList<NicknameMap>> GetCanonicalNamesAsync(string nickname, Locale? locale = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        var normalizedNickname = nickname.ToUpperInvariant().Trim();
        var query = _context.NicknameMaps.Where(nm => nm.NormalizedNickname == normalizedNickname);

        if (locale != null)
        {
            query = query.Where(nm => nm.Locale.Code == locale.Code);
        }

        return await query
            .OrderByDescending(nm => nm.Confidence)
            .ThenBy(nm => nm.CanonicalName)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets all name variants (both nicknames and canonical names) for a given name


    /// </summary>


    /// <param name="name">The name to find variants for</param>


    /// <param name="locale">Optional locale filter</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of all name variants</returns>


    /// <exception cref="ArgumentException">Thrown when name is null or whitespace</exception>


    public async Task<IReadOnlyList<NicknameMap>> GetAllVariantsAsync(string name, Locale? locale = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.ToUpperInvariant().Trim();
        var query = _context.NicknameMaps.Where(nm => 
            nm.IsBidirectional && 
            (nm.NormalizedCanonicalName == normalizedName || nm.NormalizedNickname == normalizedName));

        if (locale != null)
        {
            query = query.Where(nm => nm.Locale.Code == locale.Code);
        }

        return await query
            .OrderByDescending(nm => nm.Confidence)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets nickname mappings filtered by locale with pagination


    /// </summary>


    /// <param name="locale">The locale to filter by</param>


    /// <param name="limit">Maximum number of results to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A paginated list of nickname mappings for the specified locale</returns>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NicknameMap>> GetByLocaleAsync(Locale locale, int limit = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        return await _context.NicknameMaps
            .Where(nm => nm.Locale.Code == locale.Code)
            .OrderBy(nm => nm.CanonicalName)
            .ThenBy(nm => nm.Nickname)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets nickname mappings with confidence scores above the specified threshold


    /// </summary>


    /// <param name="minConfidence">Minimum confidence score threshold</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of high-confidence nickname mappings</returns>


    public async Task<IReadOnlyList<NicknameMap>> GetHighConfidenceMappingsAsync(decimal minimumConfidence = 0.8m, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumConfidence, 0.0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumConfidence, 1.0m);

        return await _context.NicknameMaps
            .Where(nm => nm.Confidence >= minimumConfidence)
            .OrderByDescending(nm => nm.Confidence)
            .ThenBy(nm => nm.CanonicalName)
            .ThenBy(nm => nm.Nickname)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Searches for nickname mappings by text with optional locale filtering


    /// </summary>


    /// <param name="searchText">The text to search for in names</param>


    /// <param name="locale">Optional locale filter</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of matching nickname mappings</returns>


    /// <exception cref="ArgumentException">Thrown when searchText is null or whitespace</exception>


    public async Task<IReadOnlyList<NicknameMap>> SearchMappingsAsync(string searchTerm, Locale? locale = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        var normalizedSearchTerm = searchTerm.ToUpperInvariant().Trim();
        var query = _context.NicknameMaps.Where(nm =>
            EF.Functions.ILike(nm.NormalizedCanonicalName, $"%{normalizedSearchTerm}%") ||
            EF.Functions.ILike(nm.NormalizedNickname, $"%{normalizedSearchTerm}%"));

        if (locale != null)
        {
            query = query.Where(nm => nm.Locale.Code == locale.Code);
        }

        return await query
            .OrderByDescending(nm => nm.Confidence)
            .ThenBy(nm => nm.CanonicalName)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Adds a new nickname map to the repository


    /// </summary>


    /// <param name="nicknameMap">The nickname map to add</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The added nickname map with updated identifier</returns>


    /// <exception cref="ArgumentNullException">Thrown when nicknameMap is null</exception>


    public async Task<NicknameMap> AddAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nicknameMap);

        _context.NicknameMaps.Add(nicknameMap);
        await _context.SaveChangesAsync(cancellationToken);
        return nicknameMap;
    }

    /// <summary>


    /// Adds multiple nickname maps to the repository in a single operation


    /// </summary>


    /// <param name="nicknameMaps">The collection of nickname maps to add</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nicknameMaps is null</exception>


    public async Task<int> AddRangeAsync(IEnumerable<NicknameMap> nicknameMaps, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nicknameMaps);

        var maps = nicknameMaps.ToList();
        if (maps.Count > 0)
        {
            _context.NicknameMaps.AddRange(maps);
            await _context.SaveChangesAsync(cancellationToken);
            return maps.Count;
        }
        return 0;
    }

    /// <summary>


    /// Updates an existing nickname map in the repository


    /// </summary>


    /// <param name="nicknameMap">The nickname map to update</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nicknameMap is null</exception>


    public async Task UpdateAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nicknameMap);

        _context.NicknameMaps.Update(nicknameMap);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>


    /// Deletes a nickname map from the repository


    /// </summary>


    /// <param name="nicknameMap">The nickname map to delete</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nicknameMap is null</exception>


    public async Task DeleteAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nicknameMap);

        _context.NicknameMaps.Remove(nicknameMap);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>


    /// Deletes multiple nickname maps from the repository in a single operation


    /// </summary>


    /// <param name="nicknameMaps">The collection of nickname maps to delete</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nicknameMaps is null</exception>


    public async Task DeleteRangeAsync(IEnumerable<NicknameMap> nicknameMaps, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nicknameMaps);

        var maps = nicknameMaps.ToList();
        if (maps.Count > 0)
        {
            _context.NicknameMaps.RemoveRange(maps);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>


    /// Checks if a nickname mapping exists for the specified canonical name, nickname, and locale


    /// </summary>


    /// <param name="canonicalName">The canonical name to check</param>


    /// <param name="nickname">The nickname to check</param>


    /// <param name="locale">The locale to check</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>True if the mapping exists, false otherwise</returns>


    public async Task<bool> ExistsAsync(string canonicalName, string nickname, Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentNullException.ThrowIfNull(locale);

        return await _context.NicknameMaps
            .AnyAsync(nm => nm.CanonicalName == canonicalName && 
                           nm.Nickname == nickname && 
                           nm.Locale.Code == locale.Code, 
                     cancellationToken);
    }

    /// <summary>


    /// Gets the count of nickname mappings for a specific locale


    /// </summary>


    /// <param name="locale">The locale to count mappings for</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of nickname mappings for the specified locale</returns>


    public async Task<int> GetCountByLocaleAsync(Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locale);

        return await _context.NicknameMaps
            .CountAsync(nm => nm.Locale.Code == locale.Code, cancellationToken);
    }

    /// <summary>


    /// Gets the total count of nickname mappings in the repository


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The total number of nickname mappings</returns>


    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NicknameMaps.LongCountAsync(cancellationToken);
    }

    /// <summary>


    /// Updates the confidence score for a specific nickname mapping


    /// </summary>


    /// <param name="id">The nickname map identifier</param>


    /// <param name="confidence">The new confidence score</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>True if the update was successful, false otherwise</returns>


    public async Task<bool> UpdateConfidenceAsync(long id, decimal newConfidence, CancellationToken cancellationToken = default)
    {
        var nicknameMap = await _context.NicknameMaps.FirstOrDefaultAsync(nm => nm.Id == id, cancellationToken);
        
        if (nicknameMap != null)
        {
            nicknameMap.UpdateConfidence(newConfidence);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }

    /// <summary>


    /// Gets the total count of all nickname mappings


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The total count of nickname mappings</returns>


    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NicknameMaps.CountAsync(cancellationToken);
    }

    /// <summary>


    /// Gets all nickname mappings in the repository


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of all nickname mappings</returns>


    public async Task<IReadOnlyList<NicknameMap>> GetAllMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NicknameMaps
            .OrderBy(nm => nm.Locale.Code)
            .ThenBy(nm => nm.CanonicalName)
            .ThenBy(nm => nm.Nickname)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Updates the confidence score for a specific nickname mapping


    /// </summary>


    /// <param name="id">The nickname map identifier</param>


    /// <param name="confidence">The new confidence score</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>True if the update was successful, false otherwise</returns>


    public async Task<bool> UpdateConfidenceAsync(IReadOnlyDictionary<long, decimal> confidenceUpdates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(confidenceUpdates);

        if (confidenceUpdates.Count == 0) return true;

        var ids = confidenceUpdates.Keys.ToList();
        var nicknameMaps = await _context.NicknameMaps
            .Where(nm => ids.Contains(nm.Id))
            .ToListAsync(cancellationToken);

        foreach (var nicknameMap in nicknameMaps)
        {
            if (confidenceUpdates.TryGetValue(nicknameMap.Id, out var newConfidence))
            {
                nicknameMap.UpdateConfidence(newConfidence);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Gets nicknames with confidence filtering for curated nickname service
    /// </summary>
    public async Task<IReadOnlyList<NicknameMap>> GetNicknamesAsync(
        string baseName,
        string? culture = null,
        double minConfidence = 0.5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);

        var normalizedBaseName = baseName.ToUpperInvariant().Trim();
        var query = _context.NicknameMaps
            .Where(nm => nm.NormalizedCanonicalName == normalizedBaseName &&
                        nm.Confidence >= (decimal)minConfidence);

        if (!string.IsNullOrEmpty(culture))
        {
            query = query.Where(nm => nm.Locale.Code == culture);
        }

        return await query
            .OrderByDescending(nm => nm.Confidence)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets base names with confidence filtering for curated nickname service
    /// </summary>
    public async Task<IReadOnlyList<NicknameMap>> GetBaseNamesAsync(
        string nickname,
        string? culture = null,
        double minConfidence = 0.5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        var normalizedNickname = nickname.ToUpperInvariant().Trim();
        var query = _context.NicknameMaps
            .Where(nm => nm.NormalizedNickname == normalizedNickname &&
                        nm.Confidence >= (decimal)minConfidence);

        if (!string.IsNullOrEmpty(culture))
        {
            query = query.Where(nm => nm.Locale.Code == culture);
        }

        return await query
            .OrderByDescending(nm => nm.Confidence)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all active nicknames for fuzzy matching
    /// </summary>
    public async Task<IReadOnlyList<NicknameMap>> GetAllActiveNicknamesAsync(
        string? culture = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.NicknameMaps.AsQueryable();

        if (!string.IsNullOrEmpty(culture))
        {
            query = query.Where(nm => nm.Locale.Code == culture);
        }

        return await query
            .Take(5000) // Limit for performance
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets specific mappings between a base name and nickname
    /// </summary>
    public async Task<IReadOnlyList<NicknameMap>> GetMappingsAsync(
        string baseName,
        string nickname,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        var normalizedBaseName = baseName.ToUpperInvariant().Trim();
        var normalizedNickname = nickname.ToUpperInvariant().Trim();

        return await _context.NicknameMaps
            .Where(nm => nm.NormalizedCanonicalName == normalizedBaseName &&
                        nm.NormalizedNickname == normalizedNickname)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets statistical information about the nickname dataset
    /// </summary>
    public async Task<NicknameStatisticsData> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var activeNicknames = _context.NicknameMaps.AsQueryable();

        var totalMappings = await activeNicknames.CountAsync(cancellationToken);
        var uniqueBases = await activeNicknames.Select(nm => nm.NormalizedCanonicalName).Distinct().CountAsync(cancellationToken);
        var uniqueNicknames = await activeNicknames.Select(nm => nm.NormalizedNickname).Distinct().CountAsync(cancellationToken);
        var averageConfidence = totalMappings > 0 ? 
            await activeNicknames.AverageAsync(nm => (double)nm.Confidence, cancellationToken) : 0.0;

        var cultureDistribution = await activeNicknames
            .GroupBy(nm => nm.Locale.Code)
            .Select(g => new { Culture = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Culture, x => x.Count, cancellationToken);

        // Source distribution not available in current entity - using placeholder
        var sourceDistribution = new Dictionary<string, int> { ["database"] = totalMappings };

        return new NicknameStatisticsData
        {
            TotalMappings = totalMappings,
            UniqueBases = uniqueBases,
            UniqueNicknames = uniqueNicknames,
            AverageConfidence = averageConfidence,
            CultureDistribution = cultureDistribution,
            SourceDistribution = sourceDistribution,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new nickname mapping
    /// </summary>
    public async Task<NicknameMap> CreateAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nicknameMap);

        _context.NicknameMaps.Add(nicknameMap);
        await _context.SaveChangesAsync(cancellationToken);
        return nicknameMap;
    }
}

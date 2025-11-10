using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Enums;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for NameAlias entities
/// </summary>
public sealed class NameAliasRepository : INameAliasRepository
{
    private readonly PhoneticAnalyzersDbContext _context;

    /// <summary>
    /// Initializes a new instance of the NameAliasRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    public NameAliasRepository(PhoneticAnalyzersDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets a name alias by its unique identifier
    /// </summary>
    /// <param name="id">The name alias identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The name alias if found, null otherwise</returns>
    public async Task<NameAlias?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.NameAliases
            .Include(na => na.PersonName)
            .FirstOrDefaultAsync(na => na.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets all name aliases associated with a specific person name
    /// </summary>
    /// <param name="personNameId">The person name identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of name aliases for the specified person name</returns>
    public async Task<IReadOnlyList<NameAlias>> GetByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default)
    {
        return await _context.NameAliases
            .Where(na => na.PersonNameId == personNameId)
            .OrderBy(na => na.Confidence)
            .ThenBy(na => na.Alias)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Searches for name aliases by normalized text with confidence and limit filtering


    /// </summary>


    /// <param name="normalizedText">The normalized text to search for</param>


    /// <param name="locale">Optional locale filter</param>


    /// <param name="minConfidence">Minimum confidence score filter</param>


    /// <param name="limit">Maximum number of results to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of matching name aliases ordered by confidence</returns>


    /// <exception cref="ArgumentException">Thrown when normalizedText is null or whitespace</exception>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NameAlias>> SearchAsync(string normalizedText, Locale? locale = null, decimal minConfidence = 0.3m, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedText);
        ArgumentOutOfRangeException.ThrowIfNegative(minConfidence);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var query = _context.NameAliases
            .Include(na => na.PersonName)
            .Where(na => EF.Functions.ILike(na.NormalizedAlias, $"%{normalizedText}%") && 
                        na.Confidence >= minConfidence);

        if (locale != null)
        {
            query = query.Where(na => na.Locale.Code == locale.Code);
        }

        return await query
            .OrderByDescending(na => na.Confidence)
            .ThenBy(na => na.Alias)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets name aliases filtered by type and locale with pagination


    /// </summary>


    /// <param name="aliasType">The type of alias to filter by</param>


    /// <param name="locale">The locale to filter by</param>


    /// <param name="limit">Maximum number of results to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A paginated list of name aliases matching the criteria</returns>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NameAlias>> GetByTypeAndLocaleAsync(AliasType aliasType, Locale locale, int limit = 100, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        return await _context.NameAliases
            .Include(na => na.PersonName)
            .Where(na => na.AliasType == aliasType && na.Locale.Code == locale.Code)
            .OrderBy(na => na.Alias)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets name aliases filtered by their source


    /// </summary>


    /// <param name="source">The alias source to filter by</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of name aliases from the specified source</returns>


    public async Task<IReadOnlyList<NameAlias>> GetBySourceAsync(AliasSource source, CancellationToken cancellationToken = default)
    {
        return await _context.NameAliases
            .Include(na => na.PersonName)
            .Where(na => na.Source == source)
            .OrderByDescending(na => na.Confidence)
            .ThenBy(na => na.Alias)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets name aliases with confidence scores above the specified threshold


    /// </summary>


    /// <param name="minConfidence">Minimum confidence score threshold</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of high-confidence name aliases</returns>


    public async Task<IReadOnlyList<NameAlias>> GetHighConfidenceAliasesAsync(decimal minimumConfidence = 0.8m, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumConfidence, 0.0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumConfidence, 1.0m);

        return await _context.NameAliases
            .Include(na => na.PersonName)
            .Where(na => na.Confidence >= minimumConfidence)
            .OrderByDescending(na => na.Confidence)
            .ThenBy(na => na.Alias)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Adds a new name alias to the repository


    /// </summary>


    /// <param name="nameAlias">The name alias to add</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The added name alias with updated identifier</returns>


    /// <exception cref="ArgumentNullException">Thrown when nameAlias is null</exception>


    public async Task<NameAlias> AddAsync(NameAlias nameAlias, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAlias);

        _context.NameAliases.Add(nameAlias);
        await _context.SaveChangesAsync(cancellationToken);
        return nameAlias;
    }

    /// <summary>


    /// Adds multiple name aliases to the repository in a single operation


    /// </summary>


    /// <param name="nameAliases">The collection of name aliases to add</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nameAliases is null</exception>


    public async Task<int> AddRangeAsync(IEnumerable<NameAlias> nameAliases, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAliases);

        var aliases = nameAliases.ToList();
        if (aliases.Count > 0)
        {
            _context.NameAliases.AddRange(aliases);
            await _context.SaveChangesAsync(cancellationToken);
            return aliases.Count;
        }
        return 0;
    }

    /// <summary>


    /// Updates an existing name alias in the repository


    /// </summary>


    /// <param name="nameAlias">The name alias to update</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The updated name alias</returns>


    /// <exception cref="ArgumentNullException">Thrown when nameAlias is null</exception>


    public async Task<NameAlias> UpdateAsync(NameAlias nameAlias, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAlias);

        _context.NameAliases.Update(nameAlias);
        await _context.SaveChangesAsync(cancellationToken);
        return nameAlias;
    }

    /// <summary>


    /// Deletes a name alias from the repository


    /// </summary>


    /// <param name="nameAlias">The name alias to delete</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nameAlias is null</exception>


    public async Task DeleteAsync(NameAlias nameAlias, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAlias);

        _context.NameAliases.Remove(nameAlias);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>


    /// Deletes multiple name aliases from the repository in a single operation


    /// </summary>


    /// <param name="nameAliases">The collection of name aliases to delete</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <exception cref="ArgumentNullException">Thrown when nameAliases is null</exception>


    public async Task DeleteRangeAsync(IEnumerable<NameAlias> nameAliases, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameAliases);

        var aliases = nameAliases.ToList();
        if (aliases.Count > 0)
        {
            _context.NameAliases.RemoveRange(aliases);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>


    /// Checks if a name alias exists for the specified person name, alias text, and locale


    /// </summary>


    /// <param name="personNameId">The person name identifier</param>


    /// <param name="aliasText">The alias text to check</param>


    /// <param name="locale">The locale to check</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>True if the alias exists, false otherwise</returns>


    public async Task<bool> ExistsAsync(long personNameId, string alias, Locale locale, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentNullException.ThrowIfNull(locale);

        return await _context.NameAliases
            .AnyAsync(na => na.PersonNameId == personNameId && 
                           na.Alias == alias && 
                           na.Locale.Code == locale.Code, 
                     cancellationToken);
    }

    /// <summary>


    /// Deletes all name aliases associated with a specific person name


    /// </summary>


    /// <param name="personNameId">The person name identifier</param>


    /// <param name="cancellationToken">Cancellation token</param>


    public async Task<int> DeleteByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default)
    {
        var aliases = await _context.NameAliases
            .Where(na => na.PersonNameId == personNameId)
            .ToListAsync(cancellationToken);

        if (aliases.Count > 0)
        {
            _context.NameAliases.RemoveRange(aliases);
            await _context.SaveChangesAsync(cancellationToken);
            return aliases.Count;
        }

        return 0;
    }

    /// <summary>


    /// Gets the total count of name aliases in the repository


    /// </summary>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The total number of name aliases</returns>


    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NameAliases.LongCountAsync(cancellationToken);
    }

    /// <summary>


    /// Gets the count of name aliases for a specific person name


    /// </summary>


    /// <param name="personNameId">The person name identifier</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>The number of aliases for the specified person name</returns>


    public async Task<int> GetCountByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default)
    {
        return await _context.NameAliases
            .CountAsync(na => na.PersonNameId == personNameId, cancellationToken);
    }

    /// <summary>


    /// Searches for name aliases by phonetic codes (Double Metaphone and optionally Beider-Morse)


    /// </summary>


    /// <param name="dmCode">The Double Metaphone code to search for</param>


    /// <param name="bmCode">Optional Beider-Morse code for additional filtering</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of name aliases matching the phonetic codes</returns>


    /// <exception cref="ArgumentException">Thrown when dmCode is null or whitespace</exception>


    public async Task<IReadOnlyList<NameAlias>> SearchByPhoneticCodeAsync(string dmCode, string? bmCode = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dmCode);

        var query = _context.NameAliases
            .Include(na => na.PersonName)
            .Where(na => na.DoubleMetaphoneCode != null && na.DoubleMetaphoneCode.Value == dmCode);

        if (!string.IsNullOrWhiteSpace(bmCode))
        {
            query = query.Where(na => na.BeiderMorseCode != null && na.BeiderMorseCode.Value == bmCode);
        }

        return await query
            .OrderByDescending(na => na.Confidence)
            .ThenBy(na => na.Alias)
            .ToListAsync(cancellationToken);
    }

    /// <summary>


    /// Gets recently created name aliases from a specific source


    /// </summary>


    /// <param name="source">The alias source to filter by</param>


    /// <param name="limit">Maximum number of results to return</param>


    /// <param name="cancellationToken">Cancellation token</param>


    /// <returns>A list of recent name aliases ordered by creation date</returns>


    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>


    public async Task<IReadOnlyList<NameAlias>> GetRecentAliasesAsync(AliasSource source, int days = 7, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _context.NameAliases
            .Include(na => na.PersonName)
            .Where(na => na.Source == source && na.CreatedUtc >= cutoffDate)
            .OrderByDescending(na => na.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}

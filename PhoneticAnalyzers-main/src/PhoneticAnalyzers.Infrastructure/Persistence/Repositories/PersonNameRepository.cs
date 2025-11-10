using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PersonName entities
/// </summary>
public sealed class PersonNameRepository : IPersonNameRepository
{
    private readonly PhoneticAnalyzersDbContext _context;

    /// <summary>
    /// Initializes a new instance of the PersonNameRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    public PersonNameRepository(PhoneticAnalyzersDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets a person name by its unique identifier, including aliases
    /// </summary>
    /// <param name="id">The person name identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The person name if found, null otherwise</returns>
    public async Task<PersonName?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.PersonNames
            .Include(pn => pn.Aliases)
            .FirstOrDefaultAsync(pn => pn.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets a person name by its canonical name, including aliases
    /// </summary>
    /// <param name="canonicalName">The canonical name to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The person name if found, null otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when canonicalName is null or whitespace</exception>
    public async Task<PersonName?> GetByCanonicalNameAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);

        return await _context.PersonNames
            .Include(pn => pn.Aliases)
            .FirstOrDefaultAsync(pn => pn.CanonicalName == canonicalName, cancellationToken);
    }

    /// <summary>
    /// Searches for person names by normalized text with optional locale filtering
    /// </summary>
    /// <param name="normalizedText">The normalized text to search for</param>
    /// <param name="locale">Optional locale filter</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of matching person names</returns>
    /// <exception cref="ArgumentException">Thrown when normalizedText is null or whitespace</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is negative or zero</exception>
    public async Task<IReadOnlyList<PersonName>> SearchAsync(string normalizedText, Locale? locale = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedText);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var query = _context.PersonNames
            .Include(pn => pn.Aliases)
            .Where(pn => EF.Functions.ILike(pn.NormalizedName, $"%{normalizedText}%") ||
                        pn.Aliases.Any(alias => EF.Functions.ILike(alias.NormalizedAlias, $"%{normalizedText}%")));

        if (locale != null)
        {
            query = query.Where(pn => pn.LocaleHint.Code == locale.Code);
        }

        return await query
            .OrderBy(pn => pn.CanonicalName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets person names by locale with pagination
    /// </summary>
    /// <param name="locale">The locale to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A paginated list of person names for the specified locale</returns>
    /// <exception cref="ArgumentNullException">Thrown when locale is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when page or pageSize is negative or zero</exception>
    public async Task<IReadOnlyList<PersonName>> GetByLocaleAsync(Locale locale, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var skip = (page - 1) * pageSize;

        return await _context.PersonNames
            .Include(pn => pn.Aliases)
            .Where(pn => pn.LocaleHint.Code == locale.Code)
            .OrderBy(pn => pn.CanonicalName)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets person names that need enrichment based on last enrichment date
    /// </summary>
    /// <param name="limit">Maximum number of names to return</param>
    /// <param name="enrichmentIntervalDays">Number of days after which enrichment is considered stale</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of person names needing enrichment, ordered by staleness</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit or enrichmentIntervalDays is negative or zero</exception>
    public async Task<IReadOnlyList<PersonName>> GetNamesNeedingEnrichmentAsync(int limit = 100, int enrichmentIntervalDays = 30, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enrichmentIntervalDays);

        var enrichmentThreshold = DateTime.UtcNow.AddDays(-enrichmentIntervalDays);

        return await _context.PersonNames
            .Include(pn => pn.Aliases)
            .Where(pn => pn.LastEnrichmentUtc == null || pn.LastEnrichmentUtc < enrichmentThreshold)
            .OrderBy(pn => pn.LastEnrichmentUtc ?? DateTime.MinValue)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new person name to the repository
    /// </summary>
    /// <param name="personName">The person name to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added person name with updated identifier</returns>
    /// <exception cref="ArgumentNullException">Thrown when personName is null</exception>
    public async Task<PersonName> AddAsync(PersonName personName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personName);

        _context.PersonNames.Add(personName);
        await _context.SaveChangesAsync(cancellationToken);
        return personName;
    }

    /// <summary>
    /// Updates an existing person name in the repository
    /// </summary>
    /// <param name="personName">The person name to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated person name</returns>
    /// <exception cref="ArgumentNullException">Thrown when personName is null</exception>
    public async Task<PersonName> UpdateAsync(PersonName personName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personName);

        _context.PersonNames.Update(personName);
        await _context.SaveChangesAsync(cancellationToken);
        return personName;
    }

    /// <summary>
    /// Deletes a person name from the repository
    /// </summary>
    /// <param name="personName">The person name to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentNullException">Thrown when personName is null</exception>
    public async Task DeleteAsync(PersonName personName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personName);

        _context.PersonNames.Remove(personName);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a person name with the specified canonical name exists
    /// </summary>
    /// <param name="canonicalName">The canonical name to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the person name exists, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when canonicalName is null or whitespace</exception>
    public async Task<bool> ExistsAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);

        return await _context.PersonNames
            .AnyAsync(pn => pn.CanonicalName == canonicalName, cancellationToken);
    }

    /// <summary>
    /// Gets the total count of person names in the repository
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The total number of person names</returns>
    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PersonNames.LongCountAsync(cancellationToken);
    }

    /// <summary>
    /// Searches for person names by phonetic codes (Double Metaphone and optionally Beider-Morse)
    /// </summary>
    /// <param name="dmCode">Double Metaphone code to search for</param>
    /// <param name="bmCode">Optional Beider-Morse code to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of person names matching the phonetic codes</returns>
    /// <exception cref="ArgumentException">Thrown when dmCode is null or whitespace</exception>
    public async Task<IReadOnlyList<PersonName>> SearchByPhoneticCodeAsync(string dmCode, string? bmCode = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dmCode);

        var query = _context.PersonNames
            .Include(pn => pn.Aliases)
            .Where(pn => pn.DoubleMetaphoneCode != null && pn.DoubleMetaphoneCode.Value == dmCode);

        if (!string.IsNullOrWhiteSpace(bmCode))
        {
            query = query.Where(pn => pn.BeiderMorseCode != null && pn.BeiderMorseCode.Value == bmCode);
        }

        return await query
            .OrderBy(pn => pn.CanonicalName)
            .ToListAsync(cancellationToken);
    }
}

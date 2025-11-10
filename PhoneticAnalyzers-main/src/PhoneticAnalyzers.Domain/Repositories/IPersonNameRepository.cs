using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Repositories;

/// <summary>
/// Repository interface for PersonName entities
/// </summary>
public interface IPersonNameRepository
{
    /// <summary>
    /// Adds a new person name to the repository
    /// </summary>
    /// <param name="personName">The person name to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The added person name</returns>
    Task<PersonName> AddAsync(PersonName personName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing person name in the repository
    /// </summary>
    /// <param name="personName">The person name to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated person name</returns>
    Task<PersonName> UpdateAsync(PersonName personName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a person name by ID
    /// </summary>
    /// <param name="id">The person name ID</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The person name if found, null otherwise</returns>
    Task<PersonName?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a person name by canonical name
    /// </summary>
    /// <param name="canonicalName">The canonical name</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The person name if found, null otherwise</returns>
    Task<PersonName?> GetByCanonicalNameAsync(string canonicalName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets person names that need LLM enrichment
    /// </summary>
    /// <param name="limit">The maximum number of names to return</param>
    /// <param name="enrichmentIntervalDays">The interval in days between enrichments</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of person names needing enrichment</returns>
    Task<IReadOnlyList<PersonName>> GetNamesNeedingEnrichmentAsync(
        int limit = 100,
        int enrichmentIntervalDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for person names by normalized text
    /// </summary>
    /// <param name="normalizedText">The normalized search text</param>
    /// <param name="locale">The preferred locale</param>
    /// <param name="limit">The maximum number of results</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of matching person names</returns>
    Task<IReadOnlyList<PersonName>> SearchAsync(
        string normalizedText,
        Locale? locale = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of person names
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The total count</returns>
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a person name exists by canonical name
    /// </summary>
    /// <param name="canonicalName">The canonical name</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string canonicalName, CancellationToken cancellationToken = default);
}
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Enums;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Repositories;

/// <summary>
/// Repository interface for NameAlias entities
/// </summary>
public interface INameAliasRepository
{
    /// <summary>
    /// Adds a new name alias to the repository
    /// </summary>
    /// <param name="nameAlias">The name alias to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The added name alias</returns>
    Task<NameAlias> AddAsync(NameAlias nameAlias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple name aliases to the repository
    /// </summary>
    /// <param name="nameAliases">The name aliases to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of aliases added</returns>
    Task<int> AddRangeAsync(IEnumerable<NameAlias> nameAliases, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing name alias in the repository
    /// </summary>
    /// <param name="nameAlias">The name alias to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated name alias</returns>
    Task<NameAlias> UpdateAsync(NameAlias nameAlias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a name alias by ID
    /// </summary>
    /// <param name="id">The name alias ID</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The name alias if found, null otherwise</returns>
    Task<NameAlias?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aliases by person name ID
    /// </summary>
    /// <param name="personNameId">The person name ID</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of name aliases</returns>
    Task<IReadOnlyList<NameAlias>> GetByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for aliases by normalized text
    /// </summary>
    /// <param name="normalizedText">The normalized search text</param>
    /// <param name="locale">The preferred locale</param>
    /// <param name="minConfidence">The minimum confidence threshold</param>
    /// <param name="limit">The maximum number of results</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of matching name aliases</returns>
    Task<IReadOnlyList<NameAlias>> SearchAsync(
        string normalizedText,
        Locale? locale = null,
        decimal minConfidence = 0.3m,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aliases by type and locale
    /// </summary>
    /// <param name="aliasType">The alias type</param>
    /// <param name="locale">The locale</param>
    /// <param name="limit">The maximum number of results</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of name aliases</returns>
    Task<IReadOnlyList<NameAlias>> GetByTypeAndLocaleAsync(
        AliasType aliasType,
        Locale locale,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes aliases by person name ID
    /// </summary>
    /// <param name="personNameId">The person name ID</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of aliases deleted</returns>
    Task<int> DeleteByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of name aliases
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The total count</returns>
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);
}
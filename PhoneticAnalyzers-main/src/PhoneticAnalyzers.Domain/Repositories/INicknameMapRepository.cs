using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Repositories;

/// <summary>
/// Repository interface for NicknameMap entities
/// </summary>
public interface INicknameMapRepository
{
    /// <summary>
    /// Adds a new nickname mapping to the repository
    /// </summary>
    /// <param name="nicknameMap">The nickname mapping to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The added nickname mapping</returns>
    Task<NicknameMap> AddAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple nickname mappings to the repository
    /// </summary>
    /// <param name="nicknameMaps">The nickname mappings to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of mappings added</returns>
    Task<int> AddRangeAsync(IEnumerable<NicknameMap> nicknameMaps, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nicknames for a canonical name
    /// </summary>
    /// <param name="canonicalName">The canonical name</param>
    /// <param name="locale">The locale</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of nickname mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetNicknamesAsync(
        string canonicalName,
        Locale? locale = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets canonical names for a nickname (reverse lookup)
    /// </summary>
    /// <param name="nickname">The nickname</param>
    /// <param name="locale">The locale</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of nickname mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetCanonicalNamesAsync(
        string nickname,
        Locale? locale = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all variants (both directions) for a name
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <param name="locale">The locale</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of all related name variants</returns>
    Task<IReadOnlyList<NicknameMap>> GetAllVariantsAsync(
        string name,
        Locale? locale = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nickname mappings by locale
    /// </summary>
    /// <param name="locale">The locale</param>
    /// <param name="limit">The maximum number of results</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of nickname mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetByLocaleAsync(
        Locale locale,
        int limit = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a nickname mapping exists
    /// </summary>
    /// <param name="canonicalName">The canonical name</param>
    /// <param name="nickname">The nickname</param>
    /// <param name="locale">The locale</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(
        string canonicalName,
        string nickname,
        Locale locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the confidence score for a nickname mapping
    /// </summary>
    /// <param name="id">The nickname mapping ID</param>
    /// <param name="newConfidence">The new confidence score</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> UpdateConfidenceAsync(
        long id,
        decimal newConfidence,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of nickname mappings
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The total count</returns>
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nicknames with confidence filtering for curated nickname service
    /// </summary>
    /// <param name="baseName">The base name to find nicknames for</param>
    /// <param name="culture">Optional culture filter</param>
    /// <param name="minConfidence">Minimum confidence score</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>List of nickname mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetNicknamesAsync(
        string baseName,
        string? culture = null,
        double minConfidence = 0.5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets base names with confidence filtering for curated nickname service
    /// </summary>
    /// <param name="nickname">The nickname to find base names for</param>
    /// <param name="culture">Optional culture filter</param>
    /// <param name="minConfidence">Minimum confidence score</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>List of nickname mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetBaseNamesAsync(
        string nickname,
        string? culture = null,
        double minConfidence = 0.5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active nicknames for fuzzy matching
    /// </summary>
    /// <param name="culture">Optional culture filter</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>List of all active nickname mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetAllActiveNicknamesAsync(
        string? culture = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets specific mappings between a base name and nickname
    /// </summary>
    /// <param name="baseName">The base name</param>
    /// <param name="nickname">The nickname</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>List of matching mappings</returns>
    Task<IReadOnlyList<NicknameMap>> GetMappingsAsync(
        string baseName,
        string nickname,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistical information about the nickname dataset
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Statistics object</returns>
    Task<NicknameStatisticsData> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new nickname mapping
    /// </summary>
    /// <param name="nicknameMap">The nickname mapping to create</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The created nickname mapping</returns>
    Task<NicknameMap> CreateAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default);
}
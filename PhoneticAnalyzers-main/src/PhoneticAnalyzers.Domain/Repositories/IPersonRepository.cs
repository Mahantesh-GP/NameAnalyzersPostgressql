using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Repositories;

/// <summary>
/// Repository interface for Person entities
/// </summary>
public interface IPersonRepository
{
    /// <summary>
    /// Adds a new person to the repository
    /// </summary>
    /// <param name="person">The person to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The added person</returns>
    Task<Person> AddAsync(Person person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing person in the repository
    /// </summary>
    /// <param name="person">The person to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated person</returns>
    Task<Person> UpdateAsync(Person person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a person by their external ID
    /// </summary>
    /// <param name="externalId">The external ID to search for</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The person if found, null otherwise</returns>
    Task<Person?> GetByExternalIdAsync(ExternalId externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a person by their internal ID
    /// </summary>
    /// <param name="id">The internal ID to search for</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The person if found, null otherwise</returns>
    Task<Person?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for persons using phonetic matching
    /// </summary>
    /// <param name="searchCriteria">The phonetic search criteria</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of matching persons with their similarity scores</returns>
    Task<IReadOnlyList<PhoneticSearchResult>> SearchByPhoneticAsync(
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a batch upsert operation (insert or update based on external ID)
    /// </summary>
    /// <param name="persons">The persons to upsert</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The upserted persons</returns>
    Task<IReadOnlyList<Person>> UpsertBatchAsync(
        IEnumerable<Person> persons,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of persons in the repository
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The total count</returns>
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a person exists with the given external ID
    /// </summary>
    /// <param name="externalId">The external ID to check</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if the person exists, false otherwise</returns>
    Task<bool> ExistsAsync(ExternalId externalId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Criteria for phonetic searching
/// </summary>
public sealed class PhoneticSearchCriteria
{
    /// <summary>
    /// Gets the normalized query name
    /// </summary>
    public NormalizedName QueryName { get; }

    /// <summary>
    /// Gets the primary Double Metaphone code for the query
    /// </summary>
    public PhoneticCode? PrimaryDoubleMetaphone { get; }

    /// <summary>
    /// Gets the alternate Double Metaphone code for the query
    /// </summary>
    public PhoneticCode? AlternateDoubleMetaphone { get; }

    /// <summary>
    /// Gets the Beider-Morse codes for the query
    /// </summary>
    public IReadOnlyList<PhoneticCode> BeiderMorseCodes { get; }

    /// <summary>
    /// Gets the maximum number of results to return
    /// </summary>
    public int MaxResults { get; }

    /// <summary>
    /// Gets the minimum similarity threshold (0.0 to 1.0)
    /// </summary>
    public double MinSimilarityThreshold { get; }

    /// <summary>
    /// Gets whether to include trigram similarity search
    /// </summary>
    public bool IncludeTrigramSimilarity { get; }

    /// <summary>
    /// Initializes a new instance of the PhoneticSearchCriteria class
    /// </summary>
    /// <param name="queryName">The normalized query name</param>
    /// <param name="primaryDoubleMetaphone">The primary Double Metaphone code</param>
    /// <param name="alternateDoubleMetaphone">The alternate Double Metaphone code</param>
    /// <param name="beiderMorseCodes">The Beider-Morse codes</param>
    /// <param name="maxResults">The maximum number of results to return</param>
    /// <param name="minSimilarityThreshold">The minimum similarity threshold</param>
    /// <param name="includeTrigramSimilarity">Whether to include trigram similarity search</param>
    public PhoneticSearchCriteria(
        NormalizedName queryName,
        PhoneticCode? primaryDoubleMetaphone = null,
        PhoneticCode? alternateDoubleMetaphone = null,
        IReadOnlyList<PhoneticCode>? beiderMorseCodes = null,
        int maxResults = 100,
        double minSimilarityThreshold = 0.3,
        bool includeTrigramSimilarity = true)
    {
        if (maxResults <= 0)
            throw new ArgumentException("Max results must be greater than zero", nameof(maxResults));

        if (minSimilarityThreshold < 0.0 || minSimilarityThreshold > 1.0)
            throw new ArgumentException("Similarity threshold must be between 0.0 and 1.0", nameof(minSimilarityThreshold));

        QueryName = queryName;
        PrimaryDoubleMetaphone = primaryDoubleMetaphone;
        AlternateDoubleMetaphone = alternateDoubleMetaphone;
        BeiderMorseCodes = beiderMorseCodes ?? [];
        MaxResults = maxResults;
        MinSimilarityThreshold = minSimilarityThreshold;
        IncludeTrigramSimilarity = includeTrigramSimilarity;
    }
}

/// <summary>
/// Result from a phonetic search operation
/// </summary>
public sealed class PhoneticSearchResult
{
    /// <summary>
    /// Gets the person that matched the search criteria
    /// </summary>
    public Person Person { get; }

    /// <summary>
    /// Gets the similarity score (0.0 to 1.0)
    /// </summary>
    public double SimilarityScore { get; }

    /// <summary>
    /// Gets the match type that produced this result
    /// </summary>
    public PhoneticMatchType MatchType { get; }

    /// <summary>
    /// Gets additional metadata about the match
    /// </summary>
    public string? MatchMetadata { get; }

    /// <summary>
    /// Initializes a new instance of the PhoneticSearchResult class
    /// </summary>
    /// <param name="person">The matching person</param>
    /// <param name="similarityScore">The similarity score</param>
    /// <param name="matchType">The match type</param>
    /// <param name="matchMetadata">Optional match metadata</param>
    public PhoneticSearchResult(Person person, double similarityScore, PhoneticMatchType matchType, string? matchMetadata = null)
    {
        if (similarityScore < 0.0 || similarityScore > 1.0)
            throw new ArgumentException("Similarity score must be between 0.0 and 1.0", nameof(similarityScore));

        Person = person ?? throw new ArgumentNullException(nameof(person));
        SimilarityScore = similarityScore;
        MatchType = matchType;
        MatchMetadata = matchMetadata;
    }
}

/// <summary>
/// Types of phonetic matches
/// </summary>
public enum PhoneticMatchType
{
    /// <summary>
    /// Exact name match
    /// </summary>
    Exact,

    /// <summary>
    /// Match via primary Double Metaphone code
    /// </summary>
    PrimaryDoubleMetaphone,

    /// <summary>
    /// Match via alternate Double Metaphone code
    /// </summary>
    AlternateDoubleMetaphone,

    /// <summary>
    /// Match via Beider-Morse code
    /// </summary>
    BeiderMorse,

    /// <summary>
    /// Match via trigram similarity
    /// </summary>
    TrigramSimilarity
}
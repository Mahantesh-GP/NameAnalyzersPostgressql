// FluentValidation removed for smoke-test simplification
using MediatR;
using FluentValidation;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Application.Queries.Search;

/// <summary>
/// Query to search for persons using phonetic matching
/// </summary>
public sealed class SearchPersonsQuery : IRequest<SearchPersonsQueryResult>
{
    /// <summary>
    /// Gets the name to search for
    /// </summary>
    public string QueryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the maximum number of results to return
    /// </summary>
    public int MaxResults { get; init; } = 100;

    /// <summary>
    /// Gets the minimum similarity threshold (0.0 to 1.0)
    /// </summary>
    public double MinSimilarityThreshold { get; init; } = 0.3;

    /// <summary>
    /// Gets whether to include trigram similarity search
    /// </summary>
    public bool IncludeTrigramSimilarity { get; init; } = true;

    /// <summary>
    /// Gets whether to expand nicknames during search
    /// </summary>
    public bool ExpandNicknames { get; init; } = true;

    /// <summary>
    /// Gets whether to include phonetic match details in results
    /// </summary>
    public bool IncludeMatchDetails { get; init; } = true;

    /// <summary>
    /// Gets the specific phonetic algorithms to use (empty means all)
    /// </summary>
    public IReadOnlyList<PhoneticAlgorithmType> AlgorithmTypes { get; init; } = [];
}

/// <summary>
/// Result of phonetic person search
/// </summary>
public sealed class SearchPersonsQueryResult
{
    /// <summary>
    /// Gets the original query name
    /// </summary>
    public string QueryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the normalized query name
    /// </summary>
    public string NormalizedQueryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the matching persons
    /// </summary>
    public IReadOnlyList<PersonSearchResult> Matches { get; init; } = [];

    /// <summary>
    /// Gets the total count of potential matches (before similarity filtering)
    /// </summary>
    public int TotalCandidates { get; init; }

    /// <summary>
    /// Gets the search execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Gets the phonetic codes generated for the query
    /// </summary>
    public QueryPhoneticCodes PhoneticCodes { get; init; } = new();

    /// <summary>
    /// Gets any warnings generated during the search
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Individual search result for a person
/// </summary>
public sealed class PersonSearchResult
{
    /// <summary>
    /// Gets the person ID
    /// </summary>
    public long PersonId { get; init; }

    /// <summary>
    /// Gets the external ID
    /// </summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full name
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the normalized name
    /// </summary>
    public string NormalizedName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the similarity score (0.0 to 1.0)
    /// </summary>
    public double SimilarityScore { get; init; }

    /// <summary>
    /// Gets the match type that produced this result
    /// </summary>
    public PhoneticMatchType MatchType { get; init; }

    /// <summary>
    /// Gets additional match metadata
    /// </summary>
    public string? MatchMetadata { get; init; }

    /// <summary>
    /// Gets the person's phonetic codes (if requested)
    /// </summary>
    public PersonPhoneticCodes? PhoneticCodes { get; init; }
}

/// <summary>
/// Phonetic codes generated for the search query
/// </summary>
public sealed class QueryPhoneticCodes
{
    /// <summary>
    /// Gets the primary Double Metaphone code
    /// </summary>
    public string? PrimaryDoubleMetaphone { get; init; }

    /// <summary>
    /// Gets the alternate Double Metaphone code
    /// </summary>
    public string? AlternateDoubleMetaphone { get; init; }

    /// <summary>
    /// Gets the Beider-Morse codes
    /// </summary>
    public IReadOnlyList<string> BeiderMorseCodes { get; init; } = [];

    /// <summary>
    /// Gets the expanded nickname variations
    /// </summary>
    public IReadOnlyList<string> NicknameVariations { get; init; } = [];
}

/// <summary>
/// Phonetic codes for a person in search results
/// </summary>
public sealed class PersonPhoneticCodes
{
    /// <summary>
    /// Gets the primary Double Metaphone code
    /// </summary>
    public string? PrimaryDoubleMetaphone { get; init; }

    /// <summary>
    /// Gets the alternate Double Metaphone code
    /// </summary>
    public string? AlternateDoubleMetaphone { get; init; }

    /// <summary>
    /// Gets the Beider-Morse codes
    /// </summary>
    public IReadOnlyList<string> BeiderMorseCodes { get; init; } = [];
}

// removed duplicate misplaced using

/// <summary>
/// Validator for <see cref="SearchPersonsQuery"/>
/// </summary>
public sealed class SearchPersonsQueryValidator : AbstractValidator<SearchPersonsQuery>
{
    /// <summary>
    /// Creates a new validator instance for <see cref="SearchPersonsQuery"/>
    /// </summary>
    public SearchPersonsQueryValidator()
    {
        RuleFor(x => x.QueryName)
            .NotEmpty().WithMessage("Query name is required")
            .MinimumLength(2).WithMessage("Query name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Query name cannot exceed 200 characters");

        RuleFor(x => x.MaxResults)
            .GreaterThan(0).WithMessage("Max results must be greater than zero")
            .LessThanOrEqualTo(500).WithMessage("Max results cannot exceed 500");

        RuleFor(x => x.MinSimilarityThreshold)
            .InclusiveBetween(0.0, 1.0).WithMessage("Similarity threshold must be between 0.0 and 1.0")
            .LessThanOrEqualTo(0.9).WithMessage("Similarity threshold cannot exceed 0.9 to allow matches");

        RuleFor(x => x.AlgorithmTypes)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("Algorithm types cannot contain duplicates");
    }
}

/// <summary>
/// Query to get detailed information about a specific person
/// </summary>
public sealed class GetPersonDetailsQuery : IRequest<GetPersonDetailsQueryResult>
{
    /// <summary>
    /// Gets the external ID of the person to retrieve
    /// </summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether to include phonetic code details
    /// </summary>
    public bool IncludePhoneticCodes { get; init; } = true;
}

/// <summary>
/// Result of get person details query
/// </summary>
public sealed class GetPersonDetailsQueryResult
{
    /// <summary>
    /// Gets the person details (null if not found)
    /// </summary>
    public PersonDetails? Person { get; init; }

    /// <summary>
    /// Gets whether the person was found
    /// </summary>
    public bool Found => Person != null;
}

/// <summary>
/// Detailed information about a person
/// </summary>
public sealed class PersonDetails
{
    /// <summary>
    /// Gets the person ID
    /// </summary>
    public long PersonId { get; init; }

    /// <summary>
    /// Gets the external ID
    /// </summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full name
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the normalized name
    /// </summary>
    public string NormalizedName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the first letter for indexing
    /// </summary>
    public char FirstLetter { get; init; }

    /// <summary>
    /// Gets the partition hash
    /// </summary>
    public int PartitionHash { get; init; }

    /// <summary>
    /// Gets the creation timestamp
    /// </summary>
    public DateTime CreatedUtc { get; init; }

    /// <summary>
    /// Gets the last update timestamp
    /// </summary>
    public DateTime? UpdatedUtc { get; init; }

    /// <summary>
    /// Gets the phonetic codes (if requested)
    /// </summary>
    public PersonPhoneticCodes? PhoneticCodes { get; init; }
}

/// <summary>
/// Validator for <see cref="GetPersonDetailsQuery"/>
/// </summary>
public sealed class GetPersonDetailsQueryValidator : AbstractValidator<GetPersonDetailsQuery>
{
    /// <summary>
    /// Creates a new validator instance for <see cref="GetPersonDetailsQuery"/>
    /// </summary>
    public GetPersonDetailsQueryValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("External ID is required")
            .MaximumLength(64).WithMessage("External ID cannot exceed 64 characters");
    }
}
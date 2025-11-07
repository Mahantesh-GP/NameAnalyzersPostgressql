using MediatR;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Queries.Search;
using PhoneticAnalyzers.Application.Services.Phonetic;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using System.Diagnostics;

namespace PhoneticAnalyzers.Application.Handlers.Search;

/// <summary>
/// Handler for SearchPersonsQuery
/// </summary>
public sealed class SearchPersonsQueryHandler : IRequestHandler<SearchPersonsQuery, SearchPersonsQueryResult>
{
    private readonly IPersonRepository _personRepository;
    private readonly IPhoneticEncodingService _phoneticService;
    private readonly ILogger<SearchPersonsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the SearchPersonsQueryHandler class
    /// </summary>
    /// <param name="personRepository">The person repository</param>
    /// <param name="phoneticService">The phonetic service</param>
    /// <param name="logger">The logger</param>
    public SearchPersonsQueryHandler(
        IPersonRepository personRepository,
        IPhoneticEncodingService phoneticService,
        ILogger<SearchPersonsQueryHandler> logger)
    {
        _personRepository = personRepository;
        _phoneticService = phoneticService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the SearchPersonsQuery request
    /// </summary>
    /// <param name="request">The search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    public async Task<SearchPersonsQueryResult> Handle(SearchPersonsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing search request for query: {QueryName}", request.QueryName);
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.QueryName))
            {
                return new SearchPersonsQueryResult
                {
                    QueryName = request.QueryName,
                    ExecutionTime = stopwatch.Elapsed,
                    Warnings = new[] { "Query name cannot be empty" }
                };
            }

            // Normalize the query name and generate phonetic codes
            var normalizedName = NormalizedName.Create(request.QueryName);
            var phoneticResult = await _phoneticService.EncodeAsync(normalizedName);
            
            // Create search criteria
            var searchCriteria = new PhoneticSearchCriteria(
                normalizedName,
                phoneticResult.PrimaryDoubleMetaphone,
                phoneticResult.AlternateDoubleMetaphone,
                phoneticResult.BeiderMorseCodes,
                request.MaxResults,
                request.MinSimilarityThreshold,
                request.IncludeTrigramSimilarity);

            // Perform the search
            var repositoryResults = await _personRepository.SearchByPhoneticAsync(searchCriteria, cancellationToken);

            // Convert repository results to query results
            var matches = repositoryResults.Select(result => new PersonSearchResult
            {
                PersonId = result.Person.Id,
                ExternalId = result.Person.ExternalId.Value,
                FullName = result.Person.FullName,
                NormalizedName = result.Person.NormalizedName.Value,
                SimilarityScore = result.SimilarityScore,
                MatchType = result.MatchType,
                MatchMetadata = result.MatchMetadata,
                PhoneticCodes = request.IncludeMatchDetails ? ConvertToPersonPhoneticCodes(result.Person) : null
            }).ToList();

            // Generate query phonetic codes for response
            var queryPhoneticCodes = new QueryPhoneticCodes
            {
                PrimaryDoubleMetaphone = phoneticResult.PrimaryDoubleMetaphone?.Value,
                AlternateDoubleMetaphone = phoneticResult.AlternateDoubleMetaphone?.Value,
                BeiderMorseCodes = phoneticResult.BeiderMorseCodes.Select(c => c.Value).ToList(),
                NicknameVariations = request.ExpandNicknames ? GetCommonNicknames(normalizedName.Value) : new List<string>()
            };

            stopwatch.Stop();

            _logger.LogInformation(
                "Search completed. Query: {QueryName}, Results: {ResultCount}, Execution time: {ExecutionTime}ms",
                request.QueryName, matches.Count, stopwatch.ElapsedMilliseconds);

            return new SearchPersonsQueryResult
            {
                QueryName = request.QueryName,
                NormalizedQueryName = normalizedName.Value,
                Matches = matches,
                TotalCandidates = matches.Count,
                ExecutionTime = stopwatch.Elapsed,
                PhoneticCodes = queryPhoneticCodes,
                Warnings = new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request for query: {QueryName}", request.QueryName);
            
            stopwatch.Stop();
            return new SearchPersonsQueryResult
            {
                QueryName = request.QueryName,
                ExecutionTime = stopwatch.Elapsed,
                Warnings = new[] { $"Search failed: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Converts domain phonetic codes to query result format
    /// </summary>
    private static PersonPhoneticCodes ConvertToPersonPhoneticCodes(Person person)
    {
        return new PersonPhoneticCodes
        {
            PrimaryDoubleMetaphone = person.PrimaryDoubleMetaphone?.Value,
            AlternateDoubleMetaphone = person.AlternateDoubleMetaphone?.Value,
            BeiderMorseCodes = person.BeiderMorseVariants.Select(v => v.BeiderMorseCode.Value).ToList()
        };
    }

    /// <summary>
    /// Gets common nicknames for a given name
    /// </summary>
    private static List<string> GetCommonNicknames(string name)
    {
        // This is a simple implementation - in a real system you'd have a comprehensive nickname database
        var nicknames = new List<string>();
        
        var nameLower = name.ToLowerInvariant();
        
        // Simple nickname mappings
        var nicknameMap = new Dictionary<string, string[]>
        {
            { "robert", new[] { "rob", "bob", "bobby" } },
            { "william", new[] { "will", "bill", "billy" } },
            { "richard", new[] { "rick", "dick" } },
            { "michael", new[] { "mike", "mick" } },
            { "james", new[] { "jim", "jimmy" } },
            { "john", new[] { "johnny", "jack" } },
            { "elizabeth", new[] { "liz", "beth", "betty" } },
            { "margaret", new[] { "maggie", "meg", "peggy" } },
            { "catherine", new[] { "kate", "cathy" } },
            { "christopher", new[] { "chris", "kit" } }
        };

        if (nicknameMap.ContainsKey(nameLower))
        {
            nicknames.AddRange(nicknameMap[nameLower]);
        }

        return nicknames;
    }
}
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
    private readonly INicknameMapRepository _nicknameRepository;
    private readonly ILogger<SearchPersonsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the SearchPersonsQueryHandler class
    /// </summary>
    /// <param name="personRepository">The person repository</param>
    /// <param name="phoneticService">The phonetic service</param>
    /// <param name="nicknameRepository">The nickname repository</param>
    /// <param name="logger">The logger</param>
    public SearchPersonsQueryHandler(
        IPersonRepository personRepository,
        IPhoneticEncodingService phoneticService,
        INicknameMapRepository nicknameRepository,
        ILogger<SearchPersonsQueryHandler> logger)
    {
        _personRepository = personRepository;
        _phoneticService = phoneticService;
        _nicknameRepository = nicknameRepository;
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
            var nicknameVariants = request.ExpandNicknames 
                ? await GetNicknamesFromDatabaseAsync(normalizedName.Value, cancellationToken) 
                : new List<string>();

            var searchCriteria = new PhoneticSearchCriteria(
                normalizedName,
                phoneticResult.PrimaryDoubleMetaphone,
                phoneticResult.AlternateDoubleMetaphone,
                phoneticResult.BeiderMorseCodes,
                request.MaxResults,
                request.MinSimilarityThreshold,
                request.IncludeTrigramSimilarity,
                request.CountyId,
                request.RecordTypeFilter,
                nicknameVariants);

            // Perform the search
            var repositoryResults = await _personRepository.SearchByPhoneticAsync(searchCriteria, cancellationToken);

            // Convert repository results to query results
            var matches = repositoryResults.Select(result => new PersonSearchResult
            {
                PersonId = result.Person.Id,
                ExternalId = result.Person.ExternalId.Value,
                FullName = result.Person.FullName,
                NormalizedName = result.Person.NormalizedName.Value,
                County = result.Person.County,
                CountyId = result.Person.CountyId,
                CountyName = result.Person.CountyName,
                Flag = result.Person.Flag,
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
                NicknameVariations = nicknameVariants
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
    /// Gets nicknames from database for a given name, with fallback to hardcoded list
    /// </summary>
    private async Task<List<string>> GetNicknamesFromDatabaseAsync(string name, CancellationToken cancellationToken)
    {
        var nicknames = new List<string>();
        
        try
        {
            // Extract individual name tokens (e.g., "JOHN SMITH" -> ["JOHN", "SMITH"])
            var nameTokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var token in nameTokens)
            {
                // Query database for nicknames of this canonical name
                var nicknameMappings = await _nicknameRepository.GetNicknamesAsync(token, locale: null, cancellationToken);
                
                foreach (var mapping in nicknameMappings)
                {
                    var nickname = mapping.Nickname;
                    if (!string.IsNullOrWhiteSpace(nickname) && !nicknames.Contains(nickname, StringComparer.OrdinalIgnoreCase))
                    {
                        nicknames.Add(nickname.ToUpperInvariant());
                    }
                }

                // Also check reverse: if the token itself is a nickname, get the canonical name variants
                var canonicalMappings = await _nicknameRepository.GetCanonicalNamesAsync(token, locale: null, cancellationToken);
                
                foreach (var mapping in canonicalMappings)
                {
                    var canonical = mapping.CanonicalName;
                    if (!string.IsNullOrWhiteSpace(canonical) && !nicknames.Contains(canonical, StringComparer.OrdinalIgnoreCase))
                    {
                        nicknames.Add(canonical.ToUpperInvariant());
                    }
                }
            }

            _logger.LogDebug("Found {Count} nickname variants for name: {Name}", nicknames.Count, name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving nicknames from database for name: {Name}. Using fallback.", name);
            // Fallback to hardcoded nicknames if database query fails
            nicknames = GetCommonNicknamesFallback(name);
        }

        // If no database results, use fallback
        if (nicknames.Count == 0)
        {
            nicknames = GetCommonNicknamesFallback(name);
        }

        return nicknames;
    }

    /// <summary>
    /// Fallback method with hardcoded common nicknames (used when database is unavailable or empty)
    /// </summary>
    private List<string> GetCommonNicknamesFallback(string name)
    {
        var nicknames = new List<string>();
        var nameLower = name.ToLowerInvariant();
        
        // Hardcoded fallback nickname mappings
        var nicknameMap = new Dictionary<string, string[]>
        {
            { "robert", new[] { "ROB", "BOB", "BOBBY" } },
            { "william", new[] { "WILL", "BILL", "BILLY" } },
            { "richard", new[] { "RICK", "DICK" } },
            { "michael", new[] { "MIKE", "MICK" } },
            { "james", new[] { "JIM", "JIMMY" } },
            { "john", new[] { "JOHNNY", "JACK" } },
            { "elizabeth", new[] { "LIZ", "BETH", "BETTY" } },
            { "margaret", new[] { "MAGGIE", "MEG", "PEGGY" } },
            { "catherine", new[] { "KATE", "CATHY" } },
            { "christopher", new[] { "CHRIS", "KIT" } }
        };

        if (nicknameMap.ContainsKey(nameLower))
        {
            nicknames.AddRange(nicknameMap[nameLower]);
        }

        _logger.LogDebug("Using fallback nicknames for name: {Name}. Found {Count} variants.", name, nicknames.Count);
        return nicknames;
    }
}
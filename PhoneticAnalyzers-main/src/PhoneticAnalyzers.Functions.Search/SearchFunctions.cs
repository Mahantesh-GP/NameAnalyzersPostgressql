using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Queries.Search;
using MediatR;
using System.Net;
using System.Text.Json;

namespace PhoneticAnalyzers.Functions.Search;

/// <summary>
/// Azure Functions for phonetic search operations
/// </summary>
public class SearchFunctions
{
    private readonly ILogger<SearchFunctions> _logger;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the SearchFunctions class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="mediator">The mediator instance</param>
    public SearchFunctions(ILogger<SearchFunctions> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Health check endpoint for search functions
    /// </summary>
    [Function("SearchHealthCheck")]
    public async Task<HttpResponseData> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "search/health")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Search service health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            service = "PhoneticAnalyzers.Search",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        }, ct);

        return response;
    }

    /// <summary>
    /// Advanced search with multiple criteria
    /// </summary>
    [Function("AdvancedSearch")]
    public async Task<HttpResponseData> AdvancedSearch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "search/advanced")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Advanced search requested");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
            var searchRequest = JsonSerializer.Deserialize<AdvancedSearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (searchRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" }, ct);
                return badResponse;
            }

            var query = new SearchPersonsQuery
            {
                QueryName = searchRequest.QueryName,
                MaxResults = searchRequest.MaxResults ?? 50,
                MinSimilarityThreshold = searchRequest.MinSimilarityThreshold ?? 0.3,
                IncludeTrigramSimilarity = searchRequest.IncludeTrigramSimilarity ?? true,
                ExpandNicknames = searchRequest.ExpandNicknames ?? true,
                IncludeMatchDetails = searchRequest.IncludeMatchDetails ?? true
            };

            var searchResults = await _mediator.Send(query, ct);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                query = searchRequest.QueryName,
                parameters = new
                {
                    maxResults = query.MaxResults,
                    minSimilarityThreshold = query.MinSimilarityThreshold,
                    includeTrigramSimilarity = query.IncludeTrigramSimilarity,
                    expandNicknames = query.ExpandNicknames
                },
                totalMatches = searchResults.Matches.Count,
                executionTime = searchResults.ExecutionTime.TotalMilliseconds,
                phoneticCodes = new
                {
                    doubleMetaphone = new
                    {
                        primary = searchResults.PhoneticCodes.PrimaryDoubleMetaphone,
                        alternate = searchResults.PhoneticCodes.AlternateDoubleMetaphone
                    },
                    beiderMorse = searchResults.PhoneticCodes.BeiderMorseCodes,
                    nicknameVariations = searchResults.PhoneticCodes.NicknameVariations
                },
                results = searchResults.Matches.Select(match => new
                {
                    personId = match.PersonId,
                    externalId = match.ExternalId,
                    fullName = match.FullName,
                    normalizedName = match.NormalizedName,
                    similarityScore = match.SimilarityScore,
                    matchType = match.MatchType.ToString(),
                    matchMetadata = match.MatchMetadata,
                    phoneticCodes = match.PhoneticCodes != null ? new
                    {
                        doubleMetaphone = new
                        {
                            primary = match.PhoneticCodes.PrimaryDoubleMetaphone,
                            alternate = match.PhoneticCodes.AlternateDoubleMetaphone
                        },
                        beiderMorse = match.PhoneticCodes.BeiderMorseCodes
                    } : null
                }),
                warnings = searchResults.Warnings
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Internal server error",
                message = ex.Message
            }, ct);

            return errorResponse;
        }
    }

    /// <summary>
    /// Bulk search for multiple names
    /// </summary>
    [Function("BulkSearch")]
    public async Task<HttpResponseData> BulkSearch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "search/bulk")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Bulk search requested");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
            var bulkRequest = JsonSerializer.Deserialize<BulkSearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (bulkRequest?.SearchTerms == null || !bulkRequest.SearchTerms.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "No search terms provided" }, ct);
                return badResponse;
            }

            var bulkResults = new List<object>();
            var totalExecutionTime = TimeSpan.Zero;

            foreach (var searchTerm in bulkRequest.SearchTerms.Take(20)) // Limit to 20 searches per request
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Bulk search cancelled by client.");
                    break;
                }
                try
                {
                    var query = new SearchPersonsQuery
                    {
                        QueryName = searchTerm,
                        MaxResults = bulkRequest.MaxResultsPerSearch ?? 10,
                        MinSimilarityThreshold = bulkRequest.MinSimilarityThreshold ?? 0.5,
                        IncludeMatchDetails = false // Simplified for bulk operations
                    };

                    var searchResults = await _mediator.Send(query, ct);
                    totalExecutionTime = totalExecutionTime.Add(searchResults.ExecutionTime);

                    bulkResults.Add(new
                    {
                        searchTerm = searchTerm,
                        matchCount = searchResults.Matches.Count,
                        executionTime = searchResults.ExecutionTime.TotalMilliseconds,
                        topMatches = searchResults.Matches.Take(bulkRequest.MaxResultsPerSearch ?? 10).Select(match => new
                        {
                            personId = match.PersonId,
                            externalId = match.ExternalId,
                            fullName = match.FullName,
                            similarityScore = match.SimilarityScore
                        })
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching for term: {SearchTerm}", searchTerm);
                    bulkResults.Add(new
                    {
                        searchTerm = searchTerm,
                        error = ex.Message,
                        matchCount = 0
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                totalSearches = bulkResults.Count,
                totalExecutionTime = totalExecutionTime.TotalMilliseconds,
                averageExecutionTime = totalExecutionTime.TotalMilliseconds / Math.Max(bulkResults.Count, 1),
                results = bulkResults
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk search");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Internal server error",
                message = ex.Message
            }, ct);

            return errorResponse;
        }
    }
}

/// <summary>
/// Advanced search request model
/// </summary>
public class AdvancedSearchRequest
{
    /// <summary>
    /// Gets or sets the name to search for
    /// </summary>
    public string QueryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of results
    /// </summary>
    public int? MaxResults { get; set; }

    /// <summary>
    /// Gets or sets the minimum similarity threshold
    /// </summary>
    public double? MinSimilarityThreshold { get; set; }

    /// <summary>
    /// Gets or sets whether to include trigram similarity
    /// </summary>
    public bool? IncludeTrigramSimilarity { get; set; }

    /// <summary>
    /// Gets or sets whether to expand nicknames
    /// </summary>
    public bool? ExpandNicknames { get; set; }

    /// <summary>
    /// Gets or sets whether to include match details
    /// </summary>
    public bool? IncludeMatchDetails { get; set; }
}

/// <summary>
/// Bulk search request model
/// </summary>
public class BulkSearchRequest
{
    /// <summary>
    /// Gets or sets the search terms
    /// </summary>
    public IEnumerable<string> SearchTerms { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the maximum results per search
    /// </summary>
    public int? MaxResultsPerSearch { get; set; }

    /// <summary>
    /// Gets or sets the minimum similarity threshold
    /// </summary>
    public double? MinSimilarityThreshold { get; set; }
}
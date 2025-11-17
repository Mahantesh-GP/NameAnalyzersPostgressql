using Microsoft.AspNetCore.Mvc;
using PhoneticAnalyzers.NativeApi.Models;
using PhoneticAnalyzers.NativeApi.Services;
using System.Text.Json.Serialization;

namespace PhoneticAnalyzers.NativeApi.Controllers;

/// <summary>
/// Controller for person search using native SQL functions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly INativeDatabaseService _dbService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(INativeDatabaseService dbService, ILogger<SearchController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Searches persons using phonetic and fuzzy matching
    /// </summary>
    /// <param name="queryName">Name to search</param>
    /// <param name="maxResults">Maximum results to return</param>
    /// <param name="minSimilarity">Minimum similarity threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    [HttpGet]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromQuery] string queryName,
        [FromQuery] int maxResults = 50,
        [FromQuery] double minSimilarity = 0.3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryName))
            return BadRequest(new { error = "Query name is required" });

        _logger.LogInformation("Searching for: {QueryName}", queryName);

        var request = new SearchRequest
        {
            QueryName = queryName,
            MaxResults = maxResults,
            MinSimilarity = minSimilarity
        };

        var result = await _dbService.SearchPersonsAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets a person by ID
    /// </summary>
    /// <param name="id">Person ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Person details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SearchResultDto>> GetPerson(
        long id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting person: {PersonId}", id);

        var result = await _dbService.GetPersonByIdAsync(id, cancellationToken);

        if (result == null)
            return NotFound(new { error = "Person not found" });

        return Ok(result);
    }

    /// <summary>
    /// Advanced search with multiple filter criteria
    /// </summary>
    /// <param name="request">Advanced search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    [HttpPost("advanced")]
    public async Task<ActionResult<SearchResponse>> AdvancedSearch(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.QueryName))
            return BadRequest(new { error = "Query name is required" });

        _logger.LogInformation("Advanced search for: {QueryName}", request.QueryName);

        var result = await _dbService.SearchPersonsAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets name suggestions based on prefix
    /// </summary>
    /// <param name="prefix">Search prefix</param>
    /// <param name="maxSuggestions">Maximum suggestions to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of name suggestions</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(SuggestionsResponse), 200)]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string prefix,
        [FromQuery] int maxSuggestions = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return Ok(new SuggestionsResponse { Suggestions = new List<string>() });

        _logger.LogInformation("Getting suggestions for: {Prefix}", prefix);

        var suggestions = await _dbService.GetNameSuggestionsAsync(prefix, maxSuggestions, cancellationToken);

        var response = new SuggestionsResponse { Suggestions = suggestions };
        return Ok(response);
    }

    /// <summary>
    /// Bulk search for multiple names
    /// </summary>
    /// <param name="request">Bulk search request with list of names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk search results</returns>
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkSearchResponse>> BulkSearch(
        [FromBody] BulkSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SearchTerms == null || !request.SearchTerms.Any())
            return BadRequest(new { error = "Search terms are required" });

        _logger.LogInformation("Bulk search for {Count} names", request.SearchTerms.Count);

        var startTime = DateTime.UtcNow;
        var results = new List<BulkSearchResult>();

        foreach (var term in request.SearchTerms)
        {
            var searchRequest = new SearchRequest
            {
                QueryName = term,
                MaxResults = request.MaxResults,
                MinSimilarity = request.MinSimilarity
            };

            var searchResult = await _dbService.SearchPersonsAsync(searchRequest, cancellationToken);

            results.Add(new BulkSearchResult
            {
                SearchTerm = term,
                Results = searchResult.Results,
                ResultCount = searchResult.TotalResults,
                ExecutionTimeMs = searchResult.ExecutionTimeMs
            });
        }

        var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        return Ok(new BulkSearchResponse
        {
            TotalSearches = request.SearchTerms.Count,
            Results = results,
            TotalExecutionTime = totalTime
        });
    }
}

public class BulkSearchRequest
{
    public List<string> SearchTerms { get; set; } = new();
    public int MaxResults { get; set; } = 50;
    public double MinSimilarity { get; set; } = 0.3;
}

public class BulkSearchResponse
{
    public int TotalSearches { get; set; }
    public List<BulkSearchResult> Results { get; set; } = new();
    public double TotalExecutionTime { get; set; }
}

public class BulkSearchResult
{
    public string SearchTerm { get; set; } = string.Empty;
    public List<SearchResultDto> Results { get; set; } = new();
    public int ResultCount { get; set; }
    public double ExecutionTimeMs { get; set; }
}

public class SuggestionsResponse
{
    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();
}

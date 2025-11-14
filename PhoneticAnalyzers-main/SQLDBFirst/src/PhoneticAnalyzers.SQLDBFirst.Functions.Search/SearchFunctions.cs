using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.SQLDBFirst.Application.Queries;
using PhoneticAnalyzers.SQLDBFirst.Application.Queries.Search;

namespace PhoneticAnalyzers.SQLDBFirst.Functions.Search;

/// <summary>
/// Azure Functions for person search operations.
/// Database-First implementation on port 7074.
/// </summary>
public class SearchFunctions
{
    private readonly IMediator _mediator;
    private readonly ILogger<SearchFunctions> _logger;

    public SearchFunctions(IMediator mediator, ILogger<SearchFunctions> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("SearchPersons")]
    public async Task<HttpResponseData> SearchPersons(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "persons/search")] HttpRequestData req)
    {
        _logger.LogInformation("SearchPersons function triggered (Database-First)");

        try
        {
            // Parse query parameters
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var searchName = queryParams["name"];
            var minSimilarityStr = queryParams["minSimilarity"] ?? "0.3";
            var expandNicknamesStr = queryParams["expandNicknames"] ?? "false";

            if (string.IsNullOrWhiteSpace(searchName))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Search name is required" });
                return badRequest;
            }

            if (!double.TryParse(minSimilarityStr, out var minSimilarity))
            {
                minSimilarity = 0.3;
            }

            if (!bool.TryParse(expandNicknamesStr, out var expandNicknames))
            {
                expandNicknames = false;
            }

            var query = new SearchPersonsQuery
            {
                SearchName = searchName,
                MinSimilarity = minSimilarity,
                ExpandNicknames = expandNicknames
            };

            var results = await _mediator.Send(query);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                searchName,
                minSimilarity,
                expandNicknames,
                resultCount = results.Count,
                results
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching persons");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetNameSuggestions")]
    public async Task<HttpResponseData> GetNameSuggestions(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "search/suggestions")] HttpRequestData req)
    {
        _logger.LogInformation("GetNameSuggestions function triggered (Database-First)");

        try
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var prefix = queryParams["prefix"];
            var maxSuggestionsStr = queryParams["maxSuggestions"] ?? "10";

            if (string.IsNullOrWhiteSpace(prefix))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Prefix parameter is required" });
                return badRequest;
            }

            if (!int.TryParse(maxSuggestionsStr, out var maxSuggestions))
            {
                maxSuggestions = 10;
            }

            var query = new GetNameSuggestionsQuery
            {
                Prefix = prefix,
                MaxSuggestions = maxSuggestions
            };

            var result = await _mediator.Send(query);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { suggestions = result.Suggestions });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting name suggestions");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetDatabaseStats")]
    public async Task<HttpResponseData> GetDatabaseStats(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stats")] HttpRequestData req)
    {
        _logger.LogInformation("GetDatabaseStats function triggered (Database-First)");

        try
        {
            var query = new GetDatabaseStatsQuery();
            var stats = await _mediator.Send(query);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(stats);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database stats");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("Health")]
    public HttpResponseData Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check (Database-First Search)");
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteString("Database-First Search Functions - Healthy");
        return response;
    }

    [Function("DiagnosticsInfo")]
    public HttpResponseData DiagnosticsInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "diagnostics")] HttpRequestData req)
    {
        _logger.LogInformation("Diagnostics info requested (Database-First)");
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        var diagnostics = new
        {
            service = "PhoneticAnalyzers Database-First Search",
            version = "1.0.0",
            approach = "Database-First",
            port = 7074,
            database = "phonetic_db_dbfirst",
            endpoints = new[]
            {
                "GET /api/persons/search?name={name}&minSimilarity={score}&expandNicknames={bool}",
                "GET /api/search/suggestions?prefix={prefix}&maxSuggestions={count}",
                "GET /api/stats - Database statistics",
                "GET /api/health - Health check",
                "GET /api/diagnostics - This diagnostics endpoint"
            },
            algorithms = new[]
            {
                "Exact Match (1.0 confidence)",
                "Token Contains (0.95 confidence)",
                "Nickname Expansion (0.93 confidence)",
                "Primary Double Metaphone (0.9 confidence)",
                "Alternate Double Metaphone (0.85 confidence)",
                "Beider-Morse (0.8 confidence)",
                "Trigram Similarity (variable confidence)"
            },
            features = new[]
            {
                "PostgreSQL pg_trgm fuzzy matching",
                "Bidirectional nickname lookup",
                "Multi-algorithm phonetic search",
                "Match type classification",
                "Confidence scoring"
            }
        };

        response.WriteAsJsonAsync(diagnostics);
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
        _logger.LogInformation("Advanced search requested (Database-First)");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
            var searchRequest = JsonSerializer.Deserialize<AdvancedSearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (searchRequest == null || string.IsNullOrWhiteSpace(searchRequest.QueryName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body or missing query name" }, ct);
                return badResponse;
            }

            var query = new SearchPersonsQuery
            {
                SearchName = searchRequest.QueryName,
                MinSimilarity = searchRequest.MinSimilarityThreshold ?? 0.3,
                ExpandNicknames = searchRequest.ExpandNicknames ?? true
            };

            var startTime = DateTime.UtcNow;
            var results = await _mediator.Send(query, ct);
            var executionTime = DateTime.UtcNow - startTime;

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                query = searchRequest.QueryName,
                parameters = new
                {
                    maxResults = searchRequest.MaxResults ?? 50,
                    minSimilarityThreshold = query.MinSimilarity,
                    expandNicknames = query.ExpandNicknames,
                    countyId = searchRequest.CountyId,
                    recordType = searchRequest.RecordType
                },
                totalMatches = results.Count,
                executionTime = executionTime.TotalMilliseconds,
                results = results.Select(match => new
                {
                    personId = match.PersonId,
                    externalId = match.ExternalId,
                    fullName = match.FullName,
                    county = match.County,
                    matchType = match.MatchType,
                    similarityScore = match.MatchScore,
                    matchMetadata = new
                    {
                        matchedField = match.MatchedField,
                        matchedValue = match.MatchedValue,
                        matchScore = match.MatchScore,
                        matchType = match.MatchType
                    }
                }),
                warnings = new List<string>()
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
    /// Get list of available counties for filtering
    /// </summary>
    [Function("GetCounties")]
    public async Task<HttpResponseData> GetCounties(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "counties")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Get counties requested (Database-First)");

        try
        {
            // Return hardcoded list of common counties
            var counties = new List<object>
            {
                new { CountyId = 1, County = "LA", CountyName = "Los Angeles County" },
                new { CountyId = 2, County = "OR", CountyName = "Orange County" },
                new { CountyId = 3, County = "SD", CountyName = "San Diego County" },
                new { CountyId = 4, County = "SB", CountyName = "San Bernardino County" },
                new { CountyId = 5, County = "RV", CountyName = "Riverside County" },
                new { CountyId = 6, County = "VC", CountyName = "Ventura County" },
                new { CountyId = 7, County = "SC", CountyName = "Santa Clara County" },
                new { CountyId = 8, County = "AL", CountyName = "Alameda County" },
                new { CountyId = 9, County = "SF", CountyName = "San Francisco County" },
                new { CountyId = 10, County = "SM", CountyName = "San Mateo County" },
                new { CountyId = 11, County = "CC", CountyName = "Contra Costa County" },
                new { CountyId = 12, County = "SAC", CountyName = "Sacramento County" },
                new { CountyId = 13, County = "FR", CountyName = "Fresno County" },
                new { CountyId = 14, County = "KR", CountyName = "Kern County" },
                new { CountyId = 15, County = "TU", CountyName = "Tulare County" },
                new { CountyId = 16, County = "SJ", CountyName = "San Joaquin County" },
                new { CountyId = 17, County = "ST", CountyName = "Stanislaus County" },
                new { CountyId = 18, County = "MR", CountyName = "Merced County" },
                new { CountyId = 19, County = "KI", CountyName = "Kings County" },
                new { CountyId = 20, County = "MD", CountyName = "Madera County" }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(counties, ct);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting counties");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, ct);
            return errorResponse;
        }
    }
}

/// <summary>
/// Request model for advanced search
/// </summary>
public class AdvancedSearchRequest
{
    public string QueryName { get; set; } = string.Empty;
    public int? MaxResults { get; set; }
    public double? MinSimilarityThreshold { get; set; }
    public bool? IncludeTrigramSimilarity { get; set; }
    public bool? ExpandNicknames { get; set; }
    public bool? IncludeMatchDetails { get; set; }
    public int? CountyId { get; set; }
    public string? RecordType { get; set; }
}

using Newtonsoft.Json;
// using Newtonsoft.Json.Linq; // Removed unused using directive
using System.Net.Sockets;
using System.Net;

namespace PhoneticAnalyzers.Web.Services;

/// <summary>
/// API client service for PhoneticAnalyzers backend
/// </summary>
public class PhoneticAnalyzersApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PhoneticAnalyzersApiService> _logger;

    public PhoneticAnalyzersApiService(IHttpClientFactory httpClientFactory, ILogger<PhoneticAnalyzersApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// JSON serializer settings for camelCase API responses
    /// </summary>
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Represents a validation problem details payload returned by API.
    /// </summary>
    public sealed class ValidationProblemDetails
    {
        [JsonProperty("title")] public string? Title { get; set; }
        [JsonProperty("status")] public int? Status { get; set; }
        [JsonProperty("detail")] public string? Detail { get; set; }
        [JsonProperty("instance")] public string? Instance { get; set; }
        [JsonProperty("errors")] public List<ValidationErrorItem>? Errors { get; set; }
    }

    /// <summary>
    /// Single validation error item.
    /// </summary>
    public sealed class ValidationErrorItem
    {
        [JsonProperty("field")] public string? Field { get; set; }
        [JsonProperty("message")] public string? Message { get; set; }
    }

    /// <summary>
    /// Exception thrown when a validation error response is received.
    /// </summary>
    public sealed class ValidationApiException : Exception
    {
        public ValidationProblemDetails? Problem { get; }
        public HttpStatusCode StatusCode => (HttpStatusCode)(Problem?.Status ?? 400);
        public ValidationApiException(ValidationProblemDetails? problem)
            : base(problem?.Detail ?? problem?.Title ?? "Validation error")
        {
            Problem = problem;
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    public async Task<HealthCheckResult?> HealthCheckAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("IngestionApi");
            var response = await httpClient.GetAsync("/api/health");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HealthCheckResult>(content, JsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform health check");
            throw;
        }
    }

    /// <summary>
    /// Add a single person
    /// </summary>
    public async Task<PersonIngestResult?> AddPersonAsync(PersonData personData)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("IngestionApi");
            var json = JsonConvert.SerializeObject(personData, JsonSettings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("/api/ingest", content);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var validation = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson, JsonSettings);
                    throw new ValidationApiException(validation);
                }
                response.EnsureSuccessStatusCode();
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PersonIngestResult>(responseContent, JsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add person: {ExternalId}", personData.ExternalId);
            throw;
        }
    }

    /// <summary>
    /// Search for persons using phonetic matching. Attempts advanced search first (SearchApi). Falls back to simple search (IngestionApi) if the advanced service is unavailable.
    /// </summary>
    public async Task<SearchResult?> SearchPersonsAsync(string name, int maxResults = 10) 
    {
        try
        {
            // Advanced search request body
            var searchRequest = new
            {
                queryName = name,
                maxResults = maxResults,
                minSimilarityThreshold = 0.3,
                includeTrigramSimilarity = true,
                expandNicknames = true,
                includeMatchDetails = true
            };

            var httpClient = _httpClientFactory.CreateClient("SearchApi");
            var json = JsonConvert.SerializeObject(searchRequest, JsonSettings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try 
            {
                // Try advanced search endpoint first
                var response = await httpClient.PostAsync("/api/search/advanced", content);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var errorJson = await response.Content.ReadAsStringAsync();
                        var validation = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson, JsonSettings);
                        throw new ValidationApiException(validation);
                    }
                    response.EnsureSuccessStatusCode();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonConvert.DeserializeObject<AdvancedSearchResponse>(responseContent, JsonSettings);
                if (searchResponse == null)
                {
                    throw new InvalidOperationException("Advanced search returned empty response");
                }
                return MapToSearchResult(searchResponse, name, maxResults);
            }
            catch (HttpRequestException hre) when (IsConnectionRefused(hre))
            {
                // Fallback to simple search on ingestion API
                _logger.LogWarning(hre, "Advanced search API unreachable. Falling back to simple search.");
                var ingestionClient = _httpClientFactory.CreateClient("IngestionApi");
                var fallbackResponse = await ingestionClient.GetAsync($"/api/search?name={Uri.EscapeDataString(name)}&maxResults={maxResults}");
                if (!fallbackResponse.IsSuccessStatusCode)
                {
                    if (fallbackResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var errorJson = await fallbackResponse.Content.ReadAsStringAsync();
                        var validation = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson, JsonSettings);
                        throw new ValidationApiException(validation);
                    }
                    fallbackResponse.EnsureSuccessStatusCode();
                }

                var fallbackContent = await fallbackResponse.Content.ReadAsStringAsync();
                var fallbackSearchResponse = JsonConvert.DeserializeObject<SimpleSearchResponse>(fallbackContent, JsonSettings);
                if (fallbackSearchResponse == null)
                {
                    throw new InvalidOperationException("Simple search returned empty response");
                }
                return MapToSearchResult(fallbackSearchResponse, name, maxResults);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search persons with name: {Name}", name);
            throw;
        }
    }

    private static bool IsConnectionRefused(HttpRequestException hre)
    {
        if (hre.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
            return true;
        return hre.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase);
    }

    private static SearchResult MapToSearchResult(AdvancedSearchResponse searchResponse, string originalQuery, int maxResults)
    {
        var results = (searchResponse.Results ?? new List<AdvancedResultItem>())
            .Select(r => new PersonSearchResult
            {
                PersonId = r.PersonId,
                ExternalId = r.ExternalId ?? string.Empty,
                FullName = r.FullName ?? string.Empty,
                NormalizedName = r.NormalizedName ?? string.Empty,
                SimilarityScore = r.SimilarityScore,
                MatchType = r.MatchType ?? string.Empty,
                PhoneticCodes = r.PhoneticCodes != null ? new PersonPhoneticCodes
                {
                    DoubleMetaphone = new DoubleMetaphoneCodes
                    {
                        Primary = r.PhoneticCodes.DoubleMetaphone?.Primary,
                        Alternate = r.PhoneticCodes.DoubleMetaphone?.Alternate
                    },
                    BeiderMorse = r.PhoneticCodes.BeiderMorse ?? new List<string>()
                } : null
            })
            .ToList();

        return new SearchResult
        {
            Query = searchResponse.Query ?? originalQuery,
            TotalResults = searchResponse.TotalMatches ?? results.Count,
            MaxResults = maxResults,
            ExecutionTime = searchResponse.ExecutionTime ?? 0.0,
            Results = results
        };
    }

    private static SearchResult MapToSearchResult(SimpleSearchResponse searchResponse, string originalQuery, int maxResults)
    {
        var results = (searchResponse.Results ?? new List<SimpleResultItem>())
            .Select(r => new PersonSearchResult
            {
                PersonId = r.PersonId,
                ExternalId = r.ExternalId ?? string.Empty,
                FullName = r.FullName ?? string.Empty,
                NormalizedName = r.NormalizedName ?? string.Empty,
                SimilarityScore = r.SimilarityScore,
                MatchType = r.MatchType ?? string.Empty,
                PhoneticCodes = null
            })
            .ToList();

        return new SearchResult
        {
            Query = searchResponse.Query ?? originalQuery,
            TotalResults = searchResponse.TotalResults ?? results.Count,
            MaxResults = searchResponse.MaxResults ?? maxResults,
            ExecutionTime = searchResponse.ExecutionTime ?? 0.0,
            Results = results
        };
    }

    /// <summary>
    /// Batch add multiple persons
    /// </summary>
    public async Task<BatchIngestResult?> AddPersonsBatchAsync(List<PersonData> persons)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("IngestionApi");
            var batchData = new { persons };
            var json = JsonConvert.SerializeObject(batchData, JsonSettings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("/api/ingest/batch", content);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var validation = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson, JsonSettings);
                    throw new ValidationApiException(validation);
                }
                response.EnsureSuccessStatusCode();
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<BatchIngestResult>(responseContent, JsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch add {Count} persons", persons.Count);
            throw;
        }
    }

    /// <summary>
    /// Get person by ID
    /// </summary>
    public async Task<PersonDetails?> GetPersonAsync(string id)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("IngestionApi");
            var response = await httpClient.GetAsync($"/api/person/{Uri.EscapeDataString(id)}");
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var validation = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson, JsonSettings);
                    throw new ValidationApiException(validation);
                }
                response.EnsureSuccessStatusCode();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PersonDetails>(content, JsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get person: {Id}", id);
            throw;
        }
    }
}

// Data Transfer Objects
public class PersonData
{
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool ExpandNicknames { get; set; } = true;
}

public class PersonIngestResult
{
    [JsonProperty("personId")]
    public long PersonId { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonProperty("wasCreated")]
    public bool WasCreated { get; set; }
    
    [JsonProperty("phoneticCodes")]
    public PhoneticCodes PhoneticCodes { get; set; } = new();
    
    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = new();
}

public class PhoneticCodes
{
    [JsonProperty("primary")]
    public string? Primary { get; set; }
    
    [JsonProperty("alternate")]
    public string? Alternate { get; set; }
    
    [JsonProperty("beiderMorseCodes")]
    public List<string> BeiderMorseCodes { get; set; } = new();
}

public class SearchResult
{
    public string Query { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public int MaxResults { get; set; }
    public double ExecutionTime { get; set; }
    public List<PersonSearchResult> Results { get; set; } = new();
}

public class PersonSearchResult
{
    public long PersonId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public PersonPhoneticCodes? PhoneticCodes { get; set; }
}

public class PersonPhoneticCodes
{
    public DoubleMetaphoneCodes DoubleMetaphone { get; set; } = new();
    public List<string> BeiderMorse { get; set; } = new();
}

public class DoubleMetaphoneCodes
{
    public string? Primary { get; set; }
    public string? Alternate { get; set; }
}

public class BatchIngestResult
{
    public int TotalProcessed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BatchResultItem> Results { get; set; } = new();
    public List<BatchResultItem> Errors { get; set; } = new();
}

public class BatchResultItem
{
    public string ExternalId { get; set; } = string.Empty;
    public long? PersonId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
}

public class PersonDetails
{
    public long PersonId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public PersonPhoneticCodes? PhoneticCodes { get; set; }
}

// Strongly-typed Advanced search DTOs (Search Functions response)
public sealed class AdvancedSearchResponse
{
    [JsonProperty("query")] public string? Query { get; set; }
    [JsonProperty("totalMatches")] public int? TotalMatches { get; set; }
    [JsonProperty("executionTime")] public double? ExecutionTime { get; set; }
    [JsonProperty("results")] public List<AdvancedResultItem>? Results { get; set; }
    [JsonProperty("phoneticCodes")] public AdvancedPhoneticCodes? PhoneticCodes { get; set; }
    [JsonProperty("warnings")] public List<string>? Warnings { get; set; }
}

public sealed class AdvancedResultItem
{
    [JsonProperty("personId")] public long PersonId { get; set; }
    [JsonProperty("externalId")] public string? ExternalId { get; set; }
    [JsonProperty("fullName")] public string? FullName { get; set; }
    [JsonProperty("normalizedName")] public string? NormalizedName { get; set; }
    [JsonProperty("similarityScore")] public double SimilarityScore { get; set; }
    [JsonProperty("matchType")] public string? MatchType { get; set; }
    [JsonProperty("matchMetadata")] public object? MatchMetadata { get; set; }
    [JsonProperty("phoneticCodes")] public AdvancedItemPhoneticCodes? PhoneticCodes { get; set; }
}

public sealed class AdvancedItemPhoneticCodes
{
    [JsonProperty("doubleMetaphone")] public AdvancedDoubleMetaphone? DoubleMetaphone { get; set; }
    [JsonProperty("beiderMorse")] public List<string>? BeiderMorse { get; set; }
}

public sealed class AdvancedPhoneticCodes
{
    [JsonProperty("doubleMetaphone")] public AdvancedDoubleMetaphone? DoubleMetaphone { get; set; }
    [JsonProperty("beiderMorse")] public List<string>? BeiderMorse { get; set; }
    [JsonProperty("nicknameVariations")] public List<string>? NicknameVariations { get; set; }
}

public sealed class AdvancedDoubleMetaphone
{
    [JsonProperty("primary")] public string? Primary { get; set; }
    [JsonProperty("alternate")] public string? Alternate { get; set; }
}

// Strongly-typed Simple search DTOs (fallback Ingestion Functions response)
public sealed class SimpleSearchResponse
{
    [JsonProperty("query")] public string? Query { get; set; }
    [JsonProperty("totalResults")] public int? TotalResults { get; set; }
    [JsonProperty("maxResults")] public int? MaxResults { get; set; }
    [JsonProperty("executionTime")] public double? ExecutionTime { get; set; }
    [JsonProperty("results")] public List<SimpleResultItem>? Results { get; set; }
}

public sealed class SimpleResultItem
{
    [JsonProperty("personId")] public long PersonId { get; set; }
    [JsonProperty("externalId")] public string? ExternalId { get; set; }
    [JsonProperty("fullName")] public string? FullName { get; set; }
    [JsonProperty("normalizedName")] public string? NormalizedName { get; set; }
    [JsonProperty("similarityScore")] public double SimilarityScore { get; set; }
    [JsonProperty("matchType")] public string? MatchType { get; set; }
}
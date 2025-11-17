using System.Text.Json.Serialization;

namespace PhoneticAnalyzers.NativeApi.Models;

/// <summary>
/// Request model for search
/// </summary>
public class SearchRequest
{
    [JsonPropertyName("queryName")]
    public string QueryName { get; set; } = string.Empty;
    
    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; } = 50;
    
    [JsonPropertyName("minSimilarityThreshold")]
    public double MinSimilarity { get; set; } = 0.3;
    
    [JsonPropertyName("countyId")]
    public int? CountyId { get; set; }
    
    [JsonPropertyName("recordType")]
    public string? RecordType { get; set; }
}

/// <summary>
/// Individual search result
/// </summary>
public class SearchResultDto
{
    public long PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MatchType { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string MatchedField { get; set; } = string.Empty;
    public string MatchedValue { get; set; } = string.Empty;
    public string? County { get; set; }
    public string? CountyName { get; set; }
    public string? Flag { get; set; }
    public System.Text.Json.JsonElement? MatchMetadata { get; set; }
}

/// <summary>
/// Search response with metadata
/// </summary>
public class SearchResponse
{
    [JsonPropertyName("query")]
    public string QueryName { get; set; } = string.Empty;
    
    [JsonPropertyName("results")]
    public List<SearchResultDto> Results { get; set; } = new();
    
    [JsonPropertyName("totalMatches")]
    public int TotalResults { get; set; }
    
    [JsonPropertyName("executionTime")]
    public double ExecutionTimeMs { get; set; }
}

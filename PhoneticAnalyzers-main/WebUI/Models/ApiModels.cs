using System.Text.Json.Serialization;

namespace PhoneticAnalyzers.WebUI.Models;

/// <summary>
/// Advanced search request model
/// </summary>
public class AdvancedSearchRequest
{
    [JsonPropertyName("queryName")]
    public string QueryName { get; set; } = string.Empty;

    [JsonPropertyName("maxResults")]
    public int? MaxResults { get; set; } = 50;

    [JsonPropertyName("minSimilarityThreshold")]
    public double? MinSimilarityThreshold { get; set; } = 0.3;

    [JsonPropertyName("includeTrigramSimilarity")]
    public bool? IncludeTrigramSimilarity { get; set; } = true;

    [JsonPropertyName("expandNicknames")]
    public bool? ExpandNicknames { get; set; } = true;

    [JsonPropertyName("includeMatchDetails")]
    public bool? IncludeMatchDetails { get; set; } = true;

    [JsonPropertyName("countyId")]
    public int? CountyId { get; set; }

    [JsonPropertyName("recordType")]
    public string? RecordType { get; set; }
}

/// <summary>
/// Advanced search response model
/// </summary>
public class AdvancedSearchResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public SearchParameters? Parameters { get; set; }

    [JsonPropertyName("totalMatches")]
    public int TotalMatches { get; set; }

    [JsonPropertyName("executionTime")]
    public double ExecutionTime { get; set; }

    [JsonPropertyName("phoneticCodes")]
    public PhoneticCodes? PhoneticCodes { get; set; }

    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }
}

public class SearchParameters
{
    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("minSimilarityThreshold")]
    public double MinSimilarityThreshold { get; set; }

    [JsonPropertyName("includeTrigramSimilarity")]
    public bool IncludeTrigramSimilarity { get; set; }

    [JsonPropertyName("expandNicknames")]
    public bool ExpandNicknames { get; set; }

    [JsonPropertyName("countyId")]
    public int? CountyId { get; set; }

    [JsonPropertyName("recordType")]
    public string? RecordType { get; set; }
}

public class PhoneticCodes
{
    [JsonPropertyName("doubleMetaphone")]
    public DoubleMetaphoneResult? DoubleMetaphone { get; set; }

    [JsonPropertyName("beiderMorse")]
    public List<string>? BeiderMorse { get; set; }

    [JsonPropertyName("nicknameVariations")]
    public List<string>? NicknameVariations { get; set; }
}

public class DoubleMetaphoneResult
{
    [JsonPropertyName("primary")]
    public string? Primary { get; set; }

    [JsonPropertyName("alternate")]
    public string? Alternate { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("personId")]
    public Guid PersonId { get; set; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("normalizedName")]
    public string? NormalizedName { get; set; }

    [JsonPropertyName("county")]
    public string County { get; set; } = string.Empty;

    [JsonPropertyName("countyId")]
    public int CountyId { get; set; }

    [JsonPropertyName("countyName")]
    public string CountyName { get; set; } = string.Empty;

    [JsonPropertyName("flag")]
    public string Flag { get; set; } = string.Empty;

    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; set; }

    [JsonPropertyName("matchType")]
    public string MatchType { get; set; } = string.Empty;

    [JsonPropertyName("matchMetadata")]
    public Dictionary<string, object>? MatchMetadata { get; set; }

    [JsonPropertyName("phoneticCodes")]
    public PhoneticCodes? PhoneticCodes { get; set; }
}

/// <summary>
/// Bulk search request model
/// </summary>
public class BulkSearchRequest
{
    [JsonPropertyName("searchTerms")]
    public List<string> SearchTerms { get; set; } = new();

    [JsonPropertyName("maxResultsPerSearch")]
    public int? MaxResultsPerSearch { get; set; } = 10;

    [JsonPropertyName("minSimilarityThreshold")]
    public double? MinSimilarityThreshold { get; set; } = 0.5;
}

/// <summary>
/// Bulk search response model
/// </summary>
public class BulkSearchResponse
{
    [JsonPropertyName("totalSearches")]
    public int TotalSearches { get; set; }

    [JsonPropertyName("totalExecutionTime")]
    public double TotalExecutionTime { get; set; }

    [JsonPropertyName("averageExecutionTime")]
    public double AverageExecutionTime { get; set; }

    [JsonPropertyName("results")]
    public List<BulkSearchResult> Results { get; set; } = new();
}

public class BulkSearchResult
{
    [JsonPropertyName("searchTerm")]
    public string SearchTerm { get; set; } = string.Empty;

    [JsonPropertyName("matchCount")]
    public int MatchCount { get; set; }

    [JsonPropertyName("executionTime")]
    public double ExecutionTime { get; set; }

    [JsonPropertyName("topMatches")]
    public List<BulkMatch> TopMatches { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class BulkMatch
{
    [JsonPropertyName("personId")]
    public Guid PersonId { get; set; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("county")]
    public string County { get; set; } = string.Empty;

    [JsonPropertyName("countyId")]
    public int CountyId { get; set; }

    [JsonPropertyName("countyName")]
    public string CountyName { get; set; } = string.Empty;

    [JsonPropertyName("flag")]
    public string Flag { get; set; } = string.Empty;

    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; set; }
}

/// <summary>
/// County information model
/// </summary>
public class CountyInfo
{
    [JsonPropertyName("countyId")]
    public int CountyId { get; set; }

    [JsonPropertyName("county")]
    public string County { get; set; } = string.Empty;

    [JsonPropertyName("countyName")]
    public string CountyName { get; set; } = string.Empty;
}

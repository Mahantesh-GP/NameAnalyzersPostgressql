namespace PhoneticAnalyzers.Domain.Services;

/// <summary>
/// Service for managing curated nickname datasets with confidence scoring and fuzzy matching
/// </summary>
public interface ICuratedNicknameService
{
    /// <summary>
    /// Get all known nicknames for a given name with confidence scores
    /// </summary>
    /// <param name="name">The base name to find nicknames for</param>
    /// <param name="culture">Optional culture for culture-specific nicknames</param>
    /// <param name="minConfidence">Minimum confidence score (0.0-1.0)</param>
    /// <returns>Collection of nicknames with confidence scores</returns>
    Task<IEnumerable<NicknameMatch>> GetNicknamesAsync(string name, string? culture = null, double minConfidence = 0.5);

    /// <summary>
    /// Get the base name(s) for a given nickname
    /// </summary>
    /// <param name="nickname">The nickname to find base names for</param>
    /// <param name="culture">Optional culture for culture-specific matching</param>
    /// <param name="minConfidence">Minimum confidence score (0.0-1.0)</param>
    /// <returns>Collection of base names with confidence scores</returns>
    Task<IEnumerable<NicknameMatch>> GetBaseNamesAsync(string nickname, string? culture = null, double minConfidence = 0.5);

    /// <summary>
    /// Find fuzzy nickname matches using edit distance and phonetic similarity
    /// </summary>
    /// <param name="name">The name to find fuzzy matches for</param>
    /// <param name="maxEditDistance">Maximum edit distance allowed</param>
    /// <param name="includePhonetic">Include phonetic similarity matching</param>
    /// <param name="culture">Optional culture for culture-specific matching</param>
    /// <returns>Collection of fuzzy matches with confidence scores</returns>
    Task<IEnumerable<NicknameMatch>> GetFuzzyMatchesAsync(string name, int maxEditDistance = 2, bool includePhonetic = true, string? culture = null);

    /// <summary>
    /// Add or update a nickname mapping with confidence score
    /// </summary>
    /// <param name="baseName">The base name</param>
    /// <param name="nickname">The nickname</param>
    /// <param name="confidence">Confidence score (0.0-1.0)</param>
    /// <param name="culture">Optional culture identifier</param>
    /// <param name="source">Source of the nickname data</param>
    /// <returns>Success indicator</returns>
    Task<bool> AddNicknameMappingAsync(string baseName, string nickname, double confidence, string? culture = null, string? source = null);

    /// <summary>
    /// Import nickname dataset from various sources
    /// </summary>
    /// <param name="dataSource">Source of the data (e.g., 'uscensus', 'wikipedia', 'custom')</param>
    /// <param name="filePath">Path to the data file</param>
    /// <param name="format">Data format (e.g., 'csv', 'json', 'xml')</param>
    /// <param name="culture">Culture identifier for the dataset</param>
    /// <returns>Import statistics</returns>
    Task<NicknameImportResult> ImportNicknameDatasetAsync(string dataSource, string filePath, string format, string? culture = null);

    /// <summary>
    /// Get nickname statistics and metrics
    /// </summary>
    /// <returns>Statistical information about the nickname dataset</returns>
    Task<NicknameStatistics> GetStatisticsAsync();

    /// <summary>
    /// Validate and update confidence scores based on usage patterns
    /// </summary>
    /// <param name="learningData">Usage data for machine learning</param>
    /// <returns>Number of updated mappings</returns>
    Task<int> UpdateConfidenceScoresAsync(IEnumerable<NicknameUsageData> learningData);
}

/// <summary>
/// Represents a nickname match with confidence score
/// </summary>
public record NicknameMatch(
    string Name,
    string Nickname,
    double Confidence,
    string? Culture = null,
    string? Source = null,
    NicknameMatchType MatchType = NicknameMatchType.Exact
);

/// <summary>
/// Result of nickname dataset import operation
/// </summary>
public record NicknameImportResult(
    int TotalRecords,
    int SuccessfulImports,
    int SkippedDuplicates,
    int Errors,
    TimeSpan Duration,
    string[] ErrorMessages
);

/// <summary>
/// Statistical information about nickname dataset
/// </summary>
public record NicknameStatistics(
    int TotalMappings,
    int UniqueBases,
    int UniqueNicknames,
    double AverageConfidence,
    Dictionary<string, int> CultureDistribution,
    Dictionary<string, int> SourceDistribution,
    DateTime LastUpdated
);

/// <summary>
/// Usage data for machine learning confidence score updates
/// </summary>
public record NicknameUsageData(
    string BaseName,
    string Nickname,
    int UsageCount,
    double UserConfidence,
    string Context
);

/// <summary>
/// Type of nickname match
/// </summary>
public enum NicknameMatchType
{
    /// <summary>
    /// Exact string match
    /// </summary>
    Exact,
    
    /// <summary>
    /// Fuzzy match based on edit distance
    /// </summary>
    Fuzzy,
    
    /// <summary>
    /// Phonetic similarity match
    /// </summary>
    Phonetic,
    
    /// <summary>
    /// Cultural or linguistic variant
    /// </summary>
    Cultural
}
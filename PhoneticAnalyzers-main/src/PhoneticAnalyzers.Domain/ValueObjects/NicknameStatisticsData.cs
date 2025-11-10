namespace PhoneticAnalyzers.Domain.ValueObjects;

/// <summary>
/// Statistical data about nickname mappings
/// </summary>
public record NicknameStatisticsData
{
    /// <summary>
    /// Total number of nickname mappings
    /// </summary>
    public int TotalMappings { get; init; }

    /// <summary>
    /// Number of unique base names
    /// </summary>
    public int UniqueBases { get; init; }

    /// <summary>
    /// Number of unique nicknames
    /// </summary>
    public int UniqueNicknames { get; init; }

    /// <summary>
    /// Average confidence score across all mappings
    /// </summary>
    public double AverageConfidence { get; init; }

    /// <summary>
    /// Distribution of mappings by culture
    /// </summary>
    public Dictionary<string, int> CultureDistribution { get; init; } = new();

    /// <summary>
    /// Distribution of mappings by source
    /// </summary>
    public Dictionary<string, int> SourceDistribution { get; init; } = new();

    /// <summary>
    /// When the statistics were last updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}
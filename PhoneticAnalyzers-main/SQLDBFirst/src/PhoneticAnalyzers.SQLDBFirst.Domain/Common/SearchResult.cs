using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

namespace PhoneticAnalyzers.SQLDBFirst.Domain.Common;

/// <summary>
/// Search result with match metadata.
/// Contains the person entity and details about how it was matched.
/// </summary>
public class SearchResult
{
    public Person Person { get; set; } = null!;
    public MatchType MatchType { get; set; }
    public double MatchScore { get; set; }
    public string? MatchedField { get; set; }
    public string? MatchedValue { get; set; }
}

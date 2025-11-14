namespace PhoneticAnalyzers.SQLDBFirst.Application.DTOs;

/// <summary>
/// Data Transfer Object for person search results.
/// </summary>
public class PersonSearchResultDto
{
    public long PersonId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? County { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public string? MatchedField { get; set; }
    public string? MatchedValue { get; set; }
}

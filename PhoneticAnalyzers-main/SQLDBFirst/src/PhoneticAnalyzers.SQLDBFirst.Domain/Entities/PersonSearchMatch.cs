namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// Represents a person search result with similarity score
/// </summary>
public class PersonSearchMatch
{
    public Person Person { get; set; } = null!;
    public double SimilarityScore { get; set; }
}

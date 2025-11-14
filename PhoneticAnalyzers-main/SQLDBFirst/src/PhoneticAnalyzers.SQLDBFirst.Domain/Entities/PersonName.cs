namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// PersonName entity for Database-First approach.
/// Stores individual name tokens for advanced searching.
/// This will be REPLACED by scaffolded model from database.
/// </summary>
public class PersonName
{
    public long PersonNameId { get; set; }
    public long PersonId { get; set; }
    public string NameToken { get; set; } = string.Empty;
    public int TokenPosition { get; set; }
    public string? PrimaryMetaphone { get; set; }
    public string? AlternateMetaphone { get; set; }
    public DateTime CreatedUtc { get; set; }

    // Navigation property
    public virtual Person Person { get; set; } = null!;
}

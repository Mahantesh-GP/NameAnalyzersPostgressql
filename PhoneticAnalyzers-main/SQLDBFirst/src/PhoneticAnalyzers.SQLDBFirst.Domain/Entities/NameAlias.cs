namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// NameAlias entity for Database-First approach.
/// Alternative name spellings and cultural variations.
/// This will be REPLACED by scaffolded model from database.
/// </summary>
public class NameAlias
{
    public long NameAliasId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string? Culture { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

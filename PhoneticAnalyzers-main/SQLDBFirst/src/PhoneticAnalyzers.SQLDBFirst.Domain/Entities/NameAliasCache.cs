namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// NameAliasCache entity for Database-First approach.
/// Performance cache for frequently used name variations.
/// This will be REPLACED by scaffolded model from database.
/// </summary>
public class NameAliasCache
{
    public long NameAliasCacheId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AllAliases { get; set; } = string.Empty;
    public DateTime CachedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

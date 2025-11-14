namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// NicknameMap entity for Database-First approach.
/// Maps canonical names to nicknames (e.g., William -> Bill, Bob -> Robert).
/// This will be REPLACED by scaffolded model from database.
/// </summary>
public class NicknameMap
{
    public long NicknameMapId { get; set; }
    public string CanonicalName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Locale { get; set; } = "en-US";
    public decimal Confidence { get; set; } = 0.95m;
    public bool IsBidirectional { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

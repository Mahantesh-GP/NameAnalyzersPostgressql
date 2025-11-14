namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// Person entity for Database-First approach.
/// This will be REPLACED by scaffolded model from database.
/// Temporary placeholder until scaffold-models.ps1 is run.
/// </summary>
public class Person
{
    public long PersonId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? PrimaryMetaphone { get; set; }
    public string? AlternateMetaphone { get; set; }
    public string? County { get; set; }
    public char Flag { get; set; } = 'I';
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    // Navigation properties
    public virtual ICollection<PersonName> PersonNames { get; set; } = new List<PersonName>();
    public virtual ICollection<PersonBm> PersonBms { get; set; } = new List<PersonBm>();
}

namespace PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

/// <summary>
/// PersonBm entity for Database-First approach.
/// Stores Beider-Morse phonetic encodings.
/// This will be REPLACED by scaffolded model from database.
/// </summary>
public class PersonBm
{
    public long PersonBmId { get; set; }
    public long PersonId { get; set; }
    public string BmCode { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    // Navigation property
    public virtual Person Person { get; set; } = null!;
}

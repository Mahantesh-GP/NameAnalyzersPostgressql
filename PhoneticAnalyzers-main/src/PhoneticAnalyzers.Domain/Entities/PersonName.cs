using PhoneticAnalyzers.Domain.Common;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Entities;

/// <summary>
/// Represents a canonical person name for multilingual search
/// </summary>
public sealed class PersonName : AggregateRoot
{
    private readonly List<NameAlias> _aliases = [];

    /// <summary>
    /// Gets the canonical name (normalized form)
    /// </summary>
    public string CanonicalName { get; private set; }

    /// <summary>
    /// Gets the locale hint for this name
    /// </summary>
    public Locale LocaleHint { get; private set; }

    /// <summary>
    /// Gets the script hint for this name
    /// </summary>
    public Script ScriptHint { get; private set; }

    /// <summary>
    /// Gets the normalized name for searching
    /// </summary>
    public NormalizedName NormalizedName { get; private set; }

    /// <summary>
    /// Gets the Double Metaphone code for phonetic matching
    /// </summary>
    public PhoneticCode? DoubleMetaphoneCode { get; private set; }

    /// <summary>
    /// Gets the Beider-Morse code for phonetic matching
    /// </summary>
    public PhoneticCode? BeiderMorseCode { get; private set; }



    /// <summary>
    /// Gets the last enrichment timestamp (when LLM variants were generated)
    /// </summary>
    public DateTime? LastEnrichmentUtc { get; private set; }

    /// <summary>
    /// Gets the read-only collection of aliases
    /// </summary>
    public IReadOnlyList<NameAlias> Aliases => _aliases.AsReadOnly();

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private PersonName()
    {
        CanonicalName = string.Empty;
        LocaleHint = null!;
        ScriptHint = null!;
        NormalizedName = null!;
    }

    /// <summary>
    /// Creates a new PersonName
    /// </summary>
    /// <param name="canonicalName">The canonical name</param>
    /// <param name="localeHint">The locale hint</param>
    /// <param name="scriptHint">The script hint</param>
    /// <param name="doubleMetaphoneCode">The Double Metaphone code</param>
    /// <param name="beiderMorseCode">The Beider-Morse code</param>
    /// <returns>A new PersonName instance</returns>
    public static PersonName Create(
        string canonicalName,
        Locale localeHint,
        Script scriptHint,
        PhoneticCode? doubleMetaphoneCode = null,
        PhoneticCode? beiderMorseCode = null)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
            throw new ArgumentException("Canonical name cannot be null or whitespace.", nameof(canonicalName));

        ArgumentNullException.ThrowIfNull(localeHint);
        ArgumentNullException.ThrowIfNull(scriptHint);

        var personName = new PersonName
        {
            CanonicalName = canonicalName.Trim(),
            LocaleHint = localeHint,
            ScriptHint = scriptHint,
            NormalizedName = NormalizedName.Create(canonicalName),
            DoubleMetaphoneCode = doubleMetaphoneCode,
            BeiderMorseCode = beiderMorseCode
        };

        personName.SetCreatedTimestamp();
        personName.AddDomainEvent(new PersonNameCreatedDomainEvent(personName.Id, personName.CanonicalName));

        return personName;
    }

    /// <summary>
    /// Adds an alias to this person name
    /// </summary>
    /// <param name="alias">The alias to add</param>
    public void AddAlias(NameAlias alias)
    {
        ArgumentNullException.ThrowIfNull(alias);

        if (_aliases.Any(a => a.Alias == alias.Alias && a.Locale.Code == alias.Locale.Code))
            return; // Alias already exists

        _aliases.Add(alias);
        MarkAsUpdated();

        AddDomainEvent(new NameAliasAddedDomainEvent(Id, alias.Alias, alias.AliasType, alias.Locale.Code));
    }

    /// <summary>
    /// Marks this person name as enriched by LLM
    /// </summary>
    public void MarkAsEnriched()
    {
        LastEnrichmentUtc = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Checks if this person name needs enrichment
    /// </summary>
    /// <param name="enrichmentIntervalDays">The interval in days between enrichments</param>
    /// <returns>True if enrichment is needed</returns>
    public bool NeedsEnrichment(int enrichmentIntervalDays = 30)
    {
        if (LastEnrichmentUtc == null)
            return true;

        return DateTime.UtcNow - LastEnrichmentUtc.Value > TimeSpan.FromDays(enrichmentIntervalDays);
    }
}

/// <summary>
/// Domain event raised when a person name is created
/// </summary>
/// <param name="PersonNameId">The person name ID</param>
/// <param name="CanonicalName">The canonical name</param>
public sealed record PersonNameCreatedDomainEvent(long PersonNameId, string CanonicalName) : DomainEvent;

/// <summary>
/// Domain event raised when a name alias is added
/// </summary>
/// <param name="PersonNameId">The person name ID</param>
/// <param name="Alias">The alias that was added</param>
/// <param name="AliasType">The type of alias</param>
/// <param name="Locale">The locale of the alias</param>
public sealed record NameAliasAddedDomainEvent(long PersonNameId, string Alias, Enums.AliasType AliasType, string Locale) : DomainEvent;
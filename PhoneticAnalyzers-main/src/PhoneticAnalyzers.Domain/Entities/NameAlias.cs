using PhoneticAnalyzers.Domain.Common;
using PhoneticAnalyzers.Domain.Enums;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Entities;

/// <summary>
/// Represents a name alias for multilingual search
/// </summary>
public sealed class NameAlias : BaseEntity
{
    /// <summary>
    /// Gets the person name ID this alias belongs to
    /// </summary>
    public long PersonNameId { get; private set; }

    /// <summary>
    /// Gets the alias text
    /// </summary>
    public string Alias { get; private set; }

    /// <summary>
    /// Gets the type of alias
    /// </summary>
    public AliasType AliasType { get; private set; }

    /// <summary>
    /// Gets the locale for this alias
    /// </summary>
    public Locale Locale { get; private set; }

    /// <summary>
    /// Gets the script for this alias
    /// </summary>
    public Script Script { get; private set; }

    /// <summary>
    /// Gets the source of this alias
    /// </summary>
    public AliasSource Source { get; private set; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0)
    /// </summary>
    public decimal Confidence { get; private set; }

    /// <summary>
    /// Gets the normalized alias for searching
    /// </summary>
    public NormalizedName NormalizedAlias { get; private set; }

    /// <summary>
    /// Gets the Double Metaphone code for phonetic matching
    /// </summary>
    public PhoneticCode? DoubleMetaphoneCode { get; private set; }

    /// <summary>
    /// Gets the Beider-Morse code for phonetic matching
    /// </summary>
    public PhoneticCode? BeiderMorseCode { get; private set; }



    /// <summary>
    /// Navigation property to PersonName
    /// </summary>
    public PersonName? PersonName { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private NameAlias()
    {
        Alias = string.Empty;
        Locale = null!;
        Script = null!;
        NormalizedAlias = null!;
    }

    /// <summary>
    /// Creates a new name alias
    /// </summary>
    /// <param name="personNameId">The person name ID</param>
    /// <param name="alias">The alias text</param>
    /// <param name="aliasType">The type of alias</param>
    /// <param name="locale">The locale</param>
    /// <param name="script">The script</param>
    /// <param name="source">The source of the alias</param>
    /// <param name="confidence">The confidence score</param>
    /// <param name="doubleMetaphoneCode">The Double Metaphone code</param>
    /// <param name="beiderMorseCode">The Beider-Morse code</param>
    /// <returns>A new NameAlias instance</returns>
    public static NameAlias Create(
        long personNameId,
        string alias,
        AliasType aliasType,
        Locale locale,
        Script script,
        AliasSource source,
        decimal confidence,
        PhoneticCode? doubleMetaphoneCode = null,
        PhoneticCode? beiderMorseCode = null)
    {
        if (personNameId <= 0)
            throw new ArgumentException("Person name ID must be greater than zero.", nameof(personNameId));

        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("Alias cannot be null or whitespace.", nameof(alias));

        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(script);

        if (confidence < 0.0m || confidence > 1.0m)
            throw new ArgumentException("Confidence must be between 0.0 and 1.0.", nameof(confidence));

        var nameAlias = new NameAlias
        {
            PersonNameId = personNameId,
            Alias = alias.Trim(),
            AliasType = aliasType,
            Locale = locale,
            Script = script,
            Source = source,
            Confidence = confidence,
            NormalizedAlias = NormalizedName.Create(alias),
            DoubleMetaphoneCode = doubleMetaphoneCode,
            BeiderMorseCode = beiderMorseCode
        };

        nameAlias.SetCreatedTimestamp();
        return nameAlias;
    }

    /// <summary>
    /// Updates the confidence score
    /// </summary>
    /// <param name="newConfidence">The new confidence score</param>
    public void UpdateConfidence(decimal newConfidence)
    {
        if (newConfidence < 0.0m || newConfidence > 1.0m)
            throw new ArgumentException("Confidence must be between 0.0 and 1.0.", nameof(newConfidence));

        Confidence = newConfidence;
    }
}
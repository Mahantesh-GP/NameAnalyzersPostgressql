using PhoneticAnalyzers.Domain.Common;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Entities;

/// <summary>
/// Represents a person with their phonetic encodings for name matching
/// </summary>
public sealed class Person : AggregateRoot
{
    private readonly List<BeiderMorseVariant> _beiderMorseVariants = [];

    /// <summary>
    /// Gets the external identifier for this person
    /// </summary>
    public ExternalId ExternalId { get; private set; }

    /// <summary>
    /// Gets the original full name as provided
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Gets the normalized name for consistent processing
    /// </summary>
    public NormalizedName NormalizedName { get; private set; }

    /// <summary>
    /// Gets the primary Double Metaphone code
    /// </summary>
    public PhoneticCode? PrimaryDoubleMetaphone { get; private set; }

    /// <summary>
    /// Gets the alternate Double Metaphone code (if available)
    /// </summary>
    public PhoneticCode? AlternateDoubleMetaphone { get; private set; }

    /// <summary>
    /// Gets the first letter of the normalized name for indexing
    /// </summary>
    public char FirstLetter => NormalizedName.FirstLetter;

    /// <summary>
    /// Gets the hash value for partitioning (derived from normalized name)
    /// </summary>
    public int PartitionHash { get; private set; }

    /// <summary>
    /// Gets the Beider-Morse phonetic variants
    /// </summary>
    public IReadOnlyList<BeiderMorseVariant> BeiderMorseVariants => _beiderMorseVariants.AsReadOnly();

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private Person()
    {
        ExternalId = null!;
        FullName = string.Empty;
        NormalizedName = null!;
    }

    /// <summary>
    /// Creates a new Person instance
    /// </summary>
    /// <param name="externalId">The external identifier</param>
    /// <param name="fullName">The full name</param>
    /// <param name="primaryDoubleMetaphone">The primary Double Metaphone code</param>
    /// <param name="alternateDoubleMetaphone">The alternate Double Metaphone code</param>
    /// <param name="beiderMorseCodes">The Beider-Morse phonetic codes</param>
    /// <returns>A new Person instance</returns>
    public static Person Create(
        ExternalId externalId,
        string fullName,
        PhoneticCode? primaryDoubleMetaphone = null,
        PhoneticCode? alternateDoubleMetaphone = null,
        IEnumerable<PhoneticCode>? beiderMorseCodes = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty", nameof(fullName));

        var normalizedName = NormalizedName.Create(fullName);
        
        var person = new Person
        {
            ExternalId = externalId,
            FullName = fullName.Trim(),
            NormalizedName = normalizedName,
            PrimaryDoubleMetaphone = primaryDoubleMetaphone,
            AlternateDoubleMetaphone = alternateDoubleMetaphone,
            PartitionHash = CalculatePartitionHash(normalizedName)
        };

        person.SetCreatedTimestamp();

        if (beiderMorseCodes != null)
        {
            person.SetBeiderMorseCodes(beiderMorseCodes);
        }

        person.AddDomainEvent(new PersonCreatedDomainEvent(person.Id, person.ExternalId, person.FullName));

        return person;
    }

    /// <summary>
    /// Updates the person's information
    /// </summary>
    /// <param name="fullName">The updated full name</param>
    /// <param name="primaryDoubleMetaphone">The updated primary Double Metaphone code</param>
    /// <param name="alternateDoubleMetaphone">The updated alternate Double Metaphone code</param>
    /// <param name="beiderMorseCodes">The updated Beider-Morse phonetic codes</param>
    public void Update(
        string fullName,
        PhoneticCode? primaryDoubleMetaphone = null,
        PhoneticCode? alternateDoubleMetaphone = null,
        IEnumerable<PhoneticCode>? beiderMorseCodes = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty", nameof(fullName));

        var wasUpdated = false;

        if (FullName != fullName.Trim())
        {
            FullName = fullName.Trim();
            NormalizedName = NormalizedName.Create(fullName);
            PartitionHash = CalculatePartitionHash(NormalizedName);
            wasUpdated = true;
        }

        if (!Equals(PrimaryDoubleMetaphone, primaryDoubleMetaphone))
        {
            PrimaryDoubleMetaphone = primaryDoubleMetaphone;
            wasUpdated = true;
        }

        if (!Equals(AlternateDoubleMetaphone, alternateDoubleMetaphone))
        {
            AlternateDoubleMetaphone = alternateDoubleMetaphone;
            wasUpdated = true;
        }

        if (beiderMorseCodes != null)
        {
            SetBeiderMorseCodes(beiderMorseCodes);
            wasUpdated = true;
        }

        if (wasUpdated)
        {
            MarkAsUpdated();
            AddDomainEvent(new PersonUpdatedDomainEvent(Id, ExternalId, FullName));
        }
    }

    /// <summary>
    /// Sets the Beider-Morse phonetic codes
    /// </summary>
    /// <param name="beiderMorseCodes">The phonetic codes to set</param>
    private void SetBeiderMorseCodes(IEnumerable<PhoneticCode> beiderMorseCodes)
    {
        _beiderMorseVariants.Clear();
        
        var variants = beiderMorseCodes
            .Where(code => code.AlgorithmType == PhoneticAlgorithmType.BeiderMorse)
            .Take(16) // Limit to 16 variants as per the plan
            .Select(code => BeiderMorseVariant.Create(code))
            .ToList();

        _beiderMorseVariants.AddRange(variants);
    }

    /// <summary>
    /// Calculates the partition hash for the given normalized name
    /// </summary>
    /// <param name="normalizedName">The normalized name</param>
    /// <returns>The partition hash value</returns>
    private static int CalculatePartitionHash(NormalizedName normalizedName)
    {
        return Math.Abs(normalizedName.Value.GetHashCode() % 64);
    }
}

/// <summary>
/// Represents a Beider-Morse phonetic variant
/// </summary>
public sealed class BeiderMorseVariant : BaseEntity
{
    /// <summary>
    /// Gets the person ID this variant belongs to
    /// </summary>
    public long PersonId { get; private set; }

    /// <summary>
    /// Gets the Beider-Morse phonetic code
    /// </summary>
    public PhoneticCode BeiderMorseCode { get; private set; }

    /// <summary>
    /// Gets the first letter of the code for indexing
    /// </summary>
    public char FirstLetter => BeiderMorseCode.Value.Length > 0 ? BeiderMorseCode.Value[0] : '\0';

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private BeiderMorseVariant()
    {
        BeiderMorseCode = null!;
    }

    /// <summary>
    /// Creates a new BeiderMorseVariant instance
    /// </summary>
    /// <param name="beiderMorseCode">The Beider-Morse phonetic code</param>
    /// <returns>A new BeiderMorseVariant instance</returns>
    public static BeiderMorseVariant Create(PhoneticCode beiderMorseCode)
    {
        if (beiderMorseCode.AlgorithmType != PhoneticAlgorithmType.BeiderMorse)
            throw new ArgumentException("Code must be a Beider-Morse phonetic code", nameof(beiderMorseCode));

        return new BeiderMorseVariant
        {
            BeiderMorseCode = beiderMorseCode
        };
    }

    /// <summary>
    /// Sets the person ID (used by the Person aggregate)
    /// </summary>
    /// <param name="personId">The person ID</param>
    internal void SetPersonId(long personId)
    {
        PersonId = personId;
    }
}

/// <summary>
/// Domain event raised when a person is created
/// </summary>
/// <param name="PersonId">The person's ID</param>
/// <param name="ExternalId">The person's external ID</param>
/// <param name="FullName">The person's full name</param>
public sealed record PersonCreatedDomainEvent(long PersonId, ExternalId ExternalId, string FullName) : DomainEvent;

/// <summary>
/// Domain event raised when a person is updated
/// </summary>
/// <param name="PersonId">The person's ID</param>
/// <param name="ExternalId">The person's external ID</param>
/// <param name="FullName">The person's updated full name</param>
public sealed record PersonUpdatedDomainEvent(long PersonId, ExternalId ExternalId, string FullName) : DomainEvent;
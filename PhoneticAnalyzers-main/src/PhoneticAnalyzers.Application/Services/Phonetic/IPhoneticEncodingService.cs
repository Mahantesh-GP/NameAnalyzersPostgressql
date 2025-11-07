using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Application.Services.Phonetic;

/// <summary>
/// Interface for phonetic encoding services
/// </summary>
public interface IPhoneticEncoder
{
    /// <summary>
    /// Gets the phonetic algorithm type this encoder handles
    /// </summary>
    PhoneticAlgorithmType AlgorithmType { get; }

    /// <summary>
    /// Encodes the given name using the phonetic algorithm
    /// </summary>
    /// <param name="normalizedName">The normalized name to encode</param>
    /// <returns>The phonetic codes generated for the name</returns>
    IReadOnlyList<PhoneticCode> Encode(NormalizedName normalizedName);

    /// <summary>
    /// Gets whether this encoder supports multiple variants per name
    /// </summary>
    bool SupportsMultipleVariants { get; }
}

/// <summary>
/// Factory interface for creating phonetic encoders
/// </summary>
public interface IPhoneticEncoderFactory
{
    /// <summary>
    /// Gets a phonetic encoder for the specified algorithm type
    /// </summary>
    /// <param name="algorithmType">The algorithm type</param>
    /// <returns>The phonetic encoder</returns>
    IPhoneticEncoder GetEncoder(PhoneticAlgorithmType algorithmType);

    /// <summary>
    /// Gets all available phonetic encoders
    /// </summary>
    /// <returns>A collection of all available encoders</returns>
    IReadOnlyList<IPhoneticEncoder> GetAllEncoders();
}

/// <summary>
/// Service for comprehensive phonetic encoding operations
/// </summary>
public interface IPhoneticEncodingService
{
    /// <summary>
    /// Encodes a name using all available phonetic algorithms
    /// </summary>
    /// <param name="normalizedName">The normalized name to encode</param>
    /// <returns>The phonetic encoding result</returns>
    Task<PhoneticEncodingResult> EncodeAsync(NormalizedName normalizedName);

    /// <summary>
    /// Encodes a name using a specific phonetic algorithm
    /// </summary>
    /// <param name="normalizedName">The normalized name to encode</param>
    /// <param name="algorithmType">The specific algorithm to use</param>
    /// <returns>The phonetic codes for the specified algorithm</returns>
    Task<IReadOnlyList<PhoneticCode>> EncodeAsync(NormalizedName normalizedName, PhoneticAlgorithmType algorithmType);

    /// <summary>
    /// Batch encodes multiple names using all available algorithms
    /// </summary>
    /// <param name="normalizedNames">The normalized names to encode</param>
    /// <returns>The phonetic encoding results</returns>
    Task<IReadOnlyList<PhoneticEncodingResult>> EncodeBatchAsync(IEnumerable<NormalizedName> normalizedNames);
}

/// <summary>
/// Result of phonetic encoding operations
/// </summary>
public sealed class PhoneticEncodingResult
{
    /// <summary>
    /// Gets the original normalized name that was encoded
    /// </summary>
    public NormalizedName OriginalName { get; }

    /// <summary>
    /// Gets the primary Double Metaphone code
    /// </summary>
    public PhoneticCode? PrimaryDoubleMetaphone { get; }

    /// <summary>
    /// Gets the alternate Double Metaphone code
    /// </summary>
    public PhoneticCode? AlternateDoubleMetaphone { get; }

    /// <summary>
    /// Gets the Beider-Morse phonetic codes
    /// </summary>
    public IReadOnlyList<PhoneticCode> BeiderMorseCodes { get; }

    /// <summary>
    /// Gets all phonetic codes grouped by algorithm type
    /// </summary>
    public IReadOnlyDictionary<PhoneticAlgorithmType, IReadOnlyList<PhoneticCode>> AllCodes { get; }

    /// <summary>
    /// Initializes a new instance of the PhoneticEncodingResult class
    /// </summary>
    /// <param name="originalName">The original normalized name</param>
    /// <param name="primaryDoubleMetaphone">The primary Double Metaphone code</param>
    /// <param name="alternateDoubleMetaphone">The alternate Double Metaphone code</param>
    /// <param name="beiderMorseCodes">The Beider-Morse codes</param>
    public PhoneticEncodingResult(
        NormalizedName originalName,
        PhoneticCode? primaryDoubleMetaphone = null,
        PhoneticCode? alternateDoubleMetaphone = null,
        IReadOnlyList<PhoneticCode>? beiderMorseCodes = null)
    {
        OriginalName = originalName;
        PrimaryDoubleMetaphone = primaryDoubleMetaphone;
        AlternateDoubleMetaphone = alternateDoubleMetaphone;
        BeiderMorseCodes = beiderMorseCodes ?? [];

        var allCodes = new Dictionary<PhoneticAlgorithmType, IReadOnlyList<PhoneticCode>>();

        var doubleMetaphoneCodes = new List<PhoneticCode>();
        if (primaryDoubleMetaphone != null)
            doubleMetaphoneCodes.Add(primaryDoubleMetaphone);
        if (alternateDoubleMetaphone != null)
            doubleMetaphoneCodes.Add(alternateDoubleMetaphone);

        if (doubleMetaphoneCodes.Count > 0)
            allCodes[PhoneticAlgorithmType.DoubleMetaphone] = doubleMetaphoneCodes;

        if (BeiderMorseCodes.Count > 0)
            allCodes[PhoneticAlgorithmType.BeiderMorse] = BeiderMorseCodes;

        AllCodes = allCodes;
    }

    /// <summary>
    /// Gets whether any phonetic codes were generated
    /// </summary>
    public bool HasAnyCodes => PrimaryDoubleMetaphone != null || AlternateDoubleMetaphone != null || BeiderMorseCodes.Count > 0;
}
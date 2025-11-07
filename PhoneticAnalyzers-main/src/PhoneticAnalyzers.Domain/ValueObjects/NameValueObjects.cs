using System.Text.RegularExpressions;

namespace PhoneticAnalyzers.Domain.ValueObjects;

/// <summary>
/// Value object representing a normalized name with validation rules
/// </summary>
public sealed record NormalizedName
{
    private static readonly Regex ValidationRegex = new(@"^[\p{L}\p{Nd} '\-]{1,200}$", RegexOptions.Compiled);

    /// <summary>
    /// Gets the normalized name value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the first character of the normalized name for indexing purposes
    /// </summary>
    public char FirstLetter => Value.Length > 0 ? Value[0] : '\0';

    /// <summary>
    /// Initializes a new instance of the NormalizedName class
    /// </summary>
    /// <param name="value">The name value to normalize and validate</param>
    /// <exception cref="ArgumentException">Thrown when the name is invalid</exception>
    private NormalizedName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be null or empty", nameof(value));

        Value = NormalizeInternal(value);

        if (!ValidationRegex.IsMatch(Value))
            throw new ArgumentException($"Invalid name format: {value}", nameof(value));
    }

    /// <summary>
    /// Creates a new NormalizedName from the provided value
    /// </summary>
    /// <param name="value">The name value to normalize</param>
    /// <returns>A new NormalizedName instance</returns>
    public static NormalizedName Create(string value) => new(value);

    /// <summary>
    /// Attempts to create a NormalizedName from the provided value
    /// </summary>
    /// <param name="value">The name value to normalize</param>
    /// <param name="normalizedName">The created normalized name if successful</param>
    /// <returns>True if the name was successfully created, false otherwise</returns>
    public static bool TryCreate(string value, out NormalizedName? normalizedName)
    {
        try
        {
            normalizedName = new NormalizedName(value);
            return true;
        }
        catch
        {
            normalizedName = null;
            return false;
        }
    }

    /// <summary>
    /// Normalizes the input string according to business rules
    /// </summary>
    /// <param name="input">The input string to normalize</param>
    /// <returns>The normalized string</returns>
    private static string NormalizeInternal(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Convert to uppercase for consistency
        var normalized = input.ToUpperInvariant();

        // Replace multiple whitespace characters with single space
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Remove invalid characters, keeping only letters, digits, spaces, hyphens, and apostrophes
        normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd} '\-]", " ");

        // Clean up multiple spaces again after character removal
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    /// <summary>
    /// Implicitly converts a NormalizedName to a string
    /// </summary>
    /// <param name="normalizedName">The normalized name</param>
    public static implicit operator string(NormalizedName normalizedName) => normalizedName.Value;

    /// <summary>
    /// Returns the string representation of this normalized name
    /// </summary>
    /// <returns>The normalized name value</returns>
    public override string ToString() => Value;
}

/// <summary>
/// Value object representing a phonetic code with validation
/// </summary>
public sealed record PhoneticCode
{
    /// <summary>
    /// Gets the phonetic code value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the type of phonetic encoding used
    /// </summary>
    public PhoneticAlgorithmType AlgorithmType { get; }

    /// <summary>
    /// Gets whether this is a primary or alternate code (for algorithms that support both)
    /// </summary>
    public bool IsPrimary { get; }

    /// <summary>
    /// Initializes a new instance of the PhoneticCode class
    /// </summary>
    /// <param name="value">The phonetic code value</param>
    /// <param name="algorithmType">The type of phonetic algorithm used</param>
    /// <param name="isPrimary">Whether this is a primary code</param>
    /// <exception cref="ArgumentException">Thrown when the code value is invalid</exception>
    private PhoneticCode(string value, PhoneticAlgorithmType algorithmType, bool isPrimary = true)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phonetic code cannot be null or empty", nameof(value));

        if (value.Length > 128)
            throw new ArgumentException("Phonetic code cannot exceed 128 characters", nameof(value));

        Value = value.ToUpperInvariant();
        AlgorithmType = algorithmType;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Creates a new PhoneticCode
    /// </summary>
    /// <param name="value">The phonetic code value</param>
    /// <param name="algorithmType">The type of phonetic algorithm used</param>
    /// <param name="isPrimary">Whether this is a primary code</param>
    /// <returns>A new PhoneticCode instance</returns>
    public static PhoneticCode Create(string value, PhoneticAlgorithmType algorithmType, bool isPrimary = true)
        => new(value, algorithmType, isPrimary);

    /// <summary>
    /// Implicitly converts a PhoneticCode to a string
    /// </summary>
    /// <param name="phoneticCode">The phonetic code</param>
    public static implicit operator string(PhoneticCode phoneticCode) => phoneticCode.Value;

    /// <summary>
    /// Returns the string representation of this phonetic code
    /// </summary>
    /// <returns>The phonetic code value</returns>
    public override string ToString() => Value;
}

/// <summary>
/// Enumeration of supported phonetic algorithms
/// </summary>
public enum PhoneticAlgorithmType
{
    /// <summary>
    /// Double Metaphone algorithm
    /// </summary>
    DoubleMetaphone,

    /// <summary>
    /// Beider-Morse phonetic matching algorithm
    /// </summary>
    BeiderMorse
}

/// <summary>
/// Value object representing an external identifier
/// </summary>
public sealed record ExternalId
{
    /// <summary>
    /// Gets the external identifier value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the ExternalId class
    /// </summary>
    /// <param name="value">The external identifier value</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid</exception>
    private ExternalId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("External ID cannot be null or empty", nameof(value));

        if (value.Length > 64)
            throw new ArgumentException("External ID cannot exceed 64 characters", nameof(value));

        Value = value.Trim();
    }

    /// <summary>
    /// Creates a new ExternalId from the provided value
    /// </summary>
    /// <param name="value">The external identifier value</param>
    /// <returns>A new ExternalId instance</returns>
    public static ExternalId Create(string value) => new(value);

    /// <summary>
    /// Implicitly converts an ExternalId to a string
    /// </summary>
    /// <param name="externalId">The external ID</param>
    public static implicit operator string(ExternalId externalId) => externalId.Value;

    /// <summary>
    /// Returns the string representation of this external ID
    /// </summary>
    /// <returns>The external ID value</returns>
    public override string ToString() => Value;
}
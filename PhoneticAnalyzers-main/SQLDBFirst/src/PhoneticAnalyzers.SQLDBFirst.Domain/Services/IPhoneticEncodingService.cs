namespace PhoneticAnalyzers.SQLDBFirst.Domain.Services;

/// <summary>
/// Service interface for phonetic encoding operations.
/// Generates Double Metaphone and Beider-Morse codes for fuzzy name matching.
/// </summary>
public interface IPhoneticEncodingService
{
    /// <summary>
    /// Generate primary and alternate Double Metaphone codes.
    /// </summary>
    (string? primary, string? alternate) GetDoubleMetaphone(string text);

    /// <summary>
    /// Generate Beider-Morse phonetic codes (may return multiple).
    /// </summary>
    IEnumerable<string> GetBeiderMorseCodes(string text);

    /// <summary>
    /// Normalize name for consistent matching (uppercase, trim, remove extra spaces).
    /// </summary>
    string NormalizeName(string name);
}

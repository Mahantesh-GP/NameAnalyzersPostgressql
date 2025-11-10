namespace PhoneticAnalyzers.Domain.Services;

/// <summary>
/// Interface for text normalization services used in multilingual name processing
/// </summary>
public interface ITextNormalizationService
{
    /// <summary>
    /// Normalizes text for consistent processing across different locales
    /// </summary>
    /// <param name="text">The text to normalize</param>
    /// <param name="locale">The locale context for normalization</param>
    /// <returns>Normalized text suitable for comparison and indexing</returns>
    string Normalize(string text, string locale = "en");

    /// <summary>
    /// Normalizes text specifically for phonetic encoding
    /// </summary>
    /// <param name="text">The text to normalize</param>
    /// <param name="locale">The locale context for normalization</param>
    /// <returns>Text optimized for phonetic algorithm processing</returns>
    string NormalizeForPhonetic(string text, string locale = "en");

    /// <summary>
    /// Removes diacritics and accents from text
    /// </summary>
    /// <param name="text">The text to process</param>
    /// <returns>Text with diacritics removed</returns>
    string RemoveDiacritics(string text);

    /// <summary>
    /// Transliterates non-Latin script text to Latin characters
    /// </summary>
    /// <param name="text">The text to transliterate</param>
    /// <param name="sourceScript">The source script (e.g., "Cyrl", "Arab", "Hant")</param>
    /// <returns>Transliterated text in Latin script</returns>
    string Transliterate(string text, string sourceScript);

    /// <summary>
    /// Tokenizes text into normalized components
    /// </summary>
    /// <param name="text">The text to tokenize</param>
    /// <param name="locale">The locale context for tokenization</param>
    /// <returns>Array of normalized tokens</returns>
    string[] Tokenize(string text, string locale = "en");

    /// <summary>
    /// Standardizes whitespace and punctuation in text
    /// </summary>
    /// <param name="text">The text to standardize</param>
    /// <returns>Text with standardized whitespace and punctuation</returns>
    string StandardizeText(string text);
}
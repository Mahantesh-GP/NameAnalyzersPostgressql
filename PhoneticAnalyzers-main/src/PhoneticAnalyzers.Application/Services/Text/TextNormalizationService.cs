using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PhoneticAnalyzers.Domain.Services;

namespace PhoneticAnalyzers.Application.Services.Text;

/// <summary>
/// Implementation of text normalization services for multilingual name processing
/// </summary>
public sealed class TextNormalizationService : ITextNormalizationService
{
    private static readonly Dictionary<string, CultureInfo> CultureMap = new()
    {
        { "en", new CultureInfo("en-US") },
        { "es", new CultureInfo("es-ES") },
        { "fr", new CultureInfo("fr-FR") },
        { "de", new CultureInfo("de-DE") },
        { "it", new CultureInfo("it-IT") },
        { "pt", new CultureInfo("pt-PT") },
        { "hi", new CultureInfo("hi-IN") },
        { "ar", new CultureInfo("ar-SA") },
        { "zh", new CultureInfo("zh-CN") },
        { "ja", new CultureInfo("ja-JP") },
        { "ko", new CultureInfo("ko-KR") },
        { "ru", new CultureInfo("ru-RU") },
        { "tr", new CultureInfo("tr-TR") },
        { "pl", new CultureInfo("pl-PL") },
        { "nl", new CultureInfo("nl-NL") },
        { "sv", new CultureInfo("sv-SE") },
        { "no", new CultureInfo("no-NO") },
        { "da", new CultureInfo("da-DK") },
        { "fi", new CultureInfo("fi-FI") }
    };

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex PunctuationRegex = new(@"[^\p{L}\p{N}\s]", RegexOptions.Compiled);
    private static readonly Regex NonAlphanumericRegex = new(@"[^\p{L}\p{N}]", RegexOptions.Compiled);

    // Transliteration mappings for common non-Latin scripts
    private static readonly Dictionary<string, Dictionary<char, string>> TransliterationMaps = new()
    {
        ["Cyrl"] = new Dictionary<char, string>
        {
            ['А'] = "A", ['Б'] = "B", ['В'] = "V", ['Г'] = "G", ['Д'] = "D", ['Е'] = "E", ['Ё'] = "E",
            ['Ж'] = "Zh", ['З'] = "Z", ['И'] = "I", ['Й'] = "Y", ['К'] = "K", ['Л'] = "L", ['М'] = "M",
            ['Н'] = "N", ['О'] = "O", ['П'] = "P", ['Р'] = "R", ['С'] = "S", ['Т'] = "T", ['У'] = "U",
            ['Ф'] = "F", ['Х'] = "Kh", ['Ц'] = "Ts", ['Ч'] = "Ch", ['Ш'] = "Sh", ['Щ'] = "Shch",
            ['Ъ'] = "", ['Ы'] = "Y", ['Ь'] = "", ['Э'] = "E", ['Ю'] = "Yu", ['Я'] = "Ya",
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
            ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
            ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
            ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch",
            ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
        },
        ["Arab"] = new Dictionary<char, string>
        {
            ['ا'] = "a", ['ب'] = "b", ['ت'] = "t", ['ث'] = "th", ['ج'] = "j", ['ح'] = "h", ['خ'] = "kh",
            ['د'] = "d", ['ذ'] = "dh", ['ر'] = "r", ['ز'] = "z", ['س'] = "s", ['ش'] = "sh", ['ص'] = "s",
            ['ض'] = "d", ['ط'] = "t", ['ظ'] = "z", ['ع'] = "", ['غ'] = "gh", ['ف'] = "f", ['ق'] = "q",
            ['ك'] = "k", ['ل'] = "l", ['م'] = "m", ['ن'] = "n", ['ه'] = "h", ['و'] = "w", ['ي'] = "y"
        }
    };

    /// <summary>
    /// Normalizes text for consistent processing across different locales
    /// </summary>
    public string Normalize(string text, string locale = "en")
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Get culture for locale-specific processing
        var culture = GetCultureForLocale(locale);

        // Step 1: Normalize Unicode (NFD - Canonical Decomposition)
        var normalized = text.Normalize(NormalizationForm.FormD);

        // Step 2: Convert to lowercase using locale-specific rules
        normalized = normalized.ToLower(culture);

        // Step 3: Remove diacritics
        normalized = RemoveDiacritics(normalized);

        // Step 4: Standardize whitespace and punctuation
        normalized = StandardizeText(normalized);

        // Step 5: Trim and normalize whitespace
        normalized = WhitespaceRegex.Replace(normalized.Trim(), " ");

        return normalized;
    }

    /// <summary>
    /// Normalizes text specifically for phonetic encoding
    /// </summary>
    public string NormalizeForPhonetic(string text, string locale = "en")
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Start with general normalization
        var normalized = Normalize(text, locale);

        // Additional phonetic-specific processing
        // Remove all non-alphabetic characters for phonetic algorithms
        normalized = NonAlphanumericRegex.Replace(normalized, "");

        // Ensure we have clean alphabetic text
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return normalized;
    }

    /// <summary>
    /// Removes diacritics and accents from text
    /// </summary>
    public string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalizedText = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var character in normalizedText)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            
            // Skip combining diacritical marks
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(character);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Transliterates non-Latin script text to Latin characters
    /// </summary>
    public string Transliterate(string text, string sourceScript)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (!TransliterationMaps.TryGetValue(sourceScript, out var transliterationMap))
        {
            // Fallback: remove non-Latin characters and return what we can
            return RemoveNonLatinCharacters(text);
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var character in text)
        {
            if (transliterationMap.TryGetValue(character, out var transliteration))
            {
                stringBuilder.Append(transliteration);
            }
            else if (IsLatinCharacter(character) || char.IsWhiteSpace(character))
            {
                // Keep Latin characters and whitespace as-is
                stringBuilder.Append(character);
            }
            // Skip characters we can't transliterate
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Tokenizes text into normalized components
    /// </summary>
    public string[] Tokenize(string text, string locale = "en")
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        // Normalize the text first
        var normalized = Normalize(text, locale);

        // Split on whitespace and filter out empty tokens
        var tokens = normalized
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();

        return tokens;
    }

    /// <summary>
    /// Standardizes whitespace and punctuation in text
    /// </summary>
    public string StandardizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Replace various punctuation with spaces for name processing
        var standardized = PunctuationRegex.Replace(text, " ");

        // Normalize whitespace
        standardized = WhitespaceRegex.Replace(standardized, " ");

        return standardized.Trim();
    }

    private static CultureInfo GetCultureForLocale(string locale)
    {
        return CultureMap.TryGetValue(locale.ToLowerInvariant(), out var culture) 
            ? culture 
            : CultureInfo.InvariantCulture;
    }

    private static bool IsLatinCharacter(char character)
    {
        // Check if character is in Latin script ranges
        var code = (int)character;
        return (code >= 0x0041 && code <= 0x005A) || // A-Z
               (code >= 0x0061 && code <= 0x007A) || // a-z
               (code >= 0x00C0 && code <= 0x024F) || // Latin Extended A & B
               (code >= 0x1E00 && code <= 0x1EFF);   // Latin Extended Additional
    }

    private static string RemoveNonLatinCharacters(string text)
    {
        var stringBuilder = new StringBuilder();
        
        foreach (var character in text)
        {
            if (IsLatinCharacter(character) || char.IsWhiteSpace(character) || char.IsDigit(character))
            {
                stringBuilder.Append(character);
            }
        }

        return stringBuilder.ToString();
    }
}
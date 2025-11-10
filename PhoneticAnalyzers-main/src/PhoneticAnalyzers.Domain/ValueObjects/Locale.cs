using PhoneticAnalyzers.Domain.Common;

namespace PhoneticAnalyzers.Domain.ValueObjects;

/// <summary>
/// Represents a locale for multilingual name variants (e.g., "en", "es", "hi")
/// </summary>
public sealed class Locale : ValueObject
{
    private static readonly HashSet<string> ValidLocales = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "es", "fr", "de", "it", "pt", "hi", "ar", "zh", "ja", "ko", "ru", "tr", "pl", "nl", "sv", "no", "da", "fi"
    };

    /// <summary>
    /// Gets the locale code
    /// </summary>
    public string Code { get; }

    private Locale(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Creates a new locale from a string code
    /// </summary>
    /// <param name="code">The locale code</param>
    /// <returns>A new Locale instance</returns>
    /// <exception cref="ArgumentException">Thrown when the locale code is invalid</exception>
    public static Locale Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Locale code cannot be null or whitespace.", nameof(code));

        var normalizedCode = code.Trim().ToLowerInvariant();

        // Handle locale codes like "en-US" by taking only the language part
        if (normalizedCode.Contains('-'))
            normalizedCode = normalizedCode.Split('-')[0];

        if (!ValidLocales.Contains(normalizedCode))
            throw new ArgumentException($"Invalid locale code: {code}. Supported locales: {string.Join(", ", ValidLocales)}", nameof(code));

        return new Locale(normalizedCode);
    }

    /// <summary>
    /// Creates a default English locale
    /// </summary>
    public static Locale English => new("en");

    /// <summary>
    /// Gets all supported locale codes
    /// </summary>
    public static IReadOnlyCollection<string> SupportedLocales => ValidLocales.ToList().AsReadOnly();

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <inheritdoc/>
    public override string ToString() => Code;
}
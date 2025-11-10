using PhoneticAnalyzers.Domain.Common;

namespace PhoneticAnalyzers.Domain.ValueObjects;

/// <summary>
/// Represents a script type for multilingual name variants (e.g., "Latn", "Cyrl", "Deva")
/// </summary>
public sealed class Script : ValueObject
{
    private static readonly HashSet<string> ValidScripts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Latn", "Cyrl", "Deva", "Arab", "Hani", "Hira", "Kana", "Hang", "Thai", "Grek", "Hebr"
    };

    /// <summary>
    /// Gets the script code
    /// </summary>
    public string Code { get; }

    private Script(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Creates a new script from a string code
    /// </summary>
    /// <param name="code">The script code</param>
    /// <returns>A new Script instance</returns>
    /// <exception cref="ArgumentException">Thrown when the script code is invalid</exception>
    public static Script Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Script code cannot be null or whitespace.", nameof(code));

        var normalizedCode = code.Trim();
        
        // Normalize script code (first letter uppercase, rest lowercase)
        normalizedCode = char.ToUpperInvariant(normalizedCode[0]) + normalizedCode[1..].ToLowerInvariant();

        if (!ValidScripts.Contains(normalizedCode))
            throw new ArgumentException($"Invalid script code: {code}. Supported scripts: {string.Join(", ", ValidScripts)}", nameof(code));

        return new Script(normalizedCode);
    }

    /// <summary>
    /// Creates a Latin script
    /// </summary>
    public static Script Latin => new("Latn");

    /// <summary>
    /// Creates a Cyrillic script
    /// </summary>
    public static Script Cyrillic => new("Cyrl");

    /// <summary>
    /// Creates a Devanagari script
    /// </summary>
    public static Script Devanagari => new("Deva");

    /// <summary>
    /// Creates an Arabic script
    /// </summary>
    public static Script Arabic => new("Arab");

    /// <summary>
    /// Gets all supported script codes
    /// </summary>
    public static IReadOnlyCollection<string> SupportedScripts => ValidScripts.ToList().AsReadOnly();

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <inheritdoc/>
    public override string ToString() => Code;
}
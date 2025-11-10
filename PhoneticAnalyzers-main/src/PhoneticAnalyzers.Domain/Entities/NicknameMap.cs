using PhoneticAnalyzers.Domain.Common;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Domain.Entities;

/// <summary>
/// Represents a curated nickname mapping for deterministic name expansion
/// </summary>
public sealed class NicknameMap : BaseEntity
{
    /// <summary>
    /// Gets the canonical name
    /// </summary>
    public string CanonicalName { get; private set; }

    /// <summary>
    /// Gets the nickname or variant
    /// </summary>
    public string Nickname { get; private set; }

    /// <summary>
    /// Gets the locale for this mapping
    /// </summary>
    public Locale Locale { get; private set; }

    /// <summary>
    /// Gets the normalized canonical name
    /// </summary>
    public NormalizedName NormalizedCanonicalName { get; private set; }

    /// <summary>
    /// Gets the normalized nickname
    /// </summary>
    public NormalizedName NormalizedNickname { get; private set; }

    /// <summary>
    /// Gets whether this mapping is bidirectional (nickname can map back to canonical)
    /// </summary>
    public bool IsBidirectional { get; private set; }

    /// <summary>
    /// Gets the confidence score for this mapping
    /// </summary>
    public decimal Confidence { get; private set; }



    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private NicknameMap()
    {
        CanonicalName = string.Empty;
        Nickname = string.Empty;
        Locale = null!;
        NormalizedCanonicalName = null!;
        NormalizedNickname = null!;
    }

    /// <summary>
    /// Creates a new nickname mapping
    /// </summary>
    /// <param name="canonicalName">The canonical name</param>
    /// <param name="nickname">The nickname or variant</param>
    /// <param name="locale">The locale</param>
    /// <param name="isBidirectional">Whether the mapping is bidirectional</param>
    /// <param name="confidence">The confidence score</param>
    /// <returns>A new NicknameMap instance</returns>
    public static NicknameMap Create(
        string canonicalName,
        string nickname,
        Locale locale,
        bool isBidirectional = true,
        decimal confidence = 1.0m)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
            throw new ArgumentException("Canonical name cannot be null or whitespace.", nameof(canonicalName));

        if (string.IsNullOrWhiteSpace(nickname))
            throw new ArgumentException("Nickname cannot be null or whitespace.", nameof(nickname));

        ArgumentNullException.ThrowIfNull(locale);

        if (confidence < 0.0m || confidence > 1.0m)
            throw new ArgumentException("Confidence must be between 0.0 and 1.0.", nameof(confidence));

        var nicknameMap = new NicknameMap
        {
            CanonicalName = canonicalName.Trim(),
            Nickname = nickname.Trim(),
            Locale = locale,
            NormalizedCanonicalName = NormalizedName.Create(canonicalName),
            NormalizedNickname = NormalizedName.Create(nickname),
            IsBidirectional = isBidirectional,
            Confidence = confidence
        };

        nicknameMap.SetCreatedTimestamp();
        return nicknameMap;
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
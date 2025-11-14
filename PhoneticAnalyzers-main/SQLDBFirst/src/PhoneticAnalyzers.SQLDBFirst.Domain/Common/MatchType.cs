namespace PhoneticAnalyzers.SQLDBFirst.Domain.Common;

/// <summary>
/// Types of name matching algorithms used in search.
/// Each type has a different confidence score.
/// </summary>
public enum MatchType
{
    /// <summary>Exact match (1.0 confidence)</summary>
    Exact,

    /// <summary>Token contains match (0.95 confidence)</summary>
    TokenContains,

    /// <summary>Nickname expansion match (0.93 confidence)</summary>
    NicknameExpansion,

    /// <summary>Primary Double Metaphone match (0.9 confidence)</summary>
    PrimaryDoubleMetaphone,

    /// <summary>Alternate Double Metaphone match (0.85 confidence)</summary>
    AlternateDoubleMetaphone,

    /// <summary>Beider-Morse phonetic match (0.8 confidence)</summary>
    BeiderMorse,

    /// <summary>Trigram similarity match (variable confidence based on similarity score)</summary>
    TrigramSimilarity
}

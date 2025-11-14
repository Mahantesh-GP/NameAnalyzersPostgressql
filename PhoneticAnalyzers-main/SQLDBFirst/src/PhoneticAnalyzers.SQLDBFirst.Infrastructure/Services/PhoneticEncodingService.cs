using PhoneticAnalyzers.SQLDBFirst.Domain.Services;
using Lucene.Net.Analysis.Phonetic.Language;
using Lucene.Net.Analysis.Phonetic.Language.Bm;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure.Services;

/// <summary>
/// Service implementation for phonetic encoding using Lucene.Net library.
/// Generates Double Metaphone and Beider-Morse codes for fuzzy name matching.
/// </summary>
public class PhoneticEncodingService : IPhoneticEncodingService
{
    private readonly DoubleMetaphone _doubleMetaphone;
    private readonly BeiderMorseEncoder _beiderMorse;

    public PhoneticEncodingService()
    {
        _doubleMetaphone = new DoubleMetaphone { MaxCodeLen = 8 };
        _beiderMorse = new BeiderMorseEncoder
        {
            NameType = NameType.GENERIC,
            RuleType = RuleType.APPROX,
            IsConcat = true
        };
    }

    public (string? primary, string? alternate) GetDoubleMetaphone(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        try
        {
            var primary = _doubleMetaphone.GetDoubleMetaphone(text);
            var alternate = _doubleMetaphone.GetDoubleMetaphone(text, true);
            
            return (primary, alternate != primary ? alternate : null);
        }
        catch
        {
            return (null, null);
        }
    }

    public IEnumerable<string> GetBeiderMorseCodes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Enumerable.Empty<string>();

        try
        {
            var encoded = _beiderMorse.Encode(text);
            if (string.IsNullOrWhiteSpace(encoded))
                return Enumerable.Empty<string>();

            // Beider-Morse returns pipe-separated codes
            return encoded.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to uppercase, trim, and normalize spaces
        var normalized = name.ToUpperInvariant().Trim();
        
        // Replace multiple spaces with single space
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }
}

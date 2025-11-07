using Lucene.Net.Analysis.Phonetic.Language;
using PhoneticAnalyzers.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace PhoneticAnalyzers.Application.Services.Phonetic;

/// <summary>
/// Double Metaphone phonetic encoder implementation
/// </summary>
public sealed class DoubleMetaphoneEncoder : IPhoneticEncoder
{
    private readonly DoubleMetaphone _doubleMetaphone;
    private readonly ILogger<DoubleMetaphoneEncoder> _logger;

    /// <inheritdoc/>
    public PhoneticAlgorithmType AlgorithmType => PhoneticAlgorithmType.DoubleMetaphone;

    /// <inheritdoc/>
    public bool SupportsMultipleVariants => true; // Primary and alternate codes

    /// <summary>
    /// Initializes a new instance of the DoubleMetaphoneEncoder class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public DoubleMetaphoneEncoder(ILogger<DoubleMetaphoneEncoder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _doubleMetaphone = new DoubleMetaphone { MaxCodeLen = 8 };
    }

    /// <inheritdoc/>
    public IReadOnlyList<PhoneticCode> Encode(NormalizedName normalizedName)
    {
        if (normalizedName == null)
            throw new ArgumentNullException(nameof(normalizedName));

        try
        {
            var codes = new List<PhoneticCode>();

            // Get primary code
            var primaryCode = _doubleMetaphone.GetDoubleMetaphone(normalizedName.Value);
            if (!string.IsNullOrWhiteSpace(primaryCode))
            {
                codes.Add(PhoneticCode.Create(primaryCode, PhoneticAlgorithmType.DoubleMetaphone, isPrimary: true));
            }

            // Get alternate code
            var alternateCode = _doubleMetaphone.GetDoubleMetaphone(normalizedName.Value, true);
            if (!string.IsNullOrWhiteSpace(alternateCode) && alternateCode != primaryCode)
            {
                codes.Add(PhoneticCode.Create(alternateCode, PhoneticAlgorithmType.DoubleMetaphone, isPrimary: false));
            }

            _logger.LogDebug("Encoded '{Name}' to {CodeCount} Double Metaphone codes", normalizedName.Value, codes.Count);
            return codes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encoding name '{Name}' with Double Metaphone", normalizedName.Value);
            throw;
        }
    }
}

/// <summary>
/// Beider-Morse phonetic encoder implementation
/// </summary>
public sealed class BeiderMorseEncoder : IPhoneticEncoder
{
    private readonly Lucene.Net.Analysis.Phonetic.Language.Bm.BeiderMorseEncoder _beiderMorseEncoder;
    private readonly ILogger<BeiderMorseEncoder> _logger;
    private const int MaxVariants = 16;

    /// <inheritdoc/>
    public PhoneticAlgorithmType AlgorithmType => PhoneticAlgorithmType.BeiderMorse;

    /// <inheritdoc/>
    public bool SupportsMultipleVariants => true; // Multiple language variants

    /// <summary>
    /// Initializes a new instance of the BeiderMorseEncoder class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public BeiderMorseEncoder(ILogger<BeiderMorseEncoder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _beiderMorseEncoder = new Lucene.Net.Analysis.Phonetic.Language.Bm.BeiderMorseEncoder
        {
            NameType = Lucene.Net.Analysis.Phonetic.Language.Bm.NameType.GENERIC,
            RuleType = Lucene.Net.Analysis.Phonetic.Language.Bm.RuleType.APPROX,
            IsConcat = true
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<PhoneticCode> Encode(NormalizedName normalizedName)
    {
        if (normalizedName == null)
            throw new ArgumentNullException(nameof(normalizedName));

        try
        {
            var encodedValue = _beiderMorseEncoder.Encode(normalizedName.Value);
            if (string.IsNullOrWhiteSpace(encodedValue))
            {
                _logger.LogDebug("No Beider-Morse codes generated for '{Name}'", normalizedName.Value);
                return [];
            }

            // Split by pipe separator and create codes
            var variants = encodedValue
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(variant => variant.Trim().ToUpperInvariant())
                .Where(variant => !string.IsNullOrEmpty(variant))
                .Distinct()
                .Take(MaxVariants)
                .Select(variant => PhoneticCode.Create(variant, PhoneticAlgorithmType.BeiderMorse))
                .ToList();

            _logger.LogDebug("Encoded '{Name}' to {VariantCount} Beider-Morse variants", normalizedName.Value, variants.Count);
            return variants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encoding name '{Name}' with Beider-Morse", normalizedName.Value);
            throw;
        }
    }
}

/// <summary>
/// Factory for creating phonetic encoders
/// </summary>
public sealed class PhoneticEncoderFactory : IPhoneticEncoderFactory
{
    private readonly IReadOnlyDictionary<PhoneticAlgorithmType, IPhoneticEncoder> _encoders;

    /// <summary>
    /// Initializes a new instance of the PhoneticEncoderFactory class
    /// </summary>
    /// <param name="doubleMetaphoneEncoder">The Double Metaphone encoder</param>
    /// <param name="beiderMorseEncoder">The Beider-Morse encoder</param>
    public PhoneticEncoderFactory(
        DoubleMetaphoneEncoder doubleMetaphoneEncoder,
        BeiderMorseEncoder beiderMorseEncoder)
    {
        if (doubleMetaphoneEncoder == null)
            throw new ArgumentNullException(nameof(doubleMetaphoneEncoder));
        if (beiderMorseEncoder == null)
            throw new ArgumentNullException(nameof(beiderMorseEncoder));

        _encoders = new Dictionary<PhoneticAlgorithmType, IPhoneticEncoder>
        {
            [PhoneticAlgorithmType.DoubleMetaphone] = doubleMetaphoneEncoder,
            [PhoneticAlgorithmType.BeiderMorse] = beiderMorseEncoder
        };
    }

    /// <inheritdoc/>
    public IPhoneticEncoder GetEncoder(PhoneticAlgorithmType algorithmType)
    {
        if (_encoders.TryGetValue(algorithmType, out var encoder))
        {
            return encoder;
        }

        throw new NotSupportedException($"Phonetic algorithm type '{algorithmType}' is not supported");
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPhoneticEncoder> GetAllEncoders()
    {
        return _encoders.Values.ToList();
    }
}
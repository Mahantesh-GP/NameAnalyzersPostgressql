using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Application.Services.Phonetic;

/// <summary>
/// Service for phonetic encoding operations
/// </summary>
public sealed class PhoneticEncodingService : IPhoneticEncodingService
{
    private readonly IPhoneticEncoderFactory _encoderFactory;
    private readonly ILogger<PhoneticEncodingService> _logger;

    /// <summary>
    /// Initializes a new instance of the PhoneticEncodingService class
    /// </summary>
    /// <param name="encoderFactory">The phonetic encoder factory</param>
    /// <param name="logger">The logger instance</param>
    public PhoneticEncodingService(
        IPhoneticEncoderFactory encoderFactory,
        ILogger<PhoneticEncodingService> logger)
    {
        _encoderFactory = encoderFactory ?? throw new ArgumentNullException(nameof(encoderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PhoneticEncodingResult> EncodeAsync(NormalizedName normalizedName)
    {
        if (normalizedName == null)
            throw new ArgumentNullException(nameof(normalizedName));

        _logger.LogDebug("Starting phonetic encoding for '{Name}'", normalizedName.Value);

        try
        {
            // Encode with Double Metaphone
            var doubleMetaphoneEncoder = _encoderFactory.GetEncoder(PhoneticAlgorithmType.DoubleMetaphone);
            var doubleMetaphoneCodes = doubleMetaphoneEncoder.Encode(normalizedName);

            PhoneticCode? primaryDoubleMetaphone = null;
            PhoneticCode? alternateDoubleMetaphone = null;

            foreach (var code in doubleMetaphoneCodes)
            {
                if (code.IsPrimary && primaryDoubleMetaphone == null)
                {
                    primaryDoubleMetaphone = code;
                }
                else if (!code.IsPrimary && alternateDoubleMetaphone == null)
                {
                    alternateDoubleMetaphone = code;
                }
            }

            // Encode with Beider-Morse
            var beiderMorseEncoder = _encoderFactory.GetEncoder(PhoneticAlgorithmType.BeiderMorse);
            var beiderMorseCodes = beiderMorseEncoder.Encode(normalizedName);

            var result = new PhoneticEncodingResult(
                normalizedName,
                primaryDoubleMetaphone,
                alternateDoubleMetaphone,
                beiderMorseCodes);

            _logger.LogDebug(
                "Completed phonetic encoding for '{Name}': DM Primary='{DmPrimary}', DM Alternate='{DmAlternate}', BM Count={BmCount}",
                normalizedName.Value,
                primaryDoubleMetaphone?.Value ?? "null",
                alternateDoubleMetaphone?.Value ?? "null",
                beiderMorseCodes.Count);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during phonetic encoding for '{Name}'", normalizedName.Value);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PhoneticCode>> EncodeAsync(
        NormalizedName normalizedName,
        PhoneticAlgorithmType algorithmType)
    {
        if (normalizedName == null)
            throw new ArgumentNullException(nameof(normalizedName));

        _logger.LogDebug("Starting {AlgorithmType} encoding for '{Name}'", algorithmType, normalizedName.Value);

        try
        {
            var encoder = _encoderFactory.GetEncoder(algorithmType);
            var codes = encoder.Encode(normalizedName);

            _logger.LogDebug(
                "Completed {AlgorithmType} encoding for '{Name}': {CodeCount} codes generated",
                algorithmType,
                normalizedName.Value,
                codes.Count);

            return await Task.FromResult(codes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {AlgorithmType} encoding for '{Name}'", algorithmType, normalizedName.Value);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PhoneticEncodingResult>> EncodeBatchAsync(IEnumerable<NormalizedName> normalizedNames)
    {
        if (normalizedNames == null)
            throw new ArgumentNullException(nameof(normalizedNames));

        var namesList = normalizedNames.ToList();
        _logger.LogDebug("Starting batch phonetic encoding for {Count} names", namesList.Count);

        try
        {
            var results = new List<PhoneticEncodingResult>();

            // Process in parallel for better performance
            var tasks = namesList.Select(async name => await EncodeAsync(name));
            var encodingResults = await Task.WhenAll(tasks);

            results.AddRange(encodingResults);

            _logger.LogDebug("Completed batch phonetic encoding for {Count} names", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch phonetic encoding");
            throw;
        }
    }
}

/// <summary>
/// Service for nickname/alias expansion
/// </summary>
public interface INicknameService
{
    /// <summary>
    /// Expands a name to include common nicknames and aliases
    /// </summary>
    /// <param name="name">The name to expand</param>
    /// <returns>A collection of expanded name variations</returns>
    Task<IReadOnlyList<string>> ExpandNicknamesAsync(string name);

    /// <summary>
    /// Gets the canonical (formal) name for a given nickname
    /// </summary>
    /// <param name="nickname">The nickname to resolve</param>
    /// <returns>The canonical name if found, null otherwise</returns>
    Task<string?> GetCanonicalNameAsync(string nickname);
}

/// <summary>
/// Simple in-memory nickname service implementation
/// </summary>
public sealed class InMemoryNicknameService : INicknameService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> NicknameMap = 
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["JOHN"] = new[] { "JOHN", "JON", "JOHNNY", "JACK" },
            ["JONATHAN"] = new[] { "JONATHAN", "JON", "JOHN", "JOHNNY" },
            ["ELIZABETH"] = new[] { "ELIZABETH", "BETH", "LIZ", "LIZZY", "BETTY", "ELIZA" },
            ["ROBERT"] = new[] { "ROBERT", "BOB", "BOBBY", "ROB", "ROBBIE" },
            ["WILLIAM"] = new[] { "WILLIAM", "BILL", "BILLY", "WILL", "WILLIE", "LIAM" },
            ["MICHAEL"] = new[] { "MICHAEL", "MIKE", "MICKEY", "MICK" },
            ["CHRISTOPHER"] = new[] { "CHRISTOPHER", "CHRIS", "CHRISTY", "KIT" },
            ["RICHARD"] = new[] { "RICHARD", "RICK", "RICKY", "DICK", "RICH" },
            ["PATRICIA"] = new[] { "PATRICIA", "PAT", "PATTY", "TRICIA" },
            ["JENNIFER"] = new[] { "JENNIFER", "JEN", "JENNY", "JENN" },
            ["CATHERINE"] = new[] { "CATHERINE", "KATE", "KATIE", "CATHY", "CAT", "KAT" },
            ["MARGARET"] = new[] { "MARGARET", "MAGGIE", "MEG", "PEGGY", "RITA" },
            ["ANTHONY"] = new[] { "ANTHONY", "TONY", "ANT" },
            ["STEPHANIE"] = new[] { "STEPHANIE", "STEPH", "STEPHIE" },
            ["ALEXANDER"] = new[] { "ALEXANDER", "ALEX", "SANDY", "XANDER" },
            ["NICHOLAS"] = new[] { "NICHOLAS", "NICK", "NICKY", "COLE" }
        };

    private static readonly IReadOnlyDictionary<string, string> ReverseMap = 
        NicknameMap
            .SelectMany(kvp => kvp.Value.Select(nickname => new { Nickname = nickname, Canonical = kvp.Key }))
            .Where(x => !string.Equals(x.Nickname, x.Canonical, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Nickname, x => x.Canonical, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ExpandNicknamesAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        var normalizedName = name.Trim().ToUpperInvariant();

        // Try direct lookup
        if (NicknameMap.TryGetValue(normalizedName, out var directVariants))
        {
            return await Task.FromResult(directVariants);
        }

        // Try reverse lookup (nickname to canonical, then get all variants)
        if (ReverseMap.TryGetValue(normalizedName, out var canonical) && 
            NicknameMap.TryGetValue(canonical, out var variants))
        {
            return await Task.FromResult(variants);
        }

        // Return original name if no variants found
        return await Task.FromResult<IReadOnlyList<string>>(new[] { normalizedName });
    }

    /// <inheritdoc/>
    public async Task<string?> GetCanonicalNameAsync(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return null;

        var normalizedNickname = nickname.Trim().ToUpperInvariant();
        
        ReverseMap.TryGetValue(normalizedNickname, out var canonical);
        return await Task.FromResult(canonical);
    }
}
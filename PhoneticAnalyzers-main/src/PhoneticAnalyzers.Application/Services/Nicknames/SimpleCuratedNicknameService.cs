using PhoneticAnalyzers.Domain.Services;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace PhoneticAnalyzers.Application.Services.Nicknames;

/// <summary>
/// Simplified implementation of curated nickname service
/// </summary>
public sealed class SimpleCuratedNicknameService : ICuratedNicknameService
{
    private readonly INicknameMapRepository _nicknameRepository;
    private readonly ILogger<SimpleCuratedNicknameService> _logger;
    private readonly IMemoryCache _cache;

    // Pre-compiled common nicknames for faster lookup
    private static readonly Dictionary<string, HashSet<string>> CommonNicknames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alexander"] = new(StringComparer.OrdinalIgnoreCase) { "Alex", "Al", "Xander", "Lex", "Sandy" },
        ["Alexandra"] = new(StringComparer.OrdinalIgnoreCase) { "Alex", "Alexa", "Lexie", "Sandy", "Sasha" },
        ["Benjamin"] = new(StringComparer.OrdinalIgnoreCase) { "Ben", "Benny", "Benji" },
        ["Catherine"] = new(StringComparer.OrdinalIgnoreCase) { "Kate", "Katie", "Cathy", "Cat", "Kit" },
        ["Christopher"] = new(StringComparer.OrdinalIgnoreCase) { "Chris", "Christie", "Kit", "Topher" },
        ["Elizabeth"] = new(StringComparer.OrdinalIgnoreCase) { "Liz", "Beth", "Betty", "Ellie", "Libby", "Lizzie" },
        ["Michael"] = new(StringComparer.OrdinalIgnoreCase) { "Mike", "Mickey", "Micky", "Mitch" },
        ["Robert"] = new(StringComparer.OrdinalIgnoreCase) { "Rob", "Bob", "Bobby", "Robbie" },
        ["William"] = new(StringComparer.OrdinalIgnoreCase) { "Bill", "Will", "Billy", "Willie", "Liam" }
    };

    /// <summary>
    /// Initializes a new instance of the SimpleCuratedNicknameService
    /// </summary>
    /// <param name="nicknameRepository">The nickname repository</param>
    /// <param name="logger">The logger</param>
    /// <param name="cache">The memory cache</param>
    public SimpleCuratedNicknameService(
        INicknameMapRepository nicknameRepository,
        ILogger<SimpleCuratedNicknameService> logger,
        IMemoryCache cache)
    {
        _nicknameRepository = nicknameRepository;
        _logger = logger;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NicknameMatch>> GetNicknamesAsync(string name, string? culture = null, double minConfidence = 0.5)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<NicknameMatch>();

        var results = new List<NicknameMatch>();

        try
        {
            // Get from repository if available
            var locale = culture != null ? Locale.Create(culture) : null;
            var dbNicknames = await _nicknameRepository.GetNicknamesAsync(name, locale);
            
            foreach (var dbNickname in dbNicknames)
            {
                if ((double)dbNickname.Confidence >= minConfidence)
                {
                    results.Add(new NicknameMatch(
                        dbNickname.CanonicalName,
                        dbNickname.Nickname,
                        (double)dbNickname.Confidence,
                        dbNickname.Locale.Code,
                        "database",
                        NicknameMatchType.Exact
                    ));
                }
            }

            // Add common nicknames
            if (CommonNicknames.TryGetValue(name, out var commonNicks))
            {
                foreach (var nick in commonNicks)
                {
                    if (!results.Any(r => string.Equals(r.Nickname, nick, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new NicknameMatch(
                            name,
                            nick,
                            0.8, // High confidence for common nicknames
                            culture ?? "en",
                            "builtin",
                            NicknameMatchType.Exact
                        ));
                    }
                }
            }

            return results.Where(r => r.Confidence >= minConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nicknames for {Name}", name);
            return Enumerable.Empty<NicknameMatch>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NicknameMatch>> GetBaseNamesAsync(string nickname, string? culture = null, double minConfidence = 0.5)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return Enumerable.Empty<NicknameMatch>();

        var results = new List<NicknameMatch>();

        try
        {
            // Get from repository if available
            var locale = culture != null ? Locale.Create(culture) : null;
            var dbBaseNames = await _nicknameRepository.GetCanonicalNamesAsync(nickname, locale);
            
            foreach (var dbBaseName in dbBaseNames)
            {
                if ((double)dbBaseName.Confidence >= minConfidence)
                {
                    results.Add(new NicknameMatch(
                        dbBaseName.CanonicalName,
                        dbBaseName.Nickname,
                        (double)dbBaseName.Confidence,
                        dbBaseName.Locale.Code,
                        "database",
                        NicknameMatchType.Exact
                    ));
                }
            }

            // Check common nicknames in reverse
            foreach (var (baseName, nicknames) in CommonNicknames)
            {
                if (nicknames.Contains(nickname, StringComparer.OrdinalIgnoreCase))
                {
                    if (!results.Any(r => string.Equals(r.Name, baseName, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new NicknameMatch(
                            baseName,
                            nickname,
                            0.8,
                            culture ?? "en",
                            "builtin",
                            NicknameMatchType.Exact
                        ));
                    }
                }
            }

            return results.Where(r => r.Confidence >= minConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting base names for {Nickname}", nickname);
            return Enumerable.Empty<NicknameMatch>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NicknameMatch>> GetFuzzyMatchesAsync(string name, int maxEditDistance = 2, bool includePhonetic = true, string? culture = null)
    {
        // Simple implementation - just return empty for now
        await Task.Delay(1);
        return Enumerable.Empty<NicknameMatch>();
    }

    /// <inheritdoc />
    public async Task<bool> AddNicknameMappingAsync(string baseName, string nickname, double confidence, string? culture = null, string? source = null)
    {
        try
        {
            var locale = Locale.Create(culture ?? "en");
            var nicknameMap = Domain.Entities.NicknameMap.Create(
                baseName,
                nickname,
                locale,
                true, // bidirectional
                (decimal)Math.Clamp(confidence, 0.0, 1.0)
            );

            await _nicknameRepository.AddAsync(nicknameMap);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding nickname mapping {BaseName} -> {Nickname}", baseName, nickname);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<NicknameImportResult> ImportNicknameDatasetAsync(string dataSource, string filePath, string format, string? culture = null)
    {
        // Placeholder implementation
        await Task.Delay(1);
        return new NicknameImportResult(0, 0, 0, 0, TimeSpan.Zero, Array.Empty<string>());
    }

    /// <inheritdoc />
    public async Task<NicknameStatistics> GetStatisticsAsync()
    {
        try
        {
            var count = await _nicknameRepository.GetCountAsync();
            return new NicknameStatistics(
                (int)count,
                0, // UniqueBase - would need custom query
                0, // UniqueNicknames - would need custom query
                0.0, // AverageConfidence - would need custom query
                new Dictionary<string, int>(), // CultureDistribution
                new Dictionary<string, int>(), // SourceDistribution
                DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nickname statistics");
            return new NicknameStatistics(0, 0, 0, 0.0, new(), new(), DateTime.UtcNow);
        }
    }

    /// <inheritdoc />
    public async Task<int> UpdateConfidenceScoresAsync(IEnumerable<NicknameUsageData> learningData)
    {
        // Placeholder implementation
        await Task.Delay(1);
        return 0;
    }
}
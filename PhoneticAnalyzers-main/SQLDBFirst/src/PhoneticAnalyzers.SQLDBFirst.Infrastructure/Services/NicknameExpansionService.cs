using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Domain.Services;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure.Services;

/// <summary>
/// Service implementation for nickname expansion operations.
/// Uses database nickname mappings for accurate variant generation.
/// </summary>
public class NicknameExpansionService : INicknameExpansionService
{
    private readonly INicknameMapRepository _nicknameRepository;

    public NicknameExpansionService(INicknameMapRepository nicknameRepository)
    {
        _nicknameRepository = nicknameRepository;
    }

    public async Task<IEnumerable<string>> GetNicknameVariantsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<string>();

        var normalized = name.ToUpperInvariant().Trim();
        return await _nicknameRepository.GetNicknamesAsync(normalized, cancellationToken);
    }

    public async Task<bool> HasNicknamesAsync(string name, CancellationToken cancellationToken = default)
    {
        var variants = await GetNicknameVariantsAsync(name, cancellationToken);
        return variants.Any();
    }
}

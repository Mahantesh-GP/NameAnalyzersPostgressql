using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

namespace PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;

/// <summary>
/// Repository interface for NicknameMap operations.
/// Handles bidirectional nickname lookups (William ↔ Bill, Robert ↔ Bob).
/// </summary>
public interface INicknameMapRepository
{
    Task<IEnumerable<string>> GetNicknamesAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCanonicalNamesAsync(string nickname, CancellationToken cancellationToken = default);
    Task<IEnumerable<NicknameMap>> GetAllMappingsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string canonicalName, string nickname, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

namespace PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;

/// <summary>
/// Repository interface for Person entity operations.
/// Database-First implementation will use scaffolded models.
/// </summary>
public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(long personId, CancellationToken cancellationToken = default);
    Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<long> AddAsync(Person person, CancellationToken cancellationToken = default);
    Task UpdateAsync(Person person, CancellationToken cancellationToken = default);
    Task DeleteAsync(long personId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetNameSuggestionsAsync(string prefix, int maxSuggestions = 10, CancellationToken cancellationToken = default);
}

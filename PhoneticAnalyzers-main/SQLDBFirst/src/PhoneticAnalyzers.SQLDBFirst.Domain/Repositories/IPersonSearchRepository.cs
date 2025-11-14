using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

namespace PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;

/// <summary>
/// Repository interface for advanced person search operations.
/// Supports phonetic matching, trigram similarity, and nickname expansion.
/// </summary>
public interface IPersonSearchRepository
{
    Task<IEnumerable<PersonSearchMatch>> SearchByNameAsync(
        string searchName,
        double minSimilarity = 0.3,
        bool expandNicknames = false,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Person>> SearchByMetaphoneAsync(
        string searchName,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Person>> SearchByBeiderMorseAsync(
        string searchName,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Person>> SearchByTrigramAsync(
        string searchName,
        double minSimilarity = 0.3,
        CancellationToken cancellationToken = default);
}

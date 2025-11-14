using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Application.Queries;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Handlers;

/// <summary>
/// Handler for GetDatabaseStatsQuery.
/// Returns database statistics for monitoring.
/// </summary>
public class GetDatabaseStatsQueryHandler : IRequestHandler<GetDatabaseStatsQuery, DatabaseStatsDto>
{
    private readonly IPersonRepository _personRepository;
    private readonly INicknameMapRepository _nicknameRepository;

    public GetDatabaseStatsQueryHandler(
        IPersonRepository personRepository,
        INicknameMapRepository nicknameRepository)
    {
        _personRepository = personRepository;
        _nicknameRepository = nicknameRepository;
    }

    public async Task<DatabaseStatsDto> Handle(GetDatabaseStatsQuery request, CancellationToken cancellationToken)
    {
        var totalPersons = await _personRepository.GetCountAsync(cancellationToken);
        var totalNicknames = await _nicknameRepository.GetCountAsync(cancellationToken);

        return new DatabaseStatsDto
        {
            TotalPersons = totalPersons,
            TotalNicknameMappings = totalNicknames,
            LastUpdated = DateTime.UtcNow
        };
    }
}

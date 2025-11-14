using MediatR;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Queries;

/// <summary>
/// Query to get database statistics.
/// </summary>
public class GetDatabaseStatsQuery : IRequest<DatabaseStatsDto>
{
}

public class DatabaseStatsDto
{
    public int TotalPersons { get; set; }
    public int TotalNicknameMappings { get; set; }
    public DateTime? LastUpdated { get; set; }
}

using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Domain.Common;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Commands;

/// <summary>
/// Command to ingest a single person into the database.
/// </summary>
public class IngestPersonCommand : IRequest<OperationResult<long>>
{
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? County { get; set; }
    public bool ExpandNicknames { get; set; } = false;
}

using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Application.DTOs;
using PhoneticAnalyzers.SQLDBFirst.Domain.Common;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Commands;

/// <summary>
/// Command to ingest multiple persons in a batch.
/// </summary>
public class BatchIngestCommand : IRequest<OperationResult<int>>
{
    public List<PersonIngestDto> Persons { get; set; } = new();
}

using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Application.Commands;
using PhoneticAnalyzers.SQLDBFirst.Domain.Common;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Handlers;

/// <summary>
/// Handler for BatchIngestCommand.
/// Processes multiple person ingestions using IngestPersonCommand.
/// </summary>
public class BatchIngestCommandHandler : IRequestHandler<BatchIngestCommand, OperationResult<int>>
{
    private readonly IMediator _mediator;

    public BatchIngestCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<OperationResult<int>> Handle(BatchIngestCommand request, CancellationToken cancellationToken)
    {
        if (!request.Persons.Any())
        {
            return OperationResult<int>.Failure("No persons provided for batch ingestion");
        }

        int successCount = 0;
        int failureCount = 0;
        var messages = new List<string>();
        var warnings = new List<string>();
        var errors = new List<string>();

        foreach (var personDto in request.Persons)
        {
            var command = new IngestPersonCommand
            {
                ExternalId = personDto.ExternalId,
                FullName = personDto.FullName,
                County = personDto.County,
                ExpandNicknames = personDto.ExpandNicknames
            };

            try
            {
                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    successCount++;
                    messages.AddRange(result.Messages);
                    warnings.AddRange(result.Warnings);
                }
                else
                {
                    failureCount++;
                    errors.AddRange(result.Errors);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                errors.Add($"Error ingesting '{personDto.FullName}': {ex.Message}");
            }
        }

        var batchResult = OperationResult<int>.Success(
            successCount,
            $"Batch ingestion completed: {successCount} succeeded, {failureCount} failed"
        );

        foreach (var msg in messages) batchResult.AddMessage(msg);
        foreach (var warning in warnings) batchResult.AddWarning(warning);
        foreach (var error in errors) batchResult.Errors.Add(error);

        return batchResult;
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Commands.Ingestion;
using PhoneticAnalyzers.Application.Services.Phonetic;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Application.Handlers.Ingestion;

/// <summary>
/// Handler for IngestPersonCommand
/// </summary>
public sealed class IngestPersonCommandHandler : IRequestHandler<IngestPersonCommand, IngestPersonCommandResult>
{
    private readonly IPersonRepository _personRepository;
    private readonly IPhoneticEncodingService _phoneticService;
    private readonly ILogger<IngestPersonCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the IngestPersonCommandHandler class
    /// </summary>
    public IngestPersonCommandHandler(
        IPersonRepository personRepository,
        IPhoneticEncodingService phoneticService,
        ILogger<IngestPersonCommandHandler> logger)
    {
        _personRepository = personRepository;
        _phoneticService = phoneticService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the IngestPersonCommand
    /// </summary>
    public async Task<IngestPersonCommandResult> Handle(IngestPersonCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing person ingestion for ExternalId: {ExternalId}, Name: {FullName}",
            request.ExternalId, request.FullName);

        // Create value objects
        var externalId = ExternalId.Create(request.ExternalId);
        var normalizedName = NormalizedName.Create(request.FullName);

        // Generate phonetic codes
        var phoneticResult = await _phoneticService.EncodeAsync(normalizedName);

        // Check if person already exists
        var existingPerson = await _personRepository.GetByExternalIdAsync(externalId, cancellationToken);
        
        Person person;
        bool wasCreated;
        var warnings = new List<string>();

        if (existingPerson != null)
        {
            // Update existing person
            person = existingPerson;
            person.Update(
                request.FullName,
                phoneticResult.PrimaryDoubleMetaphone,
                phoneticResult.AlternateDoubleMetaphone,
                phoneticResult.BeiderMorseCodes);
            
            await _personRepository.UpdateAsync(person, cancellationToken);
            wasCreated = false;

            _logger.LogInformation("Person updated with ID: {PersonId}", person.Id);
        }
        else
        {
            // Create new person
            person = Person.Create(
                externalId,
                request.FullName,
                phoneticResult.PrimaryDoubleMetaphone,
                phoneticResult.AlternateDoubleMetaphone,
                phoneticResult.BeiderMorseCodes);

            await _personRepository.AddAsync(person, cancellationToken);
            wasCreated = true;

            _logger.LogInformation("Person created with ID: {PersonId}", person.Id);
        }

        return new IngestPersonCommandResult
        {
            PersonId = person.Id,
            ExternalId = person.ExternalId.Value,
            WasCreated = wasCreated,
            PhoneticEncoding = new PhoneticEncodingSummary
            {
                PrimaryDoubleMetaphone = phoneticResult.PrimaryDoubleMetaphone?.Value,
                AlternateDoubleMetaphone = phoneticResult.AlternateDoubleMetaphone?.Value,
                BeiderMorseCodes = phoneticResult.BeiderMorseCodes.Select(c => c.Value).ToList(),
                BeiderMorseVariantCount = phoneticResult.BeiderMorseCodes.Count
            },
            Warnings = warnings
        };
    }
}
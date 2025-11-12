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
    private readonly INicknameMapRepository _nicknameRepository;
    private readonly IPhoneticEncodingService _phoneticService;
    private readonly ILogger<IngestPersonCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the IngestPersonCommandHandler class
    /// </summary>
    public IngestPersonCommandHandler(
        IPersonRepository personRepository,
        INicknameMapRepository nicknameRepository,
        IPhoneticEncodingService phoneticService,
        ILogger<IngestPersonCommandHandler> logger)
    {
        _personRepository = personRepository;
        _nicknameRepository = nicknameRepository;
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
        // Generate ExternalId if not provided (for CSV uploads without external IDs)
        var externalIdValue = string.IsNullOrWhiteSpace(request.ExternalId)
            ? $"GEN-{Guid.NewGuid():N}"  // Generate unique ID
            : request.ExternalId;
        
        var externalId = ExternalId.Create(externalIdValue);
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
                request.County,
                request.CountyId,
                request.CountyName,
                request.Flag,
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
                request.County,
                request.CountyId,
                request.CountyName,
                request.Flag,
                phoneticResult.PrimaryDoubleMetaphone,
                phoneticResult.AlternateDoubleMetaphone,
                phoneticResult.BeiderMorseCodes);

            await _personRepository.AddAsync(person, cancellationToken);
            wasCreated = true;

            _logger.LogInformation("Person created with ID: {PersonId}", person.Id);

            // Generate nickname variants if requested
            if (request.ExpandNicknames && wasCreated)
            {
                var nicknameVariantsCreated = await CreateNicknameVariantsAsync(
                    person, 
                    request.County, 
                    request.CountyId, 
                    request.CountyName, 
                    request.Flag, 
                    cancellationToken);
                
                if (nicknameVariantsCreated > 0)
                {
                    _logger.LogInformation("Created {Count} nickname variant records for person {PersonId}", 
                        nicknameVariantsCreated, person.Id);
                    warnings.Add($"Created {nicknameVariantsCreated} nickname variant(s)");
                }
            }
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

    /// <summary>
    /// Creates person records for all nickname variants of the given person's name
    /// </summary>
    private async Task<int> CreateNicknameVariantsAsync(
        Person originalPerson,
        string county,
        int countyId,
        string countyName,
        RecordTypeFlag flag,
        CancellationToken cancellationToken)
    {
        var variantsCreated = 0;

        try
        {
            // Parse the full name to extract first name token(s)
            var nameTokens = originalPerson.NormalizedName.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (nameTokens.Length == 0)
            {
                return 0;
            }

            // Get the first name (first token)
            var firstName = nameTokens[0];
            
            // Get the rest of the name (surname and middle names)
            var restOfName = nameTokens.Length > 1 
                ? string.Join(" ", nameTokens.Skip(1)) 
                : string.Empty;

            // Query database for nickname mappings
            var nicknameMappings = await _nicknameRepository.GetNicknamesAsync(firstName, locale: null, cancellationToken);

            if (!nicknameMappings.Any())
            {
                _logger.LogDebug("No nickname mappings found for first name: {FirstName}", firstName);
                return 0;
            }

            _logger.LogInformation("Found {Count} nickname mappings for {FirstName}", nicknameMappings.Count, firstName);

            // Create a person record for each nickname variant
            foreach (var nicknameMapping in nicknameMappings)
            {
                try
                {
                    var nickname = nicknameMapping.Nickname.ToUpperInvariant();
                    
                    // Skip if nickname is the same as original
                    if (nickname == firstName)
                    {
                        continue;
                    }

                    // Construct full name with nickname
                    var nicknameFullName = string.IsNullOrEmpty(restOfName)
                        ? nickname
                        : $"{nickname} {restOfName}";

                    // Generate unique external ID for variant
                    var variantExternalId = ExternalId.Create($"{originalPerson.ExternalId.Value}-NICK-{nickname}");

                    // Check if variant already exists
                    var existingVariant = await _personRepository.GetByExternalIdAsync(variantExternalId, cancellationToken);
                    if (existingVariant != null)
                    {
                        _logger.LogDebug("Nickname variant already exists: {FullName}", nicknameFullName);
                        continue;
                    }

                    // Generate phonetic codes for the nickname variant
                    var variantNormalizedName = NormalizedName.Create(nicknameFullName);
                    var variantPhoneticResult = await _phoneticService.EncodeAsync(variantNormalizedName);

                    // Create the nickname variant person
                    var variantPerson = Person.Create(
                        variantExternalId,
                        nicknameFullName,
                        county,
                        countyId,
                        countyName,
                        flag,
                        variantPhoneticResult.PrimaryDoubleMetaphone,
                        variantPhoneticResult.AlternateDoubleMetaphone,
                        variantPhoneticResult.BeiderMorseCodes);

                    await _personRepository.AddAsync(variantPerson, cancellationToken);
                    variantsCreated++;

                    _logger.LogDebug("Created nickname variant: {FullName} (from {OriginalName})", 
                        nicknameFullName, originalPerson.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create nickname variant for {Nickname}", nicknameMapping.Nickname);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating nickname variants for person {PersonId}", originalPerson.Id);
        }

        return variantsCreated;
    }
}
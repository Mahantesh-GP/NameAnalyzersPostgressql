using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Application.Commands;
using PhoneticAnalyzers.SQLDBFirst.Domain.Common;
using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Domain.Services;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Handlers;

/// <summary>
/// Handler for IngestPersonCommand.
/// Creates person record with phonetic encodings and optional nickname variants.
/// </summary>
public class IngestPersonCommandHandler : IRequestHandler<IngestPersonCommand, OperationResult<long>>
{
    private readonly IPersonRepository _personRepository;
    private readonly INicknameMapRepository _nicknameRepository;
    private readonly IPhoneticEncodingService _phoneticService;

    public IngestPersonCommandHandler(
        IPersonRepository personRepository,
        INicknameMapRepository nicknameRepository,
        IPhoneticEncodingService phoneticService)
    {
        _personRepository = personRepository;
        _nicknameRepository = nicknameRepository;
        _phoneticService = phoneticService;
    }

    public async Task<OperationResult<long>> Handle(IngestPersonCommand request, CancellationToken cancellationToken)
    {
        // Check if person already exists
        if (await _personRepository.ExistsAsync(request.ExternalId, cancellationToken))
        {
            return OperationResult<long>.Failure($"Person with ExternalId '{request.ExternalId}' already exists");
        }

        // Normalize name
        var normalizedName = _phoneticService.NormalizeName(request.FullName);

        // Generate phonetic codes
        var (primaryMetaphone, alternateMetaphone) = _phoneticService.GetDoubleMetaphone(normalizedName);
        var beiderMorseCodes = _phoneticService.GetBeiderMorseCodes(normalizedName).ToList();

        // Create person entity
        var person = new Person
        {
            ExternalId = request.ExternalId,
            FullName = request.FullName,
            NormalizedName = normalizedName,
            PrimaryMetaphone = primaryMetaphone,
            AlternateMetaphone = alternateMetaphone,
            County = request.County,
            Flag = 'I',
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        // Add name tokens
        var tokens = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            var (tokenPrimary, _) = _phoneticService.GetDoubleMetaphone(tokens[i]);
            person.PersonNames.Add(new PersonName
            {
                NameToken = tokens[i],
                TokenPosition = i,
                PrimaryMetaphone = tokenPrimary,
                CreatedUtc = DateTime.UtcNow
            });
        }

        // Add Beider-Morse codes
        foreach (var bmCode in beiderMorseCodes)
        {
            person.PersonBms.Add(new PersonBm
            {
                BmCode = bmCode,
                CreatedUtc = DateTime.UtcNow
            });
        }

        // Save person
        var personId = await _personRepository.AddAsync(person, cancellationToken);

        var result = OperationResult<long>.Success(personId, $"Person '{request.FullName}' ingested successfully");

        // Generate nickname variants if requested
        if (request.ExpandNicknames)
        {
            var variantCount = await CreateNicknameVariantsAsync(person, cancellationToken);
            if (variantCount > 0)
            {
                result.AddWarning($"Generated {variantCount} nickname variant(s) for '{request.FullName}'");
            }
        }

        return result;
    }

    private async Task<int> CreateNicknameVariantsAsync(Person originalPerson, CancellationToken cancellationToken)
    {
        var tokens = originalPerson.NormalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return 0;

        var firstName = tokens[0];
        var nicknames = await _nicknameRepository.GetNicknamesAsync(firstName, cancellationToken);
        var nicknameList = nicknames.ToList();

        if (!nicknameList.Any()) return 0;

        int variantCount = 0;

        foreach (var nickname in nicknameList)
        {
            // Skip if same as original
            if (nickname.Equals(firstName, StringComparison.OrdinalIgnoreCase))
                continue;

            // Build variant name
            var variantTokens = tokens.ToArray();
            variantTokens[0] = nickname.ToUpperInvariant();
            var variantNormalizedName = string.Join(" ", variantTokens);
            var variantFullName = string.Join(" ", variantTokens);

            // Create unique external ID with nickname suffix
            var variantExternalId = $"{originalPerson.ExternalId}-NICK-{nickname.ToUpperInvariant()}";

            // Check if variant already exists
            if (await _personRepository.ExistsAsync(variantExternalId, cancellationToken))
                continue;

            // Generate phonetic codes for variant
            var (variantPrimary, variantAlternate) = _phoneticService.GetDoubleMetaphone(variantNormalizedName);
            var variantBmCodes = _phoneticService.GetBeiderMorseCodes(variantNormalizedName).ToList();

            // Create variant person
            var variantPerson = new Person
            {
                ExternalId = variantExternalId,
                FullName = variantFullName,
                NormalizedName = variantNormalizedName,
                PrimaryMetaphone = variantPrimary,
                AlternateMetaphone = variantAlternate,
                County = originalPerson.County,
                Flag = 'I',
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            // Add name tokens for variant
            for (int i = 0; i < variantTokens.Length; i++)
            {
                var (tokenPrimary, _) = _phoneticService.GetDoubleMetaphone(variantTokens[i]);
                variantPerson.PersonNames.Add(new PersonName
                {
                    NameToken = variantTokens[i],
                    TokenPosition = i,
                    PrimaryMetaphone = tokenPrimary,
                    CreatedUtc = DateTime.UtcNow
                });
            }

            // Add Beider-Morse codes for variant
            foreach (var bmCode in variantBmCodes)
            {
                variantPerson.PersonBms.Add(new PersonBm
                {
                    BmCode = bmCode,
                    CreatedUtc = DateTime.UtcNow
                });
            }

            await _personRepository.AddAsync(variantPerson, cancellationToken);
            variantCount++;
        }

        return variantCount;
    }
}

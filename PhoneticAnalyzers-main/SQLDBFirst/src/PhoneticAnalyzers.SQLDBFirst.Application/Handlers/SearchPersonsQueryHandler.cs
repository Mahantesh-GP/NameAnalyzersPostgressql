using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Application.DTOs;
using PhoneticAnalyzers.SQLDBFirst.Application.Queries;
using PhoneticAnalyzers.SQLDBFirst.Domain.Common;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Domain.Services;
using DomainMatchType = PhoneticAnalyzers.SQLDBFirst.Domain.Common.MatchType;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Handlers;

/// <summary>
/// Handler for SearchPersonsQuery.
/// Performs multi-algorithm phonetic search with nickname expansion.
/// </summary>
public class SearchPersonsQueryHandler : IRequestHandler<SearchPersonsQuery, List<PersonSearchResultDto>>
{
    private readonly IPersonSearchRepository _searchRepository;
    private readonly INicknameMapRepository _nicknameRepository;
    private readonly IPhoneticEncodingService _phoneticService;

    public SearchPersonsQueryHandler(
        IPersonSearchRepository searchRepository,
        INicknameMapRepository nicknameRepository,
        IPhoneticEncodingService phoneticService)
    {
        _searchRepository = searchRepository;
        _nicknameRepository = nicknameRepository;
        _phoneticService = phoneticService;
    }

    public async Task<List<PersonSearchResultDto>> Handle(SearchPersonsQuery request, CancellationToken cancellationToken)
    {
        var results = new Dictionary<long, SearchResult>();

        // Normalize search name
        var normalizedSearchName = _phoneticService.NormalizeName(request.SearchName);

        // 1. Exact and trigram search
        var personMatches = await _searchRepository.SearchByNameAsync(
            normalizedSearchName,
            request.MinSimilarity,
            request.ExpandNicknames,
            cancellationToken);

        foreach (var match in personMatches)
        {
            if (!results.ContainsKey(match.Person.PersonId))
            {
                // Determine match type based on similarity score
                var matchType = match.SimilarityScore >= 0.99 
                    ? DomainMatchType.Exact 
                    : DomainMatchType.TrigramSimilarity;
                
                results[match.Person.PersonId] = new SearchResult
                {
                    Person = match.Person,
                    MatchType = matchType,
                    MatchScore = match.SimilarityScore,
                    MatchedField = "NormalizedName",
                    MatchedValue = match.Person.NormalizedName
                };
            }
        }

        // 2. Nickname expansion (if enabled)
        if (request.ExpandNicknames)
        {
            var expandedNames = await GetExpandedNicknamesAsync(normalizedSearchName, cancellationToken);

            foreach (var expandedName in expandedNames)
            {
                var nicknameMatches = await _searchRepository.SearchByNameAsync(
                    expandedName,
                    request.MinSimilarity,
                    false,
                    cancellationToken);

                foreach (var match in nicknameMatches)
                {
                    if (!results.ContainsKey(match.Person.PersonId))
                    {
                        results[match.Person.PersonId] = new SearchResult
                        {
                            Person = match.Person,
                            MatchType = DomainMatchType.NicknameExpansion,
                            MatchScore = 0.93,
                            MatchedField = "NormalizedName",
                            MatchedValue = match.Person.NormalizedName
                        };
                    }
                }
            }
        }

        // 3. Metaphone search
        var metaphonePersons = await _searchRepository.SearchByMetaphoneAsync(normalizedSearchName, cancellationToken);
        foreach (var person in metaphonePersons)
        {
            if (!results.ContainsKey(person.PersonId))
            {
                results[person.PersonId] = new SearchResult
                {
                    Person = person,
                    MatchType = DomainMatchType.PrimaryDoubleMetaphone,
                    MatchScore = 0.9,
                    MatchedField = "Metaphone",
                    MatchedValue = person.PrimaryMetaphone
                };
            }
        }

        // 4. Beider-Morse search
        var bmPersons = await _searchRepository.SearchByBeiderMorseAsync(normalizedSearchName, cancellationToken);
        foreach (var person in bmPersons)
        {
            if (!results.ContainsKey(person.PersonId))
            {
                var bmCode = person.PersonBms.FirstOrDefault()?.BmCode ?? string.Empty;
                results[person.PersonId] = new SearchResult
                {
                    Person = person,
                    MatchType = DomainMatchType.BeiderMorse,
                    MatchScore = 0.8,
                    MatchedField = "BeiderMorse",
                    MatchedValue = bmCode
                };
            }
        }

        // Convert to DTOs and sort by match score
        return results.Values
            .OrderByDescending(r => r.MatchScore)
            .ThenBy(r => r.Person.FullName)
            .Select(r => new PersonSearchResultDto
            {
                PersonId = r.Person.PersonId,
                ExternalId = r.Person.ExternalId,
                FullName = r.Person.FullName,
                County = r.Person.County,
                MatchType = r.MatchType.ToString(),
                MatchScore = r.MatchScore,
                MatchedField = r.MatchedField,
                MatchedValue = r.MatchedValue
            })
            .ToList();
    }

    private async Task<List<string>> GetExpandedNicknamesAsync(string searchName, CancellationToken cancellationToken)
    {
        var tokens = searchName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return new List<string>();

        var firstName = tokens[0];
        var nicknames = await _nicknameRepository.GetNicknamesAsync(firstName, cancellationToken);
        var expandedNames = new List<string>();

        foreach (var nickname in nicknames)
        {
            if (nickname.Equals(firstName, StringComparison.OrdinalIgnoreCase))
                continue;

            var expandedTokens = tokens.ToArray();
            expandedTokens[0] = nickname.ToUpperInvariant();
            expandedNames.Add(string.Join(" ", expandedTokens));
        }

        return expandedNames;
    }
}

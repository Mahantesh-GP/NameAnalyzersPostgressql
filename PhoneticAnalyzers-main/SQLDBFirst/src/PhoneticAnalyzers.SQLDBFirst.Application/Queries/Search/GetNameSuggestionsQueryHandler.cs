using MediatR;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Queries.Search;

/// <summary>
/// Handler for getting name suggestions
/// </summary>
public class GetNameSuggestionsQueryHandler : IRequestHandler<GetNameSuggestionsQuery, GetNameSuggestionsResult>
{
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<GetNameSuggestionsQueryHandler> _logger;

    public GetNameSuggestionsQueryHandler(
        IPersonRepository personRepository,
        ILogger<GetNameSuggestionsQueryHandler> logger)
    {
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<GetNameSuggestionsResult> Handle(GetNameSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prefix) || request.Prefix.Length < 2)
        {
            return new GetNameSuggestionsResult { Suggestions = new List<string>() };
        }

        _logger.LogInformation("Getting name suggestions for prefix: {Prefix}", request.Prefix);

        var suggestions = await _personRepository.GetNameSuggestionsAsync(
            request.Prefix,
            request.MaxSuggestions,
            cancellationToken);

        _logger.LogInformation("Found {Count} suggestions for prefix: {Prefix}", suggestions.Count, request.Prefix);

        return new GetNameSuggestionsResult { Suggestions = suggestions };
    }
}

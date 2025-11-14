using MediatR;
using PhoneticAnalyzers.SQLDBFirst.Application.DTOs;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Queries;

/// <summary>
/// Query to search for persons using phonetic matching and fuzzy search.
/// </summary>
public class SearchPersonsQuery : IRequest<List<PersonSearchResultDto>>
{
    public string SearchName { get; set; } = string.Empty;
    public double MinSimilarity { get; set; } = 0.3;
    public bool ExpandNicknames { get; set; } = false;
}

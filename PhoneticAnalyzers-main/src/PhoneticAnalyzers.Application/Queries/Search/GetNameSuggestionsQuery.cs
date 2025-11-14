using MediatR;

namespace PhoneticAnalyzers.Application.Queries.Search;

/// <summary>
/// Query to get name suggestions for autocomplete
/// </summary>
public record GetNameSuggestionsQuery : IRequest<GetNameSuggestionsResult>
{
    /// <summary>
    /// Partial name to search for
    /// </summary>
    public required string Prefix { get; init; }

    /// <summary>
    /// Maximum number of suggestions to return
    /// </summary>
    public int MaxSuggestions { get; init; } = 10;
}

/// <summary>
/// Result containing name suggestions
/// </summary>
public record GetNameSuggestionsResult
{
    /// <summary>
    /// List of suggested names
    /// </summary>
    public required List<string> Suggestions { get; init; }
}

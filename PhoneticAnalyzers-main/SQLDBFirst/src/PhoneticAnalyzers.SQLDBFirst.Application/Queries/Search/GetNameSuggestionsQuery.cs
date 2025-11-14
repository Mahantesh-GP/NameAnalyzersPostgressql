using MediatR;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Queries.Search;

/// <summary>
/// Query to get name suggestions for autocomplete
/// </summary>
public class GetNameSuggestionsQuery : IRequest<GetNameSuggestionsResult>
{
    /// <summary>
    /// Gets or sets the prefix to search for
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of suggestions to return
    /// </summary>
    public int MaxSuggestions { get; set; } = 10;
}

/// <summary>
/// Result containing name suggestions
/// </summary>
public class GetNameSuggestionsResult
{
    /// <summary>
    /// Gets or sets the list of suggested names
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

using PhoneticAnalyzers.NativeApi.Models;

namespace PhoneticAnalyzers.NativeApi.Services;

/// <summary>
/// Interface for native database operations
/// </summary>
public interface INativeDatabaseService
{
    /// <summary>
    /// Ingests a single person using the native SQL function
    /// </summary>
    Task<IngestPersonResult> IngestPersonAsync(IngestPersonRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests multiple persons in batch
    /// </summary>
    Task<BatchIngestResult> BatchIngestAsync(List<IngestPersonRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches persons using the native SQL function
    /// </summary>
    Task<SearchResponse> SearchPersonsAsync(SearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a person by ID
    /// </summary>
    Task<SearchResultDto?> GetPersonByIdAsync(long personId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests database connectivity
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of counties with person counts
    /// </summary>
    Task<List<CountyInfo>> GetCountiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets name suggestions based on prefix
    /// </summary>
    Task<List<string>> GetNameSuggestionsAsync(string prefix, int maxSuggestions, CancellationToken cancellationToken = default);
}

public class CountyInfo
{
    public int CountyId { get; set; }
    public string County { get; set; } = string.Empty;
    public string CountyName { get; set; } = string.Empty;
}

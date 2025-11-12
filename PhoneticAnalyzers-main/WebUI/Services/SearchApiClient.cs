using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.WebUI.Models;
using System.Net.Http.Json;

namespace PhoneticAnalyzers.WebUI.Services;

/// <summary>
/// Service for interacting with the Phonetic Analyzers Search API
/// </summary>
public interface ISearchApiClient
{
    Task<AdvancedSearchResponse?> AdvancedSearchAsync(AdvancedSearchRequest request, CancellationToken cancellationToken = default);
    Task<BulkSearchResponse?> BulkSearchAsync(BulkSearchRequest request, CancellationToken cancellationToken = default);
    Task<List<CountyInfo>> GetCountiesAsync(CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of the Search API client
/// </summary>
public class SearchApiClient : ISearchApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchApiClient> _logger;

    public SearchApiClient(HttpClient httpClient, ILogger<SearchApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AdvancedSearchResponse?> AdvancedSearchAsync(
        AdvancedSearchRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing advanced search for: {QueryName}", request.QueryName);

            var response = await _httpClient.PostAsJsonAsync(
                "api/search/advanced", 
                request, 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AdvancedSearchResponse>(
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Advanced search completed. Found {ResultCount} matches in {ExecutionTime}ms",
                result?.TotalMatches ?? 0,
                result?.ExecutionTime ?? 0);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during advanced search for: {QueryName}", request.QueryName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during advanced search for: {QueryName}", request.QueryName);
            throw;
        }
    }

    public async Task<BulkSearchResponse?> BulkSearchAsync(
        BulkSearchRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing bulk search for {Count} names", request.SearchTerms.Count);

            var response = await _httpClient.PostAsJsonAsync(
                "api/search/bulk", 
                request, 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BulkSearchResponse>(
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Bulk search completed. {TotalSearches} searches in {TotalTime}ms",
                result?.TotalSearches ?? 0,
                result?.TotalExecutionTime ?? 0);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during bulk search");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during bulk search");
            throw;
        }
    }

    public async Task<List<CountyInfo>> GetCountiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching counties list");

            var response = await _httpClient.GetAsync("api/counties", cancellationToken);
            response.EnsureSuccessStatusCode();

            var counties = await response.Content.ReadFromJsonAsync<List<CountyInfo>>(
                cancellationToken: cancellationToken);

            _logger.LogInformation("Fetched {Count} counties", counties?.Count ?? 0);

            return counties ?? new List<CountyInfo>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching counties");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching counties");
            throw;
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/search/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return false;
        }
    }
}

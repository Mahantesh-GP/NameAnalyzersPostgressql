using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.WebUI.Models;

namespace PhoneticAnalyzers.WebUI.Services;

public interface IIngestionApiClient
{
    Task<BatchIngestResponse?> BatchIngestAsync(BatchIngestRequest request, CancellationToken cancellationToken = default);
}

public class IngestionApiClient(HttpClient httpClient, ILogger<IngestionApiClient> logger) : IIngestionApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<IngestionApiClient> _logger = logger;

    public async Task<BatchIngestResponse?> BatchIngestAsync(BatchIngestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Posting batch ingest with {Count} persons", request.Persons.Count);
            var response = await _httpClient.PostAsJsonAsync("api/ingest/batch", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<BatchIngestResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Batch ingest completed: {Success}/{Total}", result?.Successful ?? 0, result?.TotalProcessed ?? 0);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during batch ingest");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during batch ingest");
            throw;
        }
    }
}

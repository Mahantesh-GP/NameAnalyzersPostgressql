using Microsoft.Extensions.Configuration;
using Npgsql;
using PhoneticAnalyzers.NativeApi.Models;
using System.Diagnostics;

namespace PhoneticAnalyzers.NativeApi.Services;

/// <summary>
/// Native database service using raw SQL and stored functions
/// </summary>
public class NativeDatabaseService : INativeDatabaseService
{
    private readonly string _connectionString;

    public NativeDatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
    }

    /// <inheritdoc/>
    public async Task<IngestPersonResult> IngestPersonAsync(IngestPersonRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(
                "SELECT ingest_person($1, $2, $3, $4)",
                connection);

            cmd.Parameters.AddWithValue(request.ExternalId);
            cmd.Parameters.AddWithValue(request.FullName);
            cmd.Parameters.AddWithValue(request.County ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue(request.Flag.ToString());

            var personId = (long)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0L);

            return new IngestPersonResult
            {
                PersonId = personId,
                ExternalId = request.ExternalId,
                FullName = request.FullName,
                Success = personId > 0,
                Message = personId > 0 ? "Person ingested successfully" : "Failed to ingest person"
            };
        }
        catch (Exception ex)
        {
            return new IngestPersonResult
            {
                ExternalId = request.ExternalId,
                FullName = request.FullName,
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<BatchIngestResult> BatchIngestAsync(List<IngestPersonRequest> requests, CancellationToken cancellationToken = default)
    {
        var result = new BatchIngestResult
        {
            TotalProcessed = requests.Count
        };

        foreach (var request in requests)
        {
            var ingestResult = await IngestPersonAsync(request, cancellationToken);
            result.Results.Add(ingestResult);

            if (ingestResult.Success)
                result.SuccessCount++;
            else
                result.FailureCount++;
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<SearchResponse> SearchPersonsAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<SearchResultDto>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM search_persons($1, $2, $3)",
                connection);

            cmd.Parameters.AddWithValue(request.QueryName);
            cmd.Parameters.AddWithValue(request.MaxResults);
            cmd.Parameters.AddWithValue(request.MinSimilarity);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var county = reader.IsDBNull(6) ? null : reader.GetString(6);
                System.Text.Json.JsonElement? metadata = null;
                
                if (!reader.IsDBNull(8))
                {
                    var metadataJson = reader.GetString(8);
                    if (!string.IsNullOrEmpty(metadataJson))
                    {
                        metadata = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(metadataJson);
                    }
                }
                
                results.Add(new SearchResultDto
                {
                    PersonId = reader.GetInt64(0),
                    FullName = reader.GetString(1),
                    MatchType = reader.GetString(2),
                    SimilarityScore = reader.GetDouble(3),
                    MatchedField = reader.GetString(4),
                    MatchedValue = reader.GetString(5),
                    County = county,
                    CountyName = county,
                    Flag = reader.IsDBNull(7) ? null : reader.GetString(7),
                    MatchMetadata = metadata
                });
            }
        }
        catch (Exception ex)
        {
            // Log exception if needed
            Console.WriteLine($"Search error: {ex.Message}");
        }

        stopwatch.Stop();

        return new SearchResponse
        {
            QueryName = request.QueryName,
            Results = results,
            TotalResults = results.Count,
            ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
        };
    }

    /// <inheritdoc/>
    public async Task<SearchResultDto?> GetPersonByIdAsync(long personId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(
                "SELECT person_id, full_name FROM person WHERE person_id = $1",
                connection);

            cmd.Parameters.AddWithValue(personId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return new SearchResultDto
                {
                    PersonId = reader.GetInt64(0),
                    FullName = reader.GetString(1),
                    MatchType = "Direct",
                    SimilarityScore = 1.0,
                    MatchedField = "PersonId",
                    MatchedValue = personId.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetById error: {ex.Message}");
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<CountyInfo>> GetCountiesAsync(CancellationToken cancellationToken = default)
    {
        var counties = new List<CountyInfo>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY county) as county_id,
                    county as county_name,
                    COUNT(*) as person_count
                FROM person
                WHERE county IS NOT NULL
                GROUP BY county
                ORDER BY county",
                connection);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                counties.Add(new CountyInfo
                {
                    CountyId = reader.GetInt32(0),
                    County = reader.GetString(1),
                    CountyName = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCounties error: {ex.Message}");
        }

        return counties;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetNameSuggestionsAsync(string prefix, int maxSuggestions, CancellationToken cancellationToken = default)
    {
        var suggestions = new List<string>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT full_name
                FROM person
                WHERE normalized_name LIKE $1 || '%'
                ORDER BY full_name
                LIMIT $2",
                connection);

            cmd.Parameters.AddWithValue(prefix.ToUpper());
            cmd.Parameters.AddWithValue(maxSuggestions);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                suggestions.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetNameSuggestions error: {ex.Message}");
        }

        return suggestions;
    }
}

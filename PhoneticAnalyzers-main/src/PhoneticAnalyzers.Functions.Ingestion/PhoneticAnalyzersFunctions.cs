using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.Application.Commands.Ingestion;
using PhoneticAnalyzers.Application.Queries.Search;
using PhoneticAnalyzers.Domain.Entities;
using MediatR;
using System.Net;
using System.Text.Json;

namespace PhoneticAnalyzers.Functions.Ingestion;

/// <summary>
/// Azure Functions for the PhoneticAnalyzers ingestion and search operations
/// </summary>
public class PhoneticAnalyzersFunctions
{
    private readonly ILogger<PhoneticAnalyzersFunctions> _logger;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the PhoneticAnalyzersFunctions class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="mediator">The mediator instance</param>
    public PhoneticAnalyzersFunctions(ILogger<PhoneticAnalyzersFunctions> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [Function("HealthCheck")]
    public async Task<HttpResponseData> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            message = "Ingestion Function App is running"
        }, ct);

        return response;
    }

    /// <summary>
    /// Simple test endpoint that doesn't require database
    /// </summary>
    [Function("Test")]
    public async Task<HttpResponseData> Test(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Test endpoint called");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            message = "Test endpoint working!",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        }, ct);

        return response;
    }

    /// <summary>
    /// Check database connection and table status
    /// </summary>
    [Function("DbCheck")]
    public async Task<HttpResponseData> DbCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "db-check")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Database check requested");

        try
        {
            // This will be injected via DI, so it will use your connection string
            using var scope = req.FunctionContext.InstanceServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PhoneticAnalyzers.Infrastructure.Persistence.PhoneticAnalyzersDbContext>();
            
            // Test basic connection
            var canConnect = await context.Database.CanConnectAsync(ct);
            
            var tableChecks = new List<object>();
            
            if (canConnect)
            {
                // Check if tables exist
                try
                {
                    var personCount = await context.Persons.CountAsync(ct);
                    tableChecks.Add(new { table = "Persons", exists = true, count = personCount });
                }
                catch
                {
                    tableChecks.Add(new { table = "Persons", exists = false, count = 0 });
                }

                try
                {
                    var variantCount = await context.BeiderMorseVariants.CountAsync(ct);
                    tableChecks.Add(new { table = "BeiderMorseVariants", exists = true, count = variantCount });
                }
                catch
                {
                    tableChecks.Add(new { table = "BeiderMorseVariants", exists = false, count = 0 });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                canConnect = canConnect,
                databaseExists = canConnect,
                tables = tableChecks,
                timestamp = DateTime.UtcNow,
                connectionString = "Host=" + (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")?.Split(';')[0]?.Split('=')[1] ?? "Unknown")
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database check failed");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await errorResponse.WriteAsJsonAsync(new
            {
                canConnect = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            }, ct);

            return errorResponse;
        }
    }

    /// <summary>
    /// Ingest a single person
    /// </summary>
    [Function("IngestPerson")]
    public async Task<HttpResponseData> IngestPerson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ingest")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Person ingestion requested");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
            var command = JsonSerializer.Deserialize<IngestPersonCommand>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (command == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" }, ct);
                return badResponse;
            }

            var result = await _mediator.Send(command, ct);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new
            {
                personId = result.PersonId,
                message = "Person ingested successfully",
                wasCreated = result.WasCreated,
                phoneticCodes = new
                {
                    primary = result.PhoneticEncoding.PrimaryDoubleMetaphone,
                    alternate = result.PhoneticEncoding.AlternateDoubleMetaphone,
                    beiderMorseCodes = result.PhoneticEncoding.BeiderMorseCodes
                },
                warnings = result.Warnings
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting person");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Internal server error",
                message = ex.Message
            }, ct);

            return errorResponse;
        }
    }

    /// <summary>
    /// Batch ingest multiple persons
    /// </summary>
    [Function("BatchIngestPersons")]
    public async Task<HttpResponseData> BatchIngestPersons(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ingest/batch")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Batch person ingestion requested");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
            var batchRequest = JsonSerializer.Deserialize<BatchIngestRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (batchRequest?.Persons == null || !batchRequest.Persons.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "No persons provided for batch ingestion" }, ct);
                return badResponse;
            }

            var results = new List<object>();
            var errors = new List<object>();

            foreach (var personData in batchRequest.Persons)
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Batch ingestion cancelled by client.");
                    break;
                }
                try
                {
                    var command = new IngestPersonCommand
                    {
                        ExternalId = personData.ExternalId,
                        FullName = personData.FullName,
                        ExpandNicknames = personData.ExpandNicknames ?? true
                    };

                    var result = await _mediator.Send(command, ct);
                    results.Add(new
                    {
                        externalId = personData.ExternalId,
                        personId = result.PersonId,
                        status = "success"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ingesting person with ExternalId: {ExternalId}", personData.ExternalId);
                    errors.Add(new
                    {
                        externalId = personData.ExternalId,
                        error = ex.Message,
                        status = "failed"
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                totalProcessed = batchRequest.Persons.Count(),
                successful = results.Count,
                failed = errors.Count,
                results = results,
                errors = errors
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch ingestion");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Internal server error",
                message = ex.Message
            }, ct);

            return errorResponse;
        }
    }

    /// <summary>
    /// Search for persons by name using phonetic matching
    /// </summary>
    [Function("SearchPersons")]
    public async Task<HttpResponseData> SearchPersons(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "search")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Person search requested");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var name = query["name"];
            var maxResultsStr = query["maxResults"];
            var algorithmStr = query["algorithm"];

            if (string.IsNullOrWhiteSpace(name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Name parameter is required" }, ct);
                return badResponse;
            }

            var maxResults = 10; // default
            if (!string.IsNullOrWhiteSpace(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            {
                maxResults = Math.Max(1, Math.Min(100, parsed)); // limit between 1-100
            }

            var searchCommand = new SearchPersonsQuery
            {
                QueryName = name,
                MaxResults = maxResults
            };

            var searchResults = await _mediator.Send(searchCommand, ct);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                query = name,
                totalResults = searchResults.Matches.Count,
                maxResults = maxResults,
                executionTime = searchResults.ExecutionTime.TotalMilliseconds,
                results = searchResults.Matches.Select(p => new
                {
                    personId = p.PersonId,
                    externalId = p.ExternalId,
                    fullName = p.FullName,
                    normalizedName = p.NormalizedName,
                    similarityScore = p.SimilarityScore,
                    matchType = p.MatchType.ToString(),
                    phoneticCodes = p.PhoneticCodes != null ? new
                    {
                        doubleMetaphone = new
                        {
                            primary = p.PhoneticCodes.PrimaryDoubleMetaphone,
                            alternate = p.PhoneticCodes.AlternateDoubleMetaphone
                        },
                        beiderMorse = p.PhoneticCodes.BeiderMorseCodes
                    } : null
                })
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching persons");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Internal server error",
                message = ex.Message
            }, ct);

            return errorResponse;
        }
    }

    /// <summary>
    /// Get person by ID
    /// </summary>
    [Function("GetPerson")]
    public async Task<HttpResponseData> GetPerson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "person/{id}")] HttpRequestData req,
        string id,
        CancellationToken ct)
    {
        _logger.LogInformation("Get person requested for ID: {Id}", id);

        try
        {
            if (!Guid.TryParse(id, out var personId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid person ID format" }, ct);
                return badResponse;
            }

            // You would implement a GetPersonQuery here
            // For now, return a placeholder
            var response = req.CreateResponse(HttpStatusCode.NotImplemented);
            await response.WriteAsJsonAsync(new
            {
                error = "Get person by ID not yet implemented",
                personId = personId
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting person");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Internal server error",
                message = ex.Message
            }, ct);

            return errorResponse;
        }
    }
}

/// <summary>
/// Request model for batch ingestion
/// </summary>
public class BatchIngestRequest
{
    /// <summary>
    /// Gets or sets the collection of persons to ingest
    /// </summary>
    public IEnumerable<PersonIngestData> Persons { get; set; } = new List<PersonIngestData>();
}

/// <summary>
/// Individual person data for batch ingestion
/// </summary>
public class PersonIngestData
{
    /// <summary>
    /// Gets or sets the external identifier
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the first name (optional)
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Gets or sets the last name (optional)
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Gets or sets whether to expand nicknames
    /// </summary>
    public bool? ExpandNicknames { get; set; }
}
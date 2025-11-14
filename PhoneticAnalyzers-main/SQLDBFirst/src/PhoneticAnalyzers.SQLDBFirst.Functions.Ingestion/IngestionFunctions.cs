using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.SQLDBFirst.Application.Commands;
using PhoneticAnalyzers.SQLDBFirst.Application.DTOs;

namespace PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion;

/// <summary>
/// Azure Functions for person ingestion operations.
/// Database-First implementation on port 7073.
/// </summary>
public class IngestionFunctions
{
    private readonly IMediator _mediator;
    private readonly ILogger<IngestionFunctions> _logger;

    public IngestionFunctions(IMediator mediator, ILogger<IngestionFunctions> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("IngestPerson")]
    public async Task<HttpResponseData> IngestPerson(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "persons")] HttpRequestData req)
    {
        _logger.LogInformation("IngestPerson function triggered (Database-First)");

        try
        {
            var dto = await req.ReadFromJsonAsync<PersonIngestDto>();
            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badRequest;
            }

            var command = new IngestPersonCommand
            {
                ExternalId = dto.ActualExternalId,
                FullName = dto.FullName,
                County = dto.County,
                ExpandNicknames = dto.ExpandNicknames
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new
                {
                    personId = result.Data,
                    messages = result.Messages,
                    warnings = result.Warnings
                });
                return response;
            }
            else
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new
                {
                    errors = result.Errors,
                    messages = result.Messages
                });
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting person");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("BatchIngest")]
    public async Task<HttpResponseData> BatchIngest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ingest/batch")] HttpRequestData req)
    {
        _logger.LogInformation("BatchIngest function triggered (Database-First)");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("Received request body (first 500 chars): {Body}", requestBody.Length > 500 ? requestBody.Substring(0, 500) : requestBody);
            
            var batchRequest = JsonSerializer.Deserialize<BatchIngestRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (batchRequest?.Persons == null || !batchRequest.Persons.Any())
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "No persons provided" });
                return badRequest;
            }

            _logger.LogInformation("Processing {Count} persons. First person - Id: '{Id}', ExternalId: '{ExternalId}', ActualExternalId: '{Actual}'", 
                batchRequest.Persons.Count, 
                batchRequest.Persons.FirstOrDefault()?.Id,
                batchRequest.Persons.FirstOrDefault()?.ExternalId,
                batchRequest.Persons.FirstOrDefault()?.ActualExternalId);

            var results = new List<object>();
            var errors = new List<object>();

            foreach (var personDto in batchRequest.Persons)
            {
                try
                {
                    var command = new IngestPersonCommand
                    {
                        ExternalId = personDto.ActualExternalId,
                        FullName = personDto.FullName,
                        County = personDto.County,
                        ExpandNicknames = personDto.ExpandNicknames
                    };

                    var result = await _mediator.Send(command);

                    if (result.IsSuccess)
                    {
                        results.Add(new
                        {
                            externalId = personDto.ActualExternalId,
                            personId = result.Data,
                            status = "success"
                        });
                    }
                    else
                    {
                        errors.Add(new
                        {
                            externalId = personDto.ActualExternalId,
                            error = string.Join(", ", result.Errors),
                            status = "failed"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ingesting person with ExternalId: {ExternalId}", personDto.ActualExternalId);
                    errors.Add(new
                    {
                        externalId = personDto.ActualExternalId,
                        error = ex.Message,
                        status = "failed"
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                totalProcessed = batchRequest.Persons.Count,
                successful = results.Count,
                failed = errors.Count,
                results = results,
                errors = errors,
                enrichment = (object?)null
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch ingestion");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("Health")]
    public HttpResponseData Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check (Database-First Ingestion)");
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteString("Database-First Ingestion Functions - Healthy");
        return response;
    }

    [Function("DiagnosticsInfo")]
    public HttpResponseData DiagnosticsInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "diagnostics")] HttpRequestData req)
    {
        _logger.LogInformation("Diagnostics info requested (Database-First)");
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        var diagnostics = new
        {
            service = "PhoneticAnalyzers Database-First Ingestion",
            version = "1.0.0",
            approach = "Database-First",
            port = 7073,
            database = "phonetic_db_dbfirst",
            endpoints = new[]
            {
                "POST /api/persons - Ingest single person",
                "POST /api/persons/batch - Batch ingest multiple persons",
                "GET /api/health - Health check",
                "GET /api/diagnostics - This diagnostics endpoint"
            },
            features = new[]
            {
                "Nickname variant generation",
                "Double Metaphone encoding",
                "Beider-Morse encoding",
                "Trigram similarity matching",
                "Bidirectional nickname lookup"
            }
        };

        response.WriteAsJsonAsync(diagnostics);
        return response;
    }
}

/// <summary>
/// Request model for batch ingestion
/// </summary>
public class BatchIngestRequest
{
    public List<PersonIngestDto> Persons { get; set; } = new();
}

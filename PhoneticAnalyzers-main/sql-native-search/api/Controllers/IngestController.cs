using Microsoft.AspNetCore.Mvc;
using PhoneticAnalyzers.NativeApi.Models;
using PhoneticAnalyzers.NativeApi.Services;

namespace PhoneticAnalyzers.NativeApi.Controllers;

/// <summary>
/// Controller for person ingestion using native SQL functions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private readonly INativeDatabaseService _dbService;
    private readonly ILogger<IngestController> _logger;

    public IngestController(INativeDatabaseService dbService, ILogger<IngestController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Ingests a single person
    /// </summary>
    /// <param name="request">Person data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ingestion result</returns>
    [HttpPost]
    public async Task<ActionResult<IngestPersonResult>> IngestPerson(
        [FromBody] IngestPersonRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting person: {ExternalId}", request.ExternalId);

        var result = await _dbService.IngestPersonAsync(request, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Ingests multiple persons in batch
    /// </summary>
    /// <param name="request">Batch of persons</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch ingestion result</returns>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchIngestResult>> BatchIngest(
        [FromBody] BatchIngestRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Batch ingesting {Count} persons", request.Persons.Count);

        var result = await _dbService.BatchIngestAsync(request.Persons, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public async Task<ActionResult> HealthCheck(CancellationToken cancellationToken)
    {
        var canConnect = await _dbService.TestConnectionAsync(cancellationToken);

        if (!canConnect)
            return StatusCode(503, new { status = "Unhealthy", message = "Cannot connect to database" });

        return Ok(new { status = "Healthy", message = "Database connection OK" });
    }
}

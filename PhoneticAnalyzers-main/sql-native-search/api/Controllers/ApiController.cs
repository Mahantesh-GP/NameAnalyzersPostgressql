using Microsoft.AspNetCore.Mvc;
using PhoneticAnalyzers.NativeApi.Services;

namespace PhoneticAnalyzers.NativeApi.Controllers;

/// <summary>
/// Controller for API utility endpoints
/// </summary>
[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly INativeDatabaseService _dbService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(INativeDatabaseService dbService, ILogger<ApiController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Gets list of counties with person counts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of counties</returns>
    [HttpGet("counties")]
    public async Task<ActionResult<List<CountyInfo>>> GetCounties(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting counties list");

        var counties = await _dbService.GetCountiesAsync(cancellationToken);

        return Ok(counties);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}

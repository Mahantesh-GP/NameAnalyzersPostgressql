using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Infrastructure.Persistence;

namespace PhoneticAnalyzers.Functions.Ingestion;

/// <summary>
/// Diagnostics endpoints to help troubleshoot database connectivity and run migrations on-demand.
/// </summary>
public class DiagnosticsFunctions
{
    private readonly PhoneticAnalyzersDbContext _db;
    private readonly ILogger<DiagnosticsFunctions> _logger;
    private readonly IConfiguration _config;
    private readonly HealthCheckService _healthChecks;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsFunctions"/> class.
    /// </summary>
    /// <param name="db">EF Core database context.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="config">Application configuration.</param>
    /// <param name="healthChecks">Health check service.</param>
    public DiagnosticsFunctions(
        PhoneticAnalyzersDbContext db,
        ILogger<DiagnosticsFunctions> logger,
        IConfiguration config,
        HealthCheckService healthChecks)
    {
        _db = db;
        _logger = logger;
        _config = config;
        _healthChecks = healthChecks;
    }

    /// <summary>
    /// Quick DB connectivity probe. Opens a connection and returns basic server info.
    /// GET /api/debug/db/ping
    /// </summary>
    [Function("DbPing")]
    public async Task<HttpResponseData> DbPing(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "debug/db/ping")] HttpRequestData req,
        FunctionContext ctx)
    {
        var response = req.CreateResponse();
        response.Headers.Add("Content-Type", "application/json");

        var conn = _db.Database.GetDbConnection();
        var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var masked = Mask(_config.GetConnectionString("DefaultConnection") ?? string.Empty);

        try
        {
            await _db.Database.OpenConnectionAsync(timeoutCts.Token);

            // minimal info without taking a dependency on Npgsql types
            var serverVersion = conn.ServerVersion;
            var databaseName = _db.Database.GetDbConnection().Database;

            // Try to fetch SSL and identity info
            string? sslSetting = null;
            string? currentDb = null;
            string? currentUser = null;
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "select current_setting('ssl');";
                var sslObj = await cmd.ExecuteScalarAsync(timeoutCts.Token);
                sslSetting = sslObj?.ToString();

                cmd.CommandText = "select current_database();";
                currentDb = (await cmd.ExecuteScalarAsync(timeoutCts.Token))?.ToString();

                cmd.CommandText = "select current_user;";
                currentUser = (await cmd.ExecuteScalarAsync(timeoutCts.Token))?.ToString();
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Auxiliary info queries failed");
            }

            var payload = new
            {
                ok = true,
                message = "Database connection established",
                connectionString = masked,
                serverVersion,
                databaseName,
                ssl = sslSetting,
                currentDb,
                currentUser
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(payload));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DbPing failed. Connection: {ConnectionString}", masked);
            var payload = new
            {
                ok = false,
                message = "Database connection failed",
                connectionString = masked,
                error = ex.Message,
                type = ex.GetType().FullName,
                stack = ex.StackTrace
            };
            await response.WriteStringAsync(JsonSerializer.Serialize(payload));
            response.StatusCode = HttpStatusCode.ServiceUnavailable;
            return response;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Run EF Core migrations on demand.
    /// POST /api/debug/db/migrate
    /// </summary>
    [Function("DbMigrate")] 
    public async Task<HttpResponseData> DbMigrate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "debug/db/migrate")] HttpRequestData req)
    {
        var response = req.CreateResponse();
        response.Headers.Add("Content-Type", "application/json");

        var masked = Mask(_config.GetConnectionString("DefaultConnection") ?? string.Empty);
        try
        {
            var pending = await _db.Database.GetPendingMigrationsAsync();
            await _db.Database.MigrateAsync();

            var payload = new
            {
                ok = true,
                message = "Migrations applied successfully",
                applied = pending.ToArray(),
                connectionString = masked
            };
            await response.WriteStringAsync(JsonSerializer.Serialize(payload));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            var payload = new
            {
                ok = false,
                message = "Migration failed",
                connectionString = masked,
                error = ex.Message,
                type = ex.GetType().FullName,
                stack = ex.StackTrace
            };
            await response.WriteStringAsync(JsonSerializer.Serialize(payload));
            response.StatusCode = HttpStatusCode.InternalServerError;
            return response;
        }
    }

    /// <summary>
    /// Expose aggregated health status, including the DB health check already configured.
    /// GET /api/debug/health
    /// </summary>
    [Function("Health")] 
    public async Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "debug/health")] HttpRequestData req)
    {
        var report = await _healthChecks.CheckHealthAsync();
        var response = req.CreateResponse(report.Status == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        response.Headers.Add("Content-Type", "application/json");

        var payload = new
        {
            status = report.Status.ToString(),
            entries = report.Entries.ToDictionary(k => k.Key, v => new
            {
                status = v.Value.Status.ToString(),
                description = v.Value.Description,
                data = v.Value.Data
            })
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(payload));
        return response;
    }

    private static string Mask(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
        var patterns = new[] { @"Password\s*=\s*[^;]+", @"Pwd\s*=\s*[^;]+", @"password\s*=\s*[^;]+" };
        var result = connectionString;
        foreach (var pattern in patterns)
            result = System.Text.RegularExpressions.Regex.Replace(result, pattern, m =>
            {
                var key = m.Value.Split('=')[0];
                return $"{key}=***MASKED***";
            }, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return result;
    }
}

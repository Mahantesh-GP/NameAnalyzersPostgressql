using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Services.Phonetic;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Infrastructure.Persistence;
using PhoneticAnalyzers.Infrastructure.Persistence.Repositories;
using FluentValidation;
using PhoneticAnalyzers.Application.Behaviors;
using MediatR;

namespace PhoneticAnalyzers.Functions.Search;

/// <summary>
/// Program entry point for the PhoneticAnalyzers Search Function App
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    public static async Task Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                // Add validation exception middleware
                builder.UseMiddleware<PhoneticAnalyzers.Functions.Search.Middleware.ValidationExceptionMiddleware>();
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                var environment = context.HostingEnvironment.EnvironmentName;

                // Load standard appsettings plus local.settings.json for Functions local dev
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                      .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Application Insights
                services.AddApplicationInsightsTelemetryWorkerService();
                services.ConfigureFunctionsApplicationInsights();

                // Database context
                var connectionString = ResolveDefaultConnectionString(context.Configuration)
                                    ?? throw new InvalidOperationException("Database connection string is required");

                // Log connection string for debugging (with password masked)
                var maskedConnectionString = MaskConnectionStringPassword(connectionString);
                WriteConnectionStringHighlight($"[CONNECTION] {maskedConnectionString}");

                services.AddDbContext<PhoneticAnalyzers.Infrastructure.Persistence.PhoneticAnalyzersDbContext>(options =>
                {
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(PhoneticAnalyzers.Infrastructure.Persistence.PhoneticAnalyzersDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    });

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }
                });

                // Repositories
                services.AddScoped<PhoneticAnalyzers.Domain.Repositories.IPersonRepository, PhoneticAnalyzers.Infrastructure.Persistence.Repositories.PersonRepository>();

                // Phonetic encoding services
                services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.DoubleMetaphoneEncoder>();
                services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.BeiderMorseEncoder>();
                services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncoderFactory, PhoneticAnalyzers.Application.Services.Phonetic.PhoneticEncoderFactory>();
                services.AddScoped<PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncodingService, PhoneticAnalyzers.Application.Services.Phonetic.PhoneticEncodingService>();
                services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.INicknameService, PhoneticAnalyzers.Application.Services.Phonetic.InMemoryNicknameService>();

                // MediatR for CQRS
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncodingService).Assembly));

                // FluentValidation: register validators & pipeline behavior
                services.AddValidatorsFromAssembly(typeof(PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncodingService).Assembly);
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

                // Health checks
                services.AddHealthChecks()
                    .AddDbContextCheck<PhoneticAnalyzers.Infrastructure.Persistence.PhoneticAnalyzersDbContext>("database")
                    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Search function app is healthy"));
            })
            .Build();

        await host.RunAsync();
    }

    static string? ResolveDefaultConnectionString(IConfiguration configuration)
    {
        // Try common locations in order, supporting Functions local.settings.json (Values section)
        return
            configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? configuration["Values:ConnectionStrings__DefaultConnection"]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    }

    static string MaskConnectionStringPassword(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Replace password value with asterisks
        var patterns = new[]
        {
            @"Password\s*=\s*[^;]+",
            @"Pwd\s*=\s*[^;]+",
            @"password\s*=\s*[^;]+"
        };

        var result = connectionString;
        foreach (var pattern in patterns)
        {
            result = System.Text.RegularExpressions.Regex.Replace(
                result, 
                pattern, 
                match => 
                {
                    var keyPart = match.Value.Split('=')[0];
                    return $"{keyPart}=***MASKED***";
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return result;
    }

    static void WriteConnectionStringHighlight(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}
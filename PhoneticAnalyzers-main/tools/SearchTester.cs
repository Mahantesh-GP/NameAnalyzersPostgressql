using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Queries.Search;
using PhoneticAnalyzers.Infrastructure.Persistence;
using MediatR;
using System.Text.Json;

namespace PhoneticAnalyzers.Tools;

/// <summary>
/// Console application to test search functionality directly
/// </summary>
public class SearchTester
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SearchTester>>();

        try
        {
            logger.LogInformation("Testing search functionality...");

            // Test with John Smith
            var query = new AdvancedSearchQuery
            {
                QueryName = "John Smith",
                MaxResults = 10
            };

            logger.LogInformation("Searching for: {QueryName}", query.QueryName);
            var result = await mediator.Send(query);

            logger.LogInformation("Search completed. Total matches: {TotalMatches}", result.TotalMatches);
            logger.LogInformation("Returned results: {ResultCount}", result.Results.Count);

            if (result.Results.Any())
            {
                logger.LogInformation("Search results:");
                foreach (var person in result.Results)
                {
                    logger.LogInformation("  - {ExternalId}: {FullName} ({County})", 
                        person.ExternalId, person.FullName, person.County);
                }
            }
            else
            {
                logger.LogWarning("No results found!");
            }

            // Test with a partial name from our data
            query = new AdvancedSearchQuery
            {
                QueryName = "Maria Rodriguez",
                MaxResults = 10
            };

            logger.LogInformation("\nSearching for: {QueryName}", query.QueryName);
            var result2 = await mediator.Send(query);

            logger.LogInformation("Search completed. Total matches: {TotalMatches}", result2.TotalMatches);
            logger.LogInformation("Returned results: {ResultCount}", result2.Results.Count);

            if (result2.Results.Any())
            {
                logger.LogInformation("Search results:");
                foreach (var person in result2.Results)
                {
                    logger.LogInformation("  - {ExternalId}: {FullName} ({County})", 
                        person.ExternalId, person.FullName, person.County);
                }
            }
            else
            {
                logger.LogWarning("No results found!");
            }

            logger.LogInformation("Search testing completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during search testing");
            throw;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Add Entity Framework
                services.AddDbContext<PhoneticAnalyzersDbContext>(options =>
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                        "Host=localhost;Port=5432;Database=phonetic_analyzers_dev;Username=postgres;Password=postgres";
                    
                    options.UseNpgsql(connectionString);
                });

                // Add MediatR and Application layer services
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AdvancedSearchQuery).Assembly));
                
                // Register repositories
                services.Scan(scan => scan
                    .FromAssembliesOf(typeof(PhoneticAnalyzersDbContext))
                    .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
                    .AsImplementedInterfaces()
                    .WithScoped.Lifetime);

            });
}
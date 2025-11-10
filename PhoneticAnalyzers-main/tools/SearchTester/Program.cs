using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Queries.Search;
using PhoneticAnalyzers.Infrastructure.Persistence;
using MediatR;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Using connection string: {connectionString?.Substring(0, Math.Min(50, connectionString.Length))}...");

var services = new ServiceCollection();

// Add logging
services.AddLogging(builder => builder.AddConsole());

services.AddDbContext<PhoneticAnalyzersDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
services.AddScoped<PhoneticAnalyzers.Domain.Repositories.IPersonRepository, PhoneticAnalyzers.Infrastructure.Persistence.Repositories.PersonRepository>();

// Multilingual Search Repositories
services.AddScoped<PhoneticAnalyzers.Domain.Repositories.IPersonNameRepository, PhoneticAnalyzers.Infrastructure.Persistence.Repositories.PersonNameRepository>();
services.AddScoped<PhoneticAnalyzers.Domain.Repositories.INameAliasRepository, PhoneticAnalyzers.Infrastructure.Persistence.Repositories.NameAliasRepository>();
services.AddScoped<PhoneticAnalyzers.Domain.Repositories.INicknameMapRepository, PhoneticAnalyzers.Infrastructure.Persistence.Repositories.NicknameMapRepository>();
services.AddScoped<PhoneticAnalyzers.Domain.Repositories.INameAliasCacheRepository, PhoneticAnalyzers.Infrastructure.Persistence.Repositories.NameAliasCacheRepository>();

// Phonetic encoding services
services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.DoubleMetaphoneEncoder>();
services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.BeiderMorseEncoder>();
services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncoderFactory, PhoneticAnalyzers.Application.Services.Phonetic.PhoneticEncoderFactory>();
services.AddScoped<PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncodingService, PhoneticAnalyzers.Application.Services.Phonetic.PhoneticEncodingService>();
services.AddSingleton<PhoneticAnalyzers.Application.Services.Phonetic.INicknameService, PhoneticAnalyzers.Application.Services.Phonetic.InMemoryNicknameService>();

// Text normalization service
services.AddScoped<PhoneticAnalyzers.Domain.Services.ITextNormalizationService, PhoneticAnalyzers.Application.Services.Text.TextNormalizationService>();

// Add MediatR
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SearchPersonsQuery).Assembly));

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var dbContext = scope.ServiceProvider.GetRequiredService<PhoneticAnalyzersDbContext>();

    Console.WriteLine("Testing database connection...");
    
    try 
    {
        var personCount = await dbContext.Persons.CountAsync();
        Console.WriteLine($"Database contains {personCount} persons");
        
        if (personCount > 0)
        {
            // Test search for "John Michael Smith" - exact match from our data
            Console.WriteLine("\n=== Testing search for 'John Michael Smith' ===");
            var query1 = new SearchPersonsQuery { QueryName = "John Michael Smith" };
            var result1 = await mediator.Send(query1);
            
            Console.WriteLine($"Search Results: {result1.Matches.Count} matches");
            Console.WriteLine($"Total candidates: {result1.TotalCandidates}");
            Console.WriteLine($"Execution time: {result1.ExecutionTime.TotalMilliseconds}ms");
            
            foreach (var person in result1.Matches.Take(5))
            {
                Console.WriteLine($"  - {person.FullName} (ID: {person.PersonId}, External: {person.ExternalId}, Score: {person.SimilarityScore:F2})");
            }

            // Test search for "Maria Elena Rodriguez" - exact match from our data
            Console.WriteLine("\n=== Testing search for 'Maria Elena Rodriguez' ===");
            var query2 = new SearchPersonsQuery { QueryName = "Maria Elena Rodriguez" };
            var result2 = await mediator.Send(query2);
            
            Console.WriteLine($"Search Results: {result2.Matches.Count} matches");
            Console.WriteLine($"Total candidates: {result2.TotalCandidates}");
            Console.WriteLine($"Execution time: {result2.ExecutionTime.TotalMilliseconds}ms");
            
            foreach (var person in result2.Matches.Take(5))
            {
                Console.WriteLine($"  - {person.FullName} (ID: {person.PersonId}, External: {person.ExternalId}, Score: {person.SimilarityScore:F2})");
            }
        }
        else 
        {
            Console.WriteLine("Database is empty - no persons to search");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
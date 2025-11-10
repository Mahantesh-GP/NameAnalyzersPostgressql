using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;
using System.Globalization;

namespace PhoneticAnalyzers.Tools;

/// <summary>
/// Console application to seed the database with sample mortgage data from CSV file
/// </summary>
public class DataSeeder
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PhoneticAnalyzersDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        try
        {
            logger.LogInformation("Starting database seeding process...");
            
            // Check if database is empty first
            var existingCount = await context.Persons.CountAsync();
            logger.LogInformation("Current person count in database: {Count}", existingCount);

            if (existingCount > 0)
            {
                logger.LogWarning("Database already contains {Count} persons. Do you want to clear the data first? (y/n)", existingCount);
                var response = Console.ReadLine()?.ToLower();
                if (response == "y" || response == "yes")
                {
                    logger.LogInformation("Clearing existing person data...");
                    context.Persons.RemoveRange(context.Persons);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Existing data cleared.");
                }
                else
                {
                    logger.LogInformation("Keeping existing data. New records will be added.");
                }
            }

            // Read and process CSV file
            var csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "sample_mortgage_data.csv");
            if (!File.Exists(csvFilePath))
            {
                logger.LogError("CSV file not found at: {FilePath}", csvFilePath);
                return;
            }

            logger.LogInformation("Reading CSV file from: {FilePath}", csvFilePath);
            var csvLines = await File.ReadAllLinesAsync(csvFilePath);
            
            if (csvLines.Length <= 1)
            {
                logger.LogError("CSV file is empty or only contains header");
                return;
            }

            // Skip header line and process data
            var dataLines = csvLines.Skip(1).ToArray();
            logger.LogInformation("Found {Count} records to process", dataLines.Length);

            var persons = new List<Person>();
            var processedCount = 0;

            foreach (var line in dataLines)
            {
                try
                {
                    var person = ParseCsvLineToPerson(line, logger);
                    if (person != null)
                    {
                        persons.Add(person);
                        processedCount++;

                        if (processedCount % 10 == 0)
                        {
                            logger.LogInformation("Processed {Count}/{Total} records...", processedCount, dataLines.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing CSV line: {Line}", line);
                }
            }

            logger.LogInformation("Successfully parsed {Count} persons from CSV", persons.Count);

            // Batch insert to database
            if (persons.Any())
            {
                logger.LogInformation("Inserting {Count} persons into database...", persons.Count);
                
                await context.Persons.AddRangeAsync(persons);
                var savedCount = await context.SaveChangesAsync();
                
                logger.LogInformation("Successfully inserted {Count} persons into database", savedCount);
            }

            // Verify insertion
            var finalCount = await context.Persons.CountAsync();
            logger.LogInformation("Final person count in database: {Count}", finalCount);

            // Show a few sample records
            var samplePersons = await context.Persons.Take(5).ToListAsync();
            logger.LogInformation("Sample persons in database:");
            foreach (var person in samplePersons)
            {
                logger.LogInformation("  - {ExternalId}: {FullName} ({County})", 
                    person.ExternalId.Value, person.FullName, person.County);
            }

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private static Person? ParseCsvLineToPerson(string csvLine, ILogger logger)
    {
        try
        {
            // Parse CSV line - handle quoted values and commas
            var values = ParseCsvLine(csvLine);
            
            if (values.Length < 7)
            {
                logger.LogWarning("Insufficient columns in CSV line: {Line}", csvLine);
                return null;
            }

            // Map CSV columns to Person properties
            // Expected format: Id,FullName,FirstName,LastName,County,CountyId,CountyName,...
            var id = values[0].Trim();
            var fullName = values[1].Trim();
            var firstName = values[2].Trim();
            var lastName = values[3].Trim();
            var county = values[4].Trim();
            var countyIdStr = values[5].Trim();
            var countyName = values[6].Trim();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(id) || 
                string.IsNullOrWhiteSpace(fullName) || 
                string.IsNullOrWhiteSpace(county) ||
                string.IsNullOrWhiteSpace(countyName))
            {
                logger.LogWarning("Missing required fields in CSV line: {Line}", csvLine);
                return null;
            }

            // Parse CountyId
            if (!int.TryParse(countyIdStr, out var countyId))
            {
                logger.LogWarning("Invalid CountyId '{CountyId}' in CSV line: {Line}", countyIdStr, csvLine);
                countyId = 0; // Default value
            }

            // Create External ID
            var externalId = ExternalId.Create(id);

            // Create Person entity
            var person = Person.Create(
                externalId: externalId,
                fullName: fullName,
                county: county,
                countyId: countyId,
                countyName: countyName,
                flag: RecordTypeFlag.Individual // Default to Individual for mortgage data
            );

            return person;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing CSV line: {Line}", csvLine);
            return null;
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        // Add the last field
        result.Add(currentField);

        return result.ToArray();
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
                        "Host=localhost;Port=5432;Database=phonetic_analyzers;Username=postgres;Password=postgres";
                    
                    options.UseNpgsql(connectionString);
                });
            });
}
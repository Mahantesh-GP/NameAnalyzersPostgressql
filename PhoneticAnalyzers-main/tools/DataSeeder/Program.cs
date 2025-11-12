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

        // Flags (non-interactive control)
        // --clear-existing : always delete existing rows first
        // --keep-existing  : never delete, append new rows
        // If neither flag specified and rows exist, prompt user.
        var clearExistingFlag = args.Any(a => a.Equals("--clear-existing", StringComparison.OrdinalIgnoreCase));
        var keepExistingFlag  = args.Any(a => a.Equals("--keep-existing", StringComparison.OrdinalIgnoreCase));

        // Ensure database exists (create if missing) before counting/seeding
        try
        {
            logger.LogInformation("Ensuring database exists / applying migrations (if any)...");
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EnsureCreated failed; continuing (migrations may handle creation). Attempting MigrateAsync...");
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception mex)
            {
                logger.LogError(mex, "Failed to create or migrate database; seeding cannot continue.");
                return;
            }
        }

        try
        {
            logger.LogInformation("Starting database seeding process...");
            
            // Check if database is empty first
            var existingCount = await context.Persons.CountAsync();
            logger.LogInformation("Current person count in database: {Count}", existingCount);

            if (existingCount > 0)
            {
                if (clearExistingFlag && keepExistingFlag)
                {
                    logger.LogWarning("Both --clear-existing and --keep-existing supplied; proceeding with clear.");
                }

                if (clearExistingFlag)
                {
                    logger.LogInformation("Clearing existing person data due to --clear-existing flag...");
                    context.Persons.RemoveRange(context.Persons);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Existing data cleared.");
                }
                else if (keepExistingFlag)
                {
                    logger.LogInformation("Keeping existing data due to --keep-existing flag. New records will be added.");
                }
                else
                {
                    logger.LogWarning("Database already contains {Count} persons. Clear first? (y/n)", existingCount);
                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (response is "y" or "yes")
                    {
                        logger.LogInformation("User chose to clear existing data...");
                        context.Persons.RemoveRange(context.Persons);
                        await context.SaveChangesAsync();
                        logger.LogInformation("Existing data cleared.");
                    }
                    else
                    {
                        logger.LogInformation("Retaining existing data; will append new records only.");
                    }
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

            // Gather existing external IDs to avoid duplicate key violations on unique index ix_person_external_id
            var existingExternalIds = new HashSet<string>(
                await context.Persons
                    .Select(p => p.ExternalId.Value)
                    .ToListAsync());

            var persons = new List<Person>();
            var processedCount = 0;
            var skippedDuplicates = 0;
            var skippedInvalid = 0;

            foreach (var line in dataLines)
            {
                try
                {
                    var values = ParseCsvLine(line);

                    // Basic sanity check on column count (expect at least 12 based on sample file)
                    if (values.Length < 12)
                    {
                        logger.LogWarning("Skipping line with insufficient columns ({Count}): {Line}", values.Length, line);
                        skippedInvalid++;
                        continue;
                    }

                    // Map actual CSV columns:
                    // 0: Id, 1: FullName, 2: FirstName, 3: LastName,
                    // 4: LoanAmount, 5: StreetAddress, 6: City, 7: State, 8: Zip,
                    // 9: CountyCode, 10: CountyNumericId, 11: CountyName, (then remaining fields...)
                    var id = values[0].Trim();
                    var fullName = values[1].Trim();
                    var countyCode = values[9].Trim();
                    var countyIdStr = values[10].Trim();
                    var countyName = values[11].Trim();

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(fullName))
                    {
                        logger.LogWarning("Skipping line with missing id/fullName: {Line}", line);
                        skippedInvalid++;
                        continue;
                    }

                    if (existingExternalIds.Contains(id))
                    {
                        skippedDuplicates++;
                        continue; // Skip duplicate
                    }

                    if (!int.TryParse(countyIdStr, out var countyId))
                    {
                        logger.LogWarning("Invalid CountyId '{CountyId}' in CSV line: {Line}", countyIdStr, line);
                        countyId = 0;
                    }

                    var externalId = ExternalId.Create(id);
                    var person = Person.Create(
                        externalId: externalId,
                        fullName: fullName,
                        county: countyCode,
                        countyId: countyId,
                        countyName: countyName,
                        flag: RecordTypeFlag.Individual);

                    persons.Add(person);
                    existingExternalIds.Add(id);
                    processedCount++;

                    if (processedCount % 10 == 0)
                    {
                        logger.LogInformation("Processed {Count}/{Total} records...", processedCount, dataLines.Length);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing CSV line: {Line}", line);
                    skippedInvalid++;
                }
            }

            logger.LogInformation("Successfully parsed {Count} new persons from CSV (Skipped duplicates: {Dup}, Invalid: {Invalid})", persons.Count, skippedDuplicates, skippedInvalid);

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

            // Seed nickname mappings
            logger.LogInformation("Seeding nickname mappings...");
            await NicknameSeeder.SeedNicknamesAsync(context, logger);

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    // Legacy ParseCsvLineToPerson removed - logic inlined in main loop with correct column mapping.

    private static string[] ParseCsvLine(string line)
    {
        // Simplified parser: the sample CSV does not contain embedded commas inside quoted fields
        // The previous state-machine parser was producing column misalignment, causing CountyId to map
        // to the street address column (e.g. logging 'Invalid CountyId "123 Oak Street"').
        // Using direct Split preserves the intended indices:
        // 0 Id, 1 FullName, 2 FirstName, 3 LastName, 4 LoanAmount, 5 PropertyAddress,
        // 6 City, 7 State, 8 ZipCode, 9 CountyCode, 10 CountyNumericId, 11 CountyName, ...
        return line.Split(',');
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
                        "Host=localhost;Port=5432;Database=PhoneticAnalyzersDb;Username=postgres;Password=postgres";

                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                    });
                });
            });
}
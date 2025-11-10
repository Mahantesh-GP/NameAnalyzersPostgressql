using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Services.LLM;
using PhoneticAnalyzers.Application.Services;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Domain.Enums;
using PhoneticAnalyzers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DemoApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 PhoneticAnalyzers LLM Demo");
        Console.WriteLine("============================");

        // Build configuration
        var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Build host
        var host = CreateHostBuilder(args, configuration).Build();
        
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            await RunDemo(services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed");
            Console.WriteLine($"❌ Demo failed: {ex.Message}");
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add memory cache
                services.AddMemoryCache();
                
                // Add in-memory database for demo
                services.AddDbContext<PhoneticAnalyzersDbContext>(options =>
                    options.UseInMemoryDatabase("DemoDb"));

                // Add repositories (using in-memory implementations for demo)
                services.AddScoped<IPersonNameRepository, InMemoryPersonNameRepository>();
                services.AddScoped<INameAliasRepository, InMemoryNameAliasRepository>();
                services.AddScoped<INameAliasCacheRepository, InMemoryNameAliasCacheRepository>();

                // Add LLM services
                services.AddLLMServices(configuration);
                
                // Add distributed cache (in-memory for demo)
                services.AddDistributedMemoryCache();
            });

    static async Task RunDemo(IServiceProvider services, ILogger logger)
    {
        Console.WriteLine();
        
        // Check LLM service availability
        var llmService = services.GetRequiredService<ILLMNameProcessingService>();
        var batchService = services.GetRequiredService<IBatchEnrichmentService>();
        var cacheService = services.GetRequiredService<ILLMCacheService>();
        
        Console.WriteLine("📋 Available LLM Providers:");
        foreach (var provider in llmService.AvailableProviders)
        {
            Console.WriteLine($"   • {provider}");
        }
        Console.WriteLine($"🎯 Current Provider: {llmService.CurrentProvider}");
        Console.WriteLine();

        // Test 1: Individual Name Analysis
        Console.WriteLine("🧪 Test 1: Individual Name Analysis");
        Console.WriteLine("----------------------------------");
        
        await TestNameAnalysis(llmService, "John", logger);
        await TestNameAnalysis(llmService, "Maria", logger);
        await TestNameAnalysis(llmService, "Zhang Wei", logger);

        // Test 2: Cache Statistics
        Console.WriteLine();
        Console.WriteLine("📊 Test 2: Cache Statistics");
        Console.WriteLine("---------------------------");
        await TestCacheStatistics(cacheService, logger);

        // Test 3: Batch Processing (small demo)
        Console.WriteLine();
        Console.WriteLine("📂 Test 3: Batch Processing Demo");
        Console.WriteLine("--------------------------------");
        await TestBatchProcessing(batchService, logger);

        Console.WriteLine();
        Console.WriteLine("✅ Demo completed successfully!");
        Console.WriteLine();
        Console.WriteLine("Next Steps:");
        Console.WriteLine("1. Install Ollama: https://ollama.ai/download");
        Console.WriteLine("2. Run: ollama pull llama3.2:3b");
        Console.WriteLine("3. Add your API keys to appsettings.Development.json");
        Console.WriteLine("4. Change DefaultProvider to test different LLMs");
    }

    static async Task TestNameAnalysis(ILLMNameProcessingService llmService, string name, ILogger logger)
    {
        try
        {
            Console.WriteLine($"Analyzing name: '{name}'");
            
            var request = new ComprehensiveNameAnalysisRequest
            {
                Name = name,
                Options = new NameAnalysisOptions
                {
                    IncludeCultural = true,
                    IncludePhonetic = true,
                    MaxAliases = 5
                }
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await llmService.AnalyzeNameAsync(request);
            stopwatch.Stop();

            Console.WriteLine($"  ⏱️  Processing time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  🏷️  Provider: {result.Metadata.Provider}");
            Console.WriteLine($"  📝 Generated {result.CombinedAliases.Count} aliases");
            
            if (result.CombinedAliases.Any())
            {
                Console.WriteLine($"  📋 Aliases: {string.Join(", ", result.CombinedAliases.Take(3).Select(a => a.Alias))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Failed: {ex.Message}");
            logger.LogWarning(ex, "Name analysis failed for {Name}", name);
        }
        Console.WriteLine();
    }

    static async Task TestCacheStatistics(ILLMCacheService cacheService, ILogger logger)
    {
        try
        {
            var stats = await cacheService.GetCacheStatisticsAsync();
            
            Console.WriteLine($"📈 Cache Statistics:");
            Console.WriteLine($"   Total Entries: {stats.TotalEntries}");
            Console.WriteLine($"   Total Hits: {stats.TotalHits}");
            Console.WriteLine($"   Hit Ratio: {stats.HitRatio:P1}");
            Console.WriteLine($"   Memory Cache Hits: {stats.MemoryCacheHits}");
            Console.WriteLine($"   Persistent Cache Hits: {stats.PersistentCacheHits}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Cache stats failed: {ex.Message}");
            logger.LogWarning(ex, "Cache statistics failed");
        }
    }

    static async Task TestBatchProcessing(IBatchEnrichmentService batchService, ILogger logger)
    {
        try
        {
            // Create a small CSV for testing
            var csvContent = @"Name,Surname,Notes
John,Smith,Common American name
Maria,Garcia,Hispanic name
Zhang,Wei,Chinese name
Ahmed,Hassan,Arabic name";

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            
            var request = new BatchEnrichmentRequest
            {
                JobId = Guid.NewGuid().ToString(),
                FileContent = csvBytes,
                FileName = "test.csv",
                ContentType = "text/csv",
                Options = new BatchEnrichmentOptions
                {
                    MaxConcurrency = 2,
                    BatchSize = 2
                }
            };

            Console.WriteLine($"Processing batch with {csvContent.Split('\n').Length - 1} names...");
            
            var result = await batchService.ProcessCsvFileAsync(request, CancellationToken.None);
            
            Console.WriteLine($"✅ Batch completed:");
            Console.WriteLine($"   📊 Total items: {result.Statistics.TotalItems} names");
            Console.WriteLine($"   ✅ Successful: {result.Statistics.SuccessfulItems}");
            Console.WriteLine($"   ❌ Failed: {result.Statistics.FailedItems}");
            Console.WriteLine($"   ⏱️  Total time: {result.Statistics.TotalProcessingTime.TotalSeconds:F1}s");
            
            if (result.SuccessfulResults.Any())
            {
                var firstResult = result.SuccessfulResults.First();
                Console.WriteLine($"   📝 Sample: '{firstResult.OriginalName}' -> {firstResult.AliasCount} aliases");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Batch processing failed: {ex.Message}");
            logger.LogWarning(ex, "Batch processing failed");
        }
    }
}

// Temporary in-memory implementations for demo purposes
public class InMemoryPersonNameRepository : IPersonNameRepository
{
    private readonly List<PhoneticAnalyzers.Domain.Entities.PersonName> _data = new();
    private long _nextId = 1;

    public Task<PhoneticAnalyzers.Domain.Entities.PersonName> AddAsync(PhoneticAnalyzers.Domain.Entities.PersonName entity, CancellationToken cancellationToken = default)
    {
        // Use reflection to set the ID since it's protected
        var idProperty = typeof(PhoneticAnalyzers.Domain.Entities.PersonName).GetProperty("Id");
        idProperty?.SetValue(entity, _nextId++);
        
        _data.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.Any(x => x.Id == id));
    }

    public Task<PhoneticAnalyzers.Domain.Entities.PersonName?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.FirstOrDefault(x => x.Id == id));
    }

    // Implement other required methods with minimal functionality for demo
    public Task<PhoneticAnalyzers.Domain.Entities.PersonName> UpdateAsync(PhoneticAnalyzers.Domain.Entities.PersonName entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        _data.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.PersonName>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.PersonName>>(_data.ToList());
    }

    public Task<PhoneticAnalyzers.Domain.Entities.PersonName?> FindByNormalizedNameAsync(PhoneticAnalyzers.Domain.ValueObjects.NormalizedName normalizedName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PhoneticAnalyzers.Domain.Entities.PersonName?>(null);
    }

    // Missing interface methods
    public Task<PhoneticAnalyzers.Domain.Entities.PersonName?> GetByCanonicalNameAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.FirstOrDefault(n => n.CanonicalName.Equals(canonicalName, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.PersonName>> GetNamesNeedingEnrichmentAsync(int limit = 100, int enrichmentIntervalDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-enrichmentIntervalDays);
        var result = _data
            .Where(n => n.LastEnrichmentUtc == null || n.LastEnrichmentUtc < cutoffDate)
            .Take(limit)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.PersonName>>(result);
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.PersonName>> SearchAsync(string normalizedText, Locale? locale = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = _data
            .Where(n => n.CanonicalName.Contains(normalizedText, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.PersonName>>(result);
    }

    public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_data.Count);
    }

    public Task<bool> ExistsAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.Any(n => n.CanonicalName.Equals(canonicalName, StringComparison.OrdinalIgnoreCase)));
    }
}

public class InMemoryNameAliasRepository : INameAliasRepository
{
    private readonly List<PhoneticAnalyzers.Domain.Entities.NameAlias> _data = new();

    public Task<PhoneticAnalyzers.Domain.Entities.NameAlias> AddAsync(PhoneticAnalyzers.Domain.Entities.NameAlias entity, CancellationToken cancellationToken = default)
    {
        _data.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>> GetByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default)
    {
        var aliases = _data.Where(x => x.PersonNameId == personNameId).ToList();
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>>(aliases);
    }

    // Implement other required methods with minimal functionality
    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.Any(x => x.Id == id));
    }

    public Task<PhoneticAnalyzers.Domain.Entities.NameAlias?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.FirstOrDefault(x => x.Id == id));
    }

    public Task<PhoneticAnalyzers.Domain.Entities.NameAlias> UpdateAsync(PhoneticAnalyzers.Domain.Entities.NameAlias entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        _data.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>>(_data.ToList());
    }

    // Missing interface methods
    public Task<int> AddRangeAsync(IEnumerable<PhoneticAnalyzers.Domain.Entities.NameAlias> nameAliases, CancellationToken cancellationToken = default)
    {
        var aliases = nameAliases.ToList();
        _data.AddRange(aliases);
        return Task.FromResult(aliases.Count);
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>> SearchAsync(string normalizedText, Locale? locale = null, decimal minConfidence = 0.3m, int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = _data
            .Where(a => a.Confidence >= minConfidence)
            .Take(limit)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>>(result);
    }

    public Task<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>> GetByTypeAndLocaleAsync(AliasType aliasType, Locale locale, int limit = 100, CancellationToken cancellationToken = default)
    {
        var result = _data
            .Where(a => a.AliasType == aliasType && a.Locale == locale)
            .Take(limit)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyList<PhoneticAnalyzers.Domain.Entities.NameAlias>>(result);
    }

    public Task<int> DeleteByPersonNameIdAsync(long personNameId, CancellationToken cancellationToken = default)
    {
        var toRemove = _data.Where(x => x.PersonNameId == personNameId).ToList();
        foreach (var item in toRemove)
        {
            _data.Remove(item);
        }
        return Task.FromResult(toRemove.Count);
    }

    public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_data.Count);
    }
}

public class InMemoryNameAliasCacheRepository : INameAliasCacheRepository
{
    private readonly List<PhoneticAnalyzers.Domain.Entities.NameAliasCache> _data = new();

    public Task<PhoneticAnalyzers.Domain.Entities.NameAliasCache> AddAsync(PhoneticAnalyzers.Domain.Entities.NameAliasCache entity, CancellationToken cancellationToken = default)
    {
        _data.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<PhoneticAnalyzers.Domain.Entities.NameAliasCache?> GetAsync(PhoneticAnalyzers.Domain.ValueObjects.NormalizedName inputQuery, PhoneticAnalyzers.Domain.ValueObjects.Locale locale, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.FirstOrDefault(x => x.InputQuery.Value == inputQuery.Value && x.Locale.Code == locale.Code));
    }

    public Task<PhoneticAnalyzers.Domain.Entities.NameAliasCache> UpdateAsync(PhoneticAnalyzers.Domain.Entities.NameAliasCache entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(entity);
    }

    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = _data.Where(x => x.IsExpired()).ToList();
        foreach (var item in expired)
            _data.Remove(item);
        return Task.FromResult(expired.Count);
    }

    public Task<PhoneticAnalyzers.Domain.Repositories.CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PhoneticAnalyzers.Domain.Repositories.CacheStatistics
        {
            TotalEntries = _data.Count,
            ExpiredEntries = _data.Count(x => x.IsExpired()),
            TotalHits = _data.Sum(x => x.HitCount),
            AverageHitsPerEntry = _data.Any() ? (decimal)_data.Average(x => x.HitCount) : 0,
            ApproximateSizeBytes = _data.Count * 1000 // Rough estimate
        });
    }

    public Task<int> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var count = _data.Count;
        _data.Clear();
        return Task.FromResult(count);
    }
}
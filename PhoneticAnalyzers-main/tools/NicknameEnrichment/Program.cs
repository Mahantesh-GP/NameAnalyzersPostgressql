using Microsoft.Extensions.Configuration;
using NicknameEnrichment;

Console.WriteLine("=== Nickname Enrichment Tool ===\n");

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var connectionString = configuration["Database:ConnectionString"] 
    ?? "Host=localhost;Database=phonetic_native;Username=postgres;Password=postgres";

var llmConfig = new LLMConfiguration
{
    Provider = Enum.Parse<LLMProvider>(configuration["LLM:Provider"] ?? "Ollama"),
    Endpoint = configuration["LLM:Endpoint"] ?? "http://localhost:11434/api/generate",
    ApiKey = configuration["LLM:ApiKey"],
    Model = configuration["LLM:Model"] ?? "llama3.2:latest",
    Temperature = double.Parse(configuration["LLM:Temperature"] ?? "0.3")
};

Console.WriteLine($"Provider: {llmConfig.Provider}");
Console.WriteLine($"Endpoint: {llmConfig.Endpoint}");
Console.WriteLine($"Model: {llmConfig.Model}");
Console.WriteLine($"Temperature: {llmConfig.Temperature}");
Console.WriteLine($"Connecting to database...\n");

var service = new NicknameEnrichmentService(connectionString, llmConfig);

try
{
    // Test LLM with a small batch first
    Console.WriteLine("🧪 Testing LLM with sample names...");
    var testNames = new List<string> { "ROBERT", "WILLIAM", "ELIZABETH" };
    var testResults = await service.GetNicknamesFromLLMBatchAsync(testNames);
    
    if (testResults.Count == 0)
    {
        Console.WriteLine("❌ LLM test failed - no nicknames returned");
        Console.WriteLine("   Check your LLM configuration and endpoint");
        Console.WriteLine("   See TROUBLESHOOTING.md for help");
        return 1;
    }
    
    Console.WriteLine($"✓ LLM test successful - received {testResults.Count} results:");
    foreach (var (name, nicks) in testResults.Take(3))
    {
        Console.WriteLine($"  {name} → {string.Join(", ", nicks)}");
    }
    Console.WriteLine();
    
    Console.WriteLine("Do you want to continue with full enrichment? (y/n)");
    var response = Console.ReadLine()?.Trim().ToLower();
    
    if (response != "y" && response != "yes")
    {
        Console.WriteLine("Enrichment cancelled.");
        return 0;
    }
    
    await service.EnrichAllNicknamesAsync(batchSize: 50);  // Reduced default batch size
    Console.WriteLine("\n✓ Enrichment completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    Console.WriteLine($"Exception Type: {ex.GetType().Name}");
    Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
    return 1;
}

return 0;

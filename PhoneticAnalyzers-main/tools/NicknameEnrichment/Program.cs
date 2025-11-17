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
    await service.EnrichAllNicknamesAsync();
    Console.WriteLine("\n✓ Enrichment completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}

return 0;

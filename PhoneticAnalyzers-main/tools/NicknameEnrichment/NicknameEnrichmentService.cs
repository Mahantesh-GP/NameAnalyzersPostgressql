using Npgsql;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NicknameEnrichment;

public enum LLMProvider
{
    Ollama,
    AzureOpenAI
}

public class LLMConfiguration
{
    public LLMProvider Provider { get; set; } = LLMProvider.Ollama;
    public string Endpoint { get; set; } = "http://localhost:11434/api/generate";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "llama3.2:latest";
    public double Temperature { get; set; } = 0.3;
}

/// <summary>
/// Service to enrich nickname_map table using LLM (Ollama or Azure OpenAI)
/// </summary>
public class NicknameEnrichmentService
{
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;
    private readonly LLMConfiguration _llmConfig;

    public NicknameEnrichmentService(string connectionString, LLMConfiguration llmConfig)
    {
        _connectionString = connectionString;
        _llmConfig = llmConfig;
        _httpClient = new HttpClient();
        
        // Add API key header for Azure OpenAI
        if (_llmConfig.Provider == LLMProvider.AzureOpenAI && !string.IsNullOrEmpty(_llmConfig.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", _llmConfig.ApiKey);
        }
    }

    /// <summary>
    /// Get all unique first names from the database
    /// </summary>
    public async Task<List<string>> GetUniqueFirstNamesAsync()
    {
        var names = new List<string>();
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Get first token from each person (typically first name)
        var sql = @"
            SELECT DISTINCT name_token 
            FROM person_names 
            WHERE token_position = 1
            AND is_nickname = FALSE
            ORDER BY name_token";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    /// <summary>
    /// Call LLM to get nickname variants for a name
    /// </summary>
    public async Task<List<string>> GetNicknamesFromLLMAsync(string name)
    {
        return _llmConfig.Provider switch
        {
            LLMProvider.AzureOpenAI => await GetNicknamesFromAzureOpenAIAsync(name),
            LLMProvider.Ollama => await GetNicknamesFromOllamaAsync(name),
            _ => throw new NotSupportedException($"Provider {_llmConfig.Provider} not supported")
        };
    }

    private async Task<List<string>> GetNicknamesFromOllamaAsync(string name)
    {
        var prompt = $@"Given the name '{name}', provide all common nickname variants.
Return ONLY a JSON array of strings, nothing else.
Example: [""bob"", ""bobby"", ""robby""]

Name: {name}
Nicknames:";

        var request = new
        {
            model = _llmConfig.Model,
            prompt = prompt,
            stream = false,
            temperature = _llmConfig.Temperature
        };

        var response = await _httpClient.PostAsJsonAsync(_llmConfig.Endpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        
        if (result?.Response != null)
        {
            return ParseNicknamesFromJson(result.Response);
        }

        return new List<string>();
    }

    private async Task<List<string>> GetNicknamesFromAzureOpenAIAsync(string name)
    {
        var systemPrompt = "You are a helpful assistant that provides nickname variants for given names. Return only a JSON array of strings with common nicknames.";
        var userPrompt = $@"Given the name '{name}', provide all common nickname variants.
Return ONLY a JSON array of strings, nothing else.
Example: [""bob"", ""bobby"", ""robby""]

Name: {name}";

        var request = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = _llmConfig.Temperature,
            max_tokens = 200
        };

        var response = await _httpClient.PostAsJsonAsync(_llmConfig.Endpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>();
        
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content;
        if (!string.IsNullOrEmpty(content))
        {
            return ParseNicknamesFromJson(content);
        }

        return new List<string>();
    }

    private List<string> ParseNicknamesFromJson(string content)
    {
        try
        {
            // Try to parse as JSON array directly
            var nicknames = JsonSerializer.Deserialize<List<string>>(content);
            return nicknames ?? new List<string>();
        }
        catch
        {
            // Try to extract JSON array from markdown or text
            var jsonStart = content.IndexOf('[');
            var jsonEnd = content.LastIndexOf(']');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                try
                {
                    var nicknames = JsonSerializer.Deserialize<List<string>>(jsonString);
                    return nicknames ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            
            return new List<string>();
        }
    }

    /// <summary>
    /// Insert nicknames into nickname_map table
    /// </summary>
    public async Task InsertNicknamesAsync(string originalName, List<string> nicknames)
    {
        if (nicknames.Count == 0) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var nickname in nicknames)
        {
            var sql = @"
                INSERT INTO nickname_map (normalized_original, normalized_nickname)
                VALUES (normalize_name(@original), normalize_name(@nickname))
                ON CONFLICT (normalized_original, normalized_nickname) DO NOTHING";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("original", originalName);
            cmd.Parameters.AddWithValue("nickname", nickname);
            
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Process all names and enrich nickname mappings
    /// </summary>
    public async Task EnrichAllNicknamesAsync(int batchSize = 100)
    {
        Console.WriteLine("Fetching unique first names...");
        var names = await GetUniqueFirstNamesAsync();
        Console.WriteLine($"Found {names.Count} unique first names");

        int processed = 0;
        int enriched = 0;

        foreach (var name in names)
        {
            try
            {
                Console.Write($"Processing '{name}'... ");
                
                var nicknames = await GetNicknamesFromLLMAsync(name);
                
                if (nicknames.Count > 0)
                {
                    await InsertNicknamesAsync(name, nicknames);
                    Console.WriteLine($"Added {nicknames.Count} nicknames");
                    enriched++;
                }
                else
                {
                    Console.WriteLine("No nicknames found");
                }

                processed++;

                if (processed % 10 == 0)
                {
                    Console.WriteLine($"\nProgress: {processed}/{names.Count} ({enriched} enriched)");
                }

                // Rate limiting - don't overwhelm LLM
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{name}': {ex.Message}");
            }
        }

        Console.WriteLine($"\nCompleted: {processed} names processed, {enriched} enriched");
    }

    // Ollama response model
    private record OllamaResponse(string Response);

    // Azure OpenAI response models
    private record AzureOpenAIResponse(
        [property: JsonPropertyName("choices")] List<Choice>? Choices
    );

    private record Choice(
        [property: JsonPropertyName("message")] Message? Message
    );

    private record Message(
        [property: JsonPropertyName("content")] string? Content
    );
}

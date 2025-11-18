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
    /// Get all unique first names from the database (individuals only, exclude businesses)
    /// </summary>
    public async Task<List<string>> GetUniqueFirstNamesAsync()
    {
        var names = new List<string>();
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Get first token from individuals only (flag='I'), exclude businesses
        var sql = @"
            SELECT DISTINCT pn.name_token 
            FROM person_names pn
            INNER JOIN person p ON p.person_id = pn.person_id
            WHERE pn.token_position = 1
            AND pn.is_nickname = FALSE
            AND p.flag = 'I'
            AND LENGTH(pn.name_token) >= 3
            ORDER BY pn.name_token";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    /// <summary>
    /// Call LLM to get nickname variants for a batch of names (optimized for cost)
    /// </summary>
    public async Task<Dictionary<string, List<string>>> GetNicknamesFromLLMBatchAsync(List<string> names)
    {
        return _llmConfig.Provider switch
        {
            LLMProvider.AzureOpenAI => await GetNicknamesFromAzureOpenAIBatchAsync(names),
            LLMProvider.Ollama => await GetNicknamesFromOllamaBatchAsync(names),
            _ => throw new NotSupportedException($"Provider {_llmConfig.Provider} not supported")
        };
    }

    /// <summary>
    /// Call LLM to get nickname variants for a single name (legacy, less efficient)
    /// </summary>
    public async Task<List<string>> GetNicknamesFromLLMAsync(string name)
    {
        var batch = new List<string> { name };
        var results = await GetNicknamesFromLLMBatchAsync(batch);
        return results.TryGetValue(name, out var nicknames) ? nicknames : new List<string>();
    }

    private async Task<Dictionary<string, List<string>>> GetNicknamesFromOllamaBatchAsync(List<string> names)
    {
        var namesList = string.Join(", ", names);
        var prompt = $@"Given the following list of names, provide all common nickname variants for each name.

RETURN RULES:
1. Return a JSON object where keys are the original names (uppercase) and values are arrays of nicknames
2. Include all common variations, diminutives, and shortened forms
3. Skip names that don't have common nicknames (return empty array)
4. Return ONLY valid JSON, no explanations or markdown

EXAMPLE OUTPUT:
{{
  ""ROBERT"": [""BOB"", ""ROB"", ""BOBBY"", ""ROBBY""],
  ""WILLIAM"": [""BILL"", ""WILL"", ""BILLY"", ""WILLY""],
  ""ELIZABETH"": [""LIZ"", ""BETH"", ""BETTY"", ""LIZZIE""]
}}

NAMES: {namesList}

JSON OUTPUT:";

        var request = new
        {
            model = _llmConfig.Model,
            prompt = prompt,
            stream = false,
            temperature = _llmConfig.Temperature,
            options = new { num_predict = 2000 }
        };

        var response = await _httpClient.PostAsJsonAsync(_llmConfig.Endpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        
        if (result?.Response != null)
        {
            return ParseBatchNicknamesFromJson(result.Response);
        }

        return new Dictionary<string, List<string>>();
    }

    private async Task<List<string>> GetNicknamesFromOllamaAsync(string name)
    {
        var batch = new List<string> { name };
        var results = await GetNicknamesFromOllamaBatchAsync(batch);
        return results.TryGetValue(name.ToUpperInvariant(), out var nicknames) ? nicknames : new List<string>();
    }

    private async Task<Dictionary<string, List<string>>> GetNicknamesFromAzureOpenAIBatchAsync(List<string> names)
    {
        var namesList = string.Join("\n", names.Select((n, i) => $"{i + 1}. {n}"));
        var systemPrompt = "You are an expert assistant that provides nickname variants for given names. You return structured JSON data only.";
        var userPrompt = $@"Given the following list of names, provide all common nickname variants for each name.

RETURN RULES:
1. Return a JSON object where keys are the original names (uppercase) and values are arrays of nicknames
2. Include all common variations, diminutives, and shortened forms
3. Skip names that don't have common nicknames (return empty array)
4. Return ONLY valid JSON, no explanations or markdown

EXAMPLE OUTPUT:
{{
  ""ROBERT"": [""BOB"", ""ROB"", ""BOBBY"", ""ROBBY""],
  ""WILLIAM"": [""BILL"", ""WILL"", ""BILLY"", ""WILLY""],
  ""ELIZABETH"": [""LIZ"", ""BETH"", ""BETTY"", ""LIZZIE""]
}}

NAMES:
{namesList}

JSON OUTPUT:";

        var request = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = _llmConfig.Temperature,
            max_tokens = 2000,
            response_format = new { type = "json_object" }
        };

        var response = await _httpClient.PostAsJsonAsync(_llmConfig.Endpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>();
        
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content;
        if (!string.IsNullOrEmpty(content))
        {
            return ParseBatchNicknamesFromJson(content);
        }

        return new Dictionary<string, List<string>>();
    }

    private async Task<List<string>> GetNicknamesFromAzureOpenAIAsync(string name)
    {
        var batch = new List<string> { name };
        var results = await GetNicknamesFromAzureOpenAIBatchAsync(batch);
        return results.TryGetValue(name.ToUpperInvariant(), out var nicknames) ? nicknames : new List<string>();
    }

    private Dictionary<string, List<string>> ParseBatchNicknamesFromJson(string content)
    {
        try
        {
            // Try to parse as JSON object directly
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var nicknames = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(content, options);
            return nicknames ?? new Dictionary<string, List<string>>();
        }
        catch
        {
            // Try to extract JSON object from markdown or text
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var nicknames = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString, options);
                    return nicknames ?? new Dictionary<string, List<string>>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse batch nicknames JSON: {ex.Message}");
                    Console.WriteLine($"Content: {jsonString.Substring(0, Math.Min(200, jsonString.Length))}...");
                    return new Dictionary<string, List<string>>();
                }
            }
            
            return new Dictionary<string, List<string>>();
        }
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
    /// Insert nicknames into nickname_map table (batch optimized)
    /// </summary>
    public async Task InsertNicknamesBatchAsync(Dictionary<string, List<string>> nicknamesByName)
    {
        if (nicknamesByName.Count == 0) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Use a transaction for batch inserts
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = @"
                INSERT INTO nickname_map (normalized_original, normalized_nickname)
                VALUES (normalize_name(@original), normalize_name(@nickname))
                ON CONFLICT (normalized_original, normalized_nickname) DO NOTHING";

            foreach (var (originalName, nicknames) in nicknamesByName)
            {
                foreach (var nickname in nicknames)
                {
                    await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                    cmd.Parameters.AddWithValue("original", originalName);
                    cmd.Parameters.AddWithValue("nickname", nickname);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Insert nicknames into nickname_map table (single name)
    /// </summary>
    public async Task InsertNicknamesAsync(string originalName, List<string> nicknames)
    {
        if (nicknames.Count == 0) return;
        
        var batch = new Dictionary<string, List<string>>
        {
            { originalName, nicknames }
        };
        
        await InsertNicknamesBatchAsync(batch);
    }

    /// <summary>
    /// Process all names and enrich nickname mappings (batch optimized)
    /// </summary>
    public async Task EnrichAllNicknamesAsync(int batchSize = 100)
    {
        Console.WriteLine("Fetching unique first names...");
        var names = await GetUniqueFirstNamesAsync();
        Console.WriteLine($"Found {names.Count} unique first names");
        Console.WriteLine($"Processing in batches of {batchSize}...");

        int processed = 0;
        int enriched = 0;
        int totalNicknames = 0;
        int batchNumber = 0;
        var batches = names.Chunk(batchSize).ToList();
        
        Console.WriteLine($"Total batches: {batches.Count}");

        foreach (var batch in batches)
        {
            batchNumber++;
            try
            {
                Console.WriteLine($"\n[Batch {batchNumber}/{batches.Count}] Processing {batch.Count()} names...");
                
                var batchList = batch.ToList();
                var nicknameResults = await GetNicknamesFromLLMBatchAsync(batchList);
                
                if (nicknameResults.Count > 0)
                {
                    await InsertNicknamesBatchAsync(nicknameResults);
                    
                    var batchNicknameCount = nicknameResults.Sum(kvp => kvp.Value.Count);
                    enriched += nicknameResults.Count;
                    totalNicknames += batchNicknameCount;
                    
                    Console.WriteLine($"✓ Added {batchNicknameCount} nicknames for {nicknameResults.Count} names");
                    
                    // Show sample results
                    var sampleResults = nicknameResults.Take(3);
                    foreach (var (name, nicks) in sampleResults)
                    {
                        Console.WriteLine($"  {name} → {string.Join(", ", nicks)}");
                    }
                    if (nicknameResults.Count > 3)
                    {
                        Console.WriteLine($"  ... and {nicknameResults.Count - 3} more");
                    }
                }
                else
                {
                    Console.WriteLine("⚠ No nicknames found in this batch");
                }

                processed += batch.Count();
                Console.WriteLine($"Progress: {processed}/{names.Count} names ({enriched} enriched, {totalNicknames} total nicknames)");

                // Rate limiting between batches - less aggressive since batching reduces calls
                if (batchNumber < batches.Count)
                {
                    Console.WriteLine("Waiting 2s before next batch...");
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing batch {batchNumber}: {ex.Message}");
                Console.WriteLine($"Batch names: {string.Join(", ", batch.Take(5))}...");
                // Continue with next batch instead of stopping
            }
        }

        Console.WriteLine($"\n{'='} COMPLETED {'='}");
        Console.WriteLine($"Names processed: {processed}/{names.Count}");
        Console.WriteLine($"Names enriched: {enriched}");
        Console.WriteLine($"Total nicknames added: {totalNicknames}");
        Console.WriteLine($"Average nicknames per name: {(enriched > 0 ? (double)totalNicknames / enriched : 0):F1}");
        Console.WriteLine($"LLM calls made: {batches.Count} (batch size: {batchSize})");
        Console.WriteLine($"Cost reduction vs single calls: {(names.Count > 0 ? (1 - (double)batches.Count / names.Count) * 100 : 0):F0}%");
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

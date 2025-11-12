using System.Text.Json.Serialization;

namespace PhoneticAnalyzers.WebUI.Models;

public class BatchIngestRequest
{
    [JsonPropertyName("persons")]
    public List<PersonIngestData> Persons { get; set; } = new();
}

public class PersonIngestData
{
    [JsonPropertyName("externalId")] public string? ExternalId { get; set; }
    [JsonPropertyName("fullName")] public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("county")] public string? County { get; set; }
    [JsonPropertyName("countyId")] public int? CountyId { get; set; }
    [JsonPropertyName("countyName")] public string? CountyName { get; set; }
    [JsonPropertyName("flag")] public string? Flag { get; set; } = "U"; // I,B,U
    [JsonPropertyName("expandNicknames")] public bool? ExpandNicknames { get; set; } = true;
}

public class BatchIngestResultItem
{
    [JsonPropertyName("externalId")] public string? ExternalId { get; set; }
    [JsonPropertyName("personId")] public long? PersonId { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public class BatchIngestResponse
{
    [JsonPropertyName("totalProcessed")] public int TotalProcessed { get; set; }
    [JsonPropertyName("successful")] public int Successful { get; set; }
    [JsonPropertyName("failed")] public int Failed { get; set; }
    [JsonPropertyName("results")] public List<BatchIngestResultItem> Results { get; set; } = new();
    [JsonPropertyName("errors")] public List<BatchIngestResultItem> Errors { get; set; } = new();
    [JsonPropertyName("enrichment")] public object? Enrichment { get; set; }
}

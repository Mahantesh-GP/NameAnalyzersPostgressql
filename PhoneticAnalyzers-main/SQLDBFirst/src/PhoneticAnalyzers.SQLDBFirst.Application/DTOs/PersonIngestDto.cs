using System.Text.Json.Serialization;

namespace PhoneticAnalyzers.SQLDBFirst.Application.DTOs;

/// <summary>
/// Data Transfer Object for person ingestion.
/// Used by API endpoints to receive person data.
/// </summary>
public class PersonIngestDto
{
    // Support "Id" from CSV (will be matched case-insensitively)
    public string? Id { get; set; }
    
    // Support "ExternalId" from API (will be matched case-insensitively)
    public string? ExternalId { get; set; }
    
    // Return whichever is populated
    [JsonIgnore]
    public string ActualExternalId => ExternalId ?? Id ?? string.Empty;
    
    public string FullName { get; set; } = string.Empty;
    
    public string? County { get; set; }
    
    public bool ExpandNicknames { get; set; } = false;
}

namespace PhoneticAnalyzers.NativeApi.Models;

/// <summary>
/// Request model for person ingestion
/// </summary>
public class IngestPersonRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? County { get; set; }
    public char Flag { get; set; } = 'I';
}

/// <summary>
/// Result of person ingestion
/// </summary>
public class IngestPersonResult
{
    public long PersonId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request for batch ingestion
/// </summary>
public class BatchIngestRequest
{
    public List<IngestPersonRequest> Persons { get; set; } = new();
}

/// <summary>
/// Result of batch ingestion
/// </summary>
public class BatchIngestResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<IngestPersonResult> Results { get; set; } = new();
}

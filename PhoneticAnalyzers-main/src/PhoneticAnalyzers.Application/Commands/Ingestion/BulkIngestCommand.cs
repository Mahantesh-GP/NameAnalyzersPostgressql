using MediatR;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Application.Commands.Ingestion;

/// <summary>
/// Command for bulk ingesting millions of records efficiently
/// </summary>
public sealed class BulkIngestCommand : IRequest<BulkIngestResult>
{
    /// <summary>
    /// Gets the file path or data source for bulk ingestion
    /// </summary>
    public string DataSource { get; init; } = string.Empty;

    /// <summary>
    /// Gets the batch size for processing
    /// </summary>
    public int BatchSize { get; init; } = 1000;

    /// <summary>
    /// Gets the maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets whether to skip phonetic encoding for performance (can be done later)
    /// </summary>
    public bool SkipPhoneticEncoding { get; init; } = false;

    /// <summary>
    /// Gets whether to continue processing on individual record errors
    /// </summary>
    public bool ContinueOnError { get; init; } = true;

    /// <summary>
    /// Gets the source system identifier
    /// </summary>
    public string? SourceSystem { get; init; }
}

/// <summary>
/// Result of bulk ingestion operation
/// </summary>
public sealed class BulkIngestResult
{
    /// <summary>
    /// Gets the total number of records processed
    /// </summary>
    public long TotalRecordsProcessed { get; init; }

    /// <summary>
    /// Gets the number of records successfully inserted
    /// </summary>
    public long RecordsInserted { get; init; }

    /// <summary>
    /// Gets the number of records successfully updated
    /// </summary>
    public long RecordsUpdated { get; init; }

    /// <summary>
    /// Gets the number of records that failed processing
    /// </summary>
    public long RecordsFailed { get; init; }

    /// <summary>
    /// Gets the processing duration
    /// </summary>
    public TimeSpan ProcessingDuration { get; init; }

    /// <summary>
    /// Gets the processing rate (records per second)
    /// </summary>
    public double RecordsPerSecond => 
        ProcessingDuration.TotalSeconds > 0 
            ? TotalRecordsProcessed / ProcessingDuration.TotalSeconds 
            : 0;

    /// <summary>
    /// Gets any batch-level errors
    /// </summary>
    public IReadOnlyList<string> BatchErrors { get; init; } = [];

    /// <summary>
    /// Gets sample failed records for debugging
    /// </summary>
    public IReadOnlyList<FailedRecordInfo> SampleFailedRecords { get; init; } = [];
}

/// <summary>
/// Information about a failed record
/// </summary>
public sealed class FailedRecordInfo
{
    /// <summary>
    /// Gets the record identifier
    /// </summary>
    public string RecordId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets the raw record data
    /// </summary>
    public string? RawData { get; init; }
}

/// <summary>
/// Record structure for bulk ingestion
/// </summary>
public sealed class BulkIngestRecord
{
    /// <summary>
    /// Gets the external identifier
    /// </summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full name
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the county
    /// </summary>
    public string County { get; init; } = string.Empty;

    /// <summary>
    /// Gets the county identifier
    /// </summary>
    public int CountyId { get; init; }

    /// <summary>
    /// Gets the county name
    /// </summary>
    public string CountyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the record type flag
    /// </summary>
    public RecordTypeFlag Flag { get; init; } = RecordTypeFlag.Unknown;

    /// <summary>
    /// Gets the source system
    /// </summary>
    public string? SourceSystem { get; init; }

    /// <summary>
    /// Gets additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
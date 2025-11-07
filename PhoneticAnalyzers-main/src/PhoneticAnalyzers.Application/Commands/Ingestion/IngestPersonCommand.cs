// FluentValidation removed for smoke-test simplification
using MediatR;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Application.Commands.Ingestion;

/// <summary>
/// Command to ingest a single person record
/// </summary>
public sealed class IngestPersonCommand : IRequest<IngestPersonCommandResult>
{
    /// <summary>
    /// Gets the external identifier for the person
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full name of the person
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether to perform nickname expansion during processing
    /// </summary>
    public bool ExpandNicknames { get; set; } = true;

    /// <summary>
    /// Gets the source system identifier (for audit purposes)
    /// </summary>
    public string? SourceSystem { get; set; }

    /// <summary>
    /// Gets additional metadata for the ingestion
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Result of person ingestion operation
/// </summary>
public sealed class IngestPersonCommandResult
{
    /// <summary>
    /// Gets the person ID
    /// </summary>
    public long PersonId { get; init; }

    /// <summary>
    /// Gets the external ID
    /// </summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the person was created (true) or updated (false)
    /// </summary>
    public bool WasCreated { get; init; }

    /// <summary>
    /// Gets the phonetic encoding result
    /// </summary>
    public PhoneticEncodingSummary PhoneticEncoding { get; init; } = new();

    /// <summary>
    /// Gets any warnings generated during processing
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Summary of phonetic encoding results
/// </summary>
public sealed class PhoneticEncodingSummary
{
    /// <summary>
    /// Gets the primary Double Metaphone code
    /// </summary>
    public string? PrimaryDoubleMetaphone { get; init; }

    /// <summary>
    /// Gets the alternate Double Metaphone code
    /// </summary>
    public string? AlternateDoubleMetaphone { get; init; }

    /// <summary>
    /// Gets the count of Beider-Morse variants generated
    /// </summary>
    public int BeiderMorseVariantCount { get; init; }

    /// <summary>
    /// Gets the Beider-Morse codes
    /// </summary>
    public IReadOnlyList<string> BeiderMorseCodes { get; init; } = [];
}

/// <summary>
/// Command to ingest multiple person records in a batch
/// </summary>
public sealed class IngestPersonBatchCommand : IRequest<IngestPersonBatchCommandResult>
{
    /// <summary>
    /// Gets the collection of person records to ingest
    /// </summary>
    public IReadOnlyList<PersonBatchItem> Persons { get; init; } = [];

    /// <summary>
    /// Gets the batch size for processing
    /// </summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Gets whether to continue processing if individual items fail
    /// </summary>
    public bool ContinueOnError { get; init; } = true;

    /// <summary>
    /// Gets the source system identifier for the entire batch
    /// </summary>
    public string? SourceSystem { get; init; }
}

/// <summary>
/// Individual item in a person batch
/// </summary>
public sealed class PersonBatchItem
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
    /// Gets whether to expand nicknames for this item
    /// </summary>
    public bool ExpandNicknames { get; init; } = true;

    /// <summary>
    /// Gets item-specific metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of batch person ingestion
/// </summary>
public sealed class IngestPersonBatchCommandResult
{
    /// <summary>
    /// Gets the total number of items processed
    /// </summary>
    public int TotalProcessed { get; init; }

    /// <summary>
    /// Gets the number of items successfully created
    /// </summary>
    public int SuccessfullyCreated { get; init; }

    /// <summary>
    /// Gets the number of items successfully updated
    /// </summary>
    public int SuccessfullyUpdated { get; init; }

    /// <summary>
    /// Gets the number of items that failed processing
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// Gets detailed results for each processed item
    /// </summary>
    public IReadOnlyList<BatchItemResult> Results { get; init; } = [];

    /// <summary>
    /// Gets any batch-level errors
    /// </summary>
    public IReadOnlyList<string> BatchErrors { get; init; } = [];

    /// <summary>
    /// Gets the processing duration
    /// </summary>
    public TimeSpan ProcessingDuration { get; init; }
}

/// <summary>
/// Result for an individual batch item
/// </summary>
public sealed class BatchItemResult
{
    /// <summary>
    /// Gets the external ID of the item
    /// </summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the processing was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the person ID if successful
    /// </summary>
    public long? PersonId { get; init; }

    /// <summary>
    /// Gets whether the person was created (true) or updated (false)
    /// </summary>
    public bool? WasCreated { get; init; }

    /// <summary>
    /// Gets the error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets any warnings for this item
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

// Note: Validation removed; consider reintroducing with a lightweight custom validator if needed.

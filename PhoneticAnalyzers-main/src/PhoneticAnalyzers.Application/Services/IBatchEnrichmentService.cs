using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Services.LLM;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneticAnalyzers.Application.Services
{
    /// <summary>
    /// Service for batch enrichment of names from file uploads
    /// </summary>
    public interface IBatchEnrichmentService
    {
        /// <summary>
        /// Processes a CSV file upload and enriches names
        /// </summary>
        Task<BatchEnrichmentResult> ProcessCsvFileAsync(BatchEnrichmentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a JSON file upload and enriches names
        /// </summary>
        Task<BatchEnrichmentResult> ProcessJsonFileAsync(BatchEnrichmentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a batch enrichment job
        /// </summary>
        Task<BatchEnrichmentStatus?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a running batch enrichment job
        /// </summary>
        Task<bool> CancelJobAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported file formats and their specifications
        /// </summary>
        Task<IReadOnlyList<FileFormatInfo>> GetSupportedFormatsAsync();
    }

    /// <summary>
    /// Batch enrichment request
    /// </summary>
    public class BatchEnrichmentRequest
    {
        /// <summary>
        /// Unique job identifier
        /// </summary>
        public string JobId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// File content as byte array
        /// </summary>
        public required byte[] FileContent { get; init; }

        /// <summary>
        /// Original filename
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// File content type
        /// </summary>
        public required string ContentType { get; init; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; init; }

        /// <summary>
        /// Processing options
        /// </summary>
        public BatchEnrichmentOptions Options { get; init; } = new();

        /// <summary>
        /// Progress callback for real-time updates
        /// </summary>
        public IProgress<BatchEnrichmentProgress>? ProgressCallback { get; init; }

        /// <summary>
        /// User who initiated the batch
        /// </summary>
        public string? UserId { get; init; }
    }

    /// <summary>
    /// Batch enrichment processing options
    /// </summary>
    public class BatchEnrichmentOptions
    {
        /// <summary>
        /// Maximum concurrent processing tasks
        /// </summary>
        public int MaxConcurrency { get; init; } = 5;

        /// <summary>
        /// Skip names that already exist in the database
        /// </summary>
        public bool SkipExistingNames { get; init; } = true;

        /// <summary>
        /// Continue processing on individual failures
        /// </summary>
        public bool ContinueOnError { get; init; } = true;

        /// <summary>
        /// Batch size for database operations
        /// </summary>
        public int BatchSize { get; init; } = 100;

        /// <summary>
        /// Name analysis options for LLM processing
        /// </summary>
        public NameAnalysisOptions AnalysisOptions { get; init; } = new();

        /// <summary>
        /// CSV-specific options
        /// </summary>
        public CsvProcessingOptions CsvOptions { get; init; } = new();

        /// <summary>
        /// JSON-specific options
        /// </summary>
        public JsonProcessingOptions JsonOptions { get; init; } = new();
    }

    /// <summary>
    /// CSV processing options
    /// </summary>
    public class CsvProcessingOptions
    {
        /// <summary>
        /// Has header row
        /// </summary>
        public bool HasHeader { get; init; } = true;

        /// <summary>
        /// CSV delimiter character
        /// </summary>
        public char Delimiter { get; init; } = ',';

        /// <summary>
        /// Column name for first name (or 0-based index)
        /// </summary>
        public string FirstNameColumn { get; init; } = "FirstName";

        /// <summary>
        /// Column name for last name (or 0-based index)
        /// </summary>
        public string? LastNameColumn { get; init; } = "LastName";

        /// <summary>
        /// Column name for cultural hint (optional)
        /// </summary>
        public string? CulturalHintColumn { get; init; } = "Culture";

        /// <summary>
        /// Text encoding for the CSV file
        /// </summary>
        public string Encoding { get; init; } = "UTF-8";
    }

    /// <summary>
    /// JSON processing options
    /// </summary>
    public class JsonProcessingOptions
    {
        /// <summary>
        /// JSON path to the array of name objects
        /// </summary>
        public string ArrayPath { get; init; } = "$";

        /// <summary>
        /// Property name for first name
        /// </summary>
        public string FirstNameProperty { get; init; } = "firstName";

        /// <summary>
        /// Property name for last name (optional)
        /// </summary>
        public string? LastNameProperty { get; init; } = "lastName";

        /// <summary>
        /// Property name for cultural hint (optional)
        /// </summary>
        public string? CulturalHintProperty { get; init; } = "culture";

        /// <summary>
        /// Case sensitive property matching
        /// </summary>
        public bool CaseSensitive { get; init; } = false;
    }

    /// <summary>
    /// Batch enrichment progress information
    /// </summary>
    public class BatchEnrichmentProgress
    {
        /// <summary>
        /// Job identifier
        /// </summary>
        public required string JobId { get; init; }

        /// <summary>
        /// Current processing phase
        /// </summary>
        public required BatchProcessingPhase Phase { get; init; }

        /// <summary>
        /// Total items to process
        /// </summary>
        public required int TotalItems { get; init; }

        /// <summary>
        /// Items processed so far
        /// </summary>
        public required int ProcessedItems { get; init; }

        /// <summary>
        /// Items that failed processing
        /// </summary>
        public required int FailedItems { get; init; }

        /// <summary>
        /// Items skipped (already exist, etc.)
        /// </summary>
        public required int SkippedItems { get; init; }

        /// <summary>
        /// Current item being processed
        /// </summary>
        public string? CurrentItem { get; init; }

        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; init; }

        /// <summary>
        /// Processing rate (items per second)
        /// </summary>
        public double ProcessingRate { get; init; }

        /// <summary>
        /// Additional status message
        /// </summary>
        public string? StatusMessage { get; init; }
    }

    /// <summary>
    /// Batch processing phases
    /// </summary>
    public enum BatchProcessingPhase
    {
        Initializing,
        ParseFile,
        ValidatingData,
        ProcessingNames,
        SavingResults,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Batch enrichment result
    /// </summary>
    public class BatchEnrichmentResult
    {
        /// <summary>
        /// Job identifier
        /// </summary>
        public required string JobId { get; init; }

        /// <summary>
        /// Overall success status
        /// </summary>
        public required bool Success { get; init; }

        /// <summary>
        /// Processing statistics
        /// </summary>
        public required BatchEnrichmentStatistics Statistics { get; init; }

        /// <summary>
        /// Names that were successfully processed
        /// </summary>
        public required IReadOnlyList<EnrichedNameResult> SuccessfulResults { get; init; }

        /// <summary>
        /// Names that failed processing
        /// </summary>
        public required IReadOnlyList<FailedNameEnrichment> FailedResults { get; init; }

        /// <summary>
        /// Names that were skipped
        /// </summary>
        public required IReadOnlyList<SkippedNameResult> SkippedResults { get; init; }

        /// <summary>
        /// Output file with enriched data (optional)
        /// </summary>
        public EnrichmentOutputFile? OutputFile { get; init; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Batch enrichment statistics
    /// </summary>
    public class BatchEnrichmentStatistics
    {
        /// <summary>
        /// Total processing time
        /// </summary>
        public required TimeSpan TotalProcessingTime { get; init; }

        /// <summary>
        /// Time spent on file parsing
        /// </summary>
        public required TimeSpan FileParsingTime { get; init; }

        /// <summary>
        /// Time spent on LLM processing
        /// </summary>
        public required TimeSpan LLMProcessingTime { get; init; }

        /// <summary>
        /// Time spent on database operations
        /// </summary>
        public required TimeSpan DatabaseTime { get; init; }

        /// <summary>
        /// Total items in file
        /// </summary>
        public required int TotalItems { get; init; }

        /// <summary>
        /// Successfully processed items
        /// </summary>
        public required int SuccessfulItems { get; init; }

        /// <summary>
        /// Failed items
        /// </summary>
        public required int FailedItems { get; init; }

        /// <summary>
        /// Skipped items
        /// </summary>
        public required int SkippedItems { get; init; }

        /// <summary>
        /// Average processing time per item
        /// </summary>
        public required TimeSpan AverageProcessingTime { get; init; }

        /// <summary>
        /// Total LLM tokens used
        /// </summary>
        public int? TotalTokensUsed { get; init; }

        /// <summary>
        /// Total estimated cost
        /// </summary>
        public decimal? TotalCost { get; init; }
    }

    /// <summary>
    /// Successfully enriched name result
    /// </summary>
    public class EnrichedNameResult
    {
        /// <summary>
        /// Original name from file
        /// </summary>
        public required string OriginalName { get; init; }

        /// <summary>
        /// Surname if provided
        /// </summary>
        public string? Surname { get; init; }

        /// <summary>
        /// Cultural hint if provided
        /// </summary>
        public string? CulturalHint { get; init; }

        /// <summary>
        /// Generated PersonName entity ID
        /// </summary>
        public required long PersonNameId { get; init; }

        /// <summary>
        /// Number of aliases generated
        /// </summary>
        public required int AliasCount { get; init; }

        /// <summary>
        /// LLM processing time for this item
        /// </summary>
        public required TimeSpan ProcessingTime { get; init; }

        /// <summary>
        /// Confidence score summary
        /// </summary>
        public ConfidenceScoreSummary ConfidenceScores { get; init; } = new();
    }

    /// <summary>
    /// Failed name enrichment result
    /// </summary>
    public class FailedNameEnrichment
    {
        /// <summary>
        /// Original name from file
        /// </summary>
        public required string OriginalName { get; init; }

        /// <summary>
        /// Surname if provided
        /// </summary>
        public string? Surname { get; init; }

        /// <summary>
        /// Row/line number in file
        /// </summary>
        public required int RowNumber { get; init; }

        /// <summary>
        /// Error message
        /// </summary>
        public required string ErrorMessage { get; init; }

        /// <summary>
        /// Exception details if available
        /// </summary>
        public string? ExceptionDetails { get; init; }

        /// <summary>
        /// Failure timestamp
        /// </summary>
        public required DateTime FailedAt { get; init; }
    }

    /// <summary>
    /// Skipped name result
    /// </summary>
    public class SkippedNameResult
    {
        /// <summary>
        /// Original name from file
        /// </summary>
        public required string OriginalName { get; init; }

        /// <summary>
        /// Surname if provided
        /// </summary>
        public string? Surname { get; init; }

        /// <summary>
        /// Row/line number in file
        /// </summary>
        public required int RowNumber { get; init; }

        /// <summary>
        /// Reason for skipping
        /// </summary>
        public required string SkipReason { get; init; }

        /// <summary>
        /// Existing PersonName ID if applicable
        /// </summary>
        public long? ExistingPersonNameId { get; init; }
    }

    /// <summary>
    /// Confidence score summary
    /// </summary>
    public class ConfidenceScoreSummary
    {
        /// <summary>
        /// Average confidence across all aliases
        /// </summary>
        public double AverageConfidence { get; init; }

        /// <summary>
        /// Highest confidence score
        /// </summary>
        public double HighestConfidence { get; init; }

        /// <summary>
        /// Number of high-confidence aliases (>= 0.8)
        /// </summary>
        public int HighConfidenceCount { get; init; }

        /// <summary>
        /// Primary cultural confidence
        /// </summary>
        public double? CulturalConfidence { get; init; }
    }

    /// <summary>
    /// Batch job status information
    /// </summary>
    public class BatchEnrichmentStatus
    {
        /// <summary>
        /// Job identifier
        /// </summary>
        public required string JobId { get; init; }

        /// <summary>
        /// Current processing phase
        /// </summary>
        public required BatchProcessingPhase Phase { get; init; }

        /// <summary>
        /// Job started timestamp
        /// </summary>
        public required DateTime StartedAt { get; init; }

        /// <summary>
        /// Job completion timestamp (if completed)
        /// </summary>
        public DateTime? CompletedAt { get; init; }

        /// <summary>
        /// Current progress
        /// </summary>
        public BatchEnrichmentProgress? CurrentProgress { get; init; }

        /// <summary>
        /// Final result (if completed)
        /// </summary>
        public BatchEnrichmentResult? Result { get; init; }

        /// <summary>
        /// Is job still running
        /// </summary>
        public bool IsRunning => Phase != BatchProcessingPhase.Completed && 
                                Phase != BatchProcessingPhase.Failed && 
                                Phase != BatchProcessingPhase.Cancelled;
    }

    /// <summary>
    /// Output file information
    /// </summary>
    public class EnrichmentOutputFile
    {
        /// <summary>
        /// File name
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// File content type
        /// </summary>
        public required string ContentType { get; init; }

        /// <summary>
        /// File content as byte array
        /// </summary>
        public required byte[] Content { get; init; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long SizeBytes => Content.Length;
    }

    /// <summary>
    /// Supported file format information
    /// </summary>
    public class FileFormatInfo
    {
        /// <summary>
        /// Format name
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// File extensions
        /// </summary>
        public required IReadOnlyList<string> Extensions { get; init; }

        /// <summary>
        /// MIME content types
        /// </summary>
        public required IReadOnlyList<string> ContentTypes { get; init; }

        /// <summary>
        /// Maximum file size in bytes
        /// </summary>
        public long MaxSizeBytes { get; init; }

        /// <summary>
        /// Format description
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// Sample format structure
        /// </summary>
        public string? SampleFormat { get; init; }
    }

    /// <summary>
    /// Raw name data extracted from file
    /// </summary>
    public class RawNameData
    {
        /// <summary>
        /// First name
        /// </summary>
        public required string FirstName { get; init; }

        /// <summary>
        /// Last name (optional)
        /// </summary>
        public string? LastName { get; init; }

        /// <summary>
        /// Cultural hint (optional)
        /// </summary>
        public string? CulturalHint { get; init; }

        /// <summary>
        /// Row/line number in source file
        /// </summary>
        public required int RowNumber { get; init; }

        /// <summary>
        /// Additional metadata from file
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
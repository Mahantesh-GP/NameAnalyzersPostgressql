using MediatR;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Commands.Ingestion;
using PhoneticAnalyzers.Application.Services.Phonetic;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace PhoneticAnalyzers.Application.Handlers.Ingestion;

/// <summary>
/// Handler for BulkIngestCommand - optimized for millions of records
/// </summary>
public sealed class BulkIngestCommandHandler : IRequestHandler<BulkIngestCommand, BulkIngestResult>
{
    private readonly IPersonRepository _personRepository;
    private readonly IPhoneticEncodingService _phoneticService;
    private readonly ILogger<BulkIngestCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the BulkIngestCommandHandler class
    /// </summary>
    public BulkIngestCommandHandler(
        IPersonRepository personRepository,
        IPhoneticEncodingService phoneticService,
        ILogger<BulkIngestCommandHandler> logger)
    {
        _personRepository = personRepository;
        _phoneticService = phoneticService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the BulkIngestCommand
    /// </summary>
    public async Task<BulkIngestResult> Handle(BulkIngestCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting bulk ingestion from source: {DataSource}, BatchSize: {BatchSize}",
            request.DataSource, request.BatchSize);

        try
        {
            // Parse CSV file and process in batches
            var records = ReadCsvRecords(request.DataSource);
            var result = await ProcessRecordsInBatches(records, request, cancellationToken);

            stopwatch.Stop();
            result = new BulkIngestResult
            {
                TotalRecordsProcessed = result.TotalRecordsProcessed,
                RecordsInserted = result.RecordsInserted,
                RecordsUpdated = result.RecordsUpdated,
                RecordsFailed = result.RecordsFailed,
                BatchErrors = result.BatchErrors,
                SampleFailedRecords = result.SampleFailedRecords,
                ProcessingDuration = stopwatch.Elapsed
            };

            _logger.LogInformation("Bulk ingestion completed. Processed: {TotalProcessed}, Inserted: {Inserted}, Updated: {Updated}, Failed: {Failed}, Rate: {Rate:F2} records/sec",
                result.TotalRecordsProcessed, result.RecordsInserted, result.RecordsUpdated, result.RecordsFailed, result.RecordsPerSecond);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk ingestion failed for source: {DataSource}", request.DataSource);
            stopwatch.Stop();
            
            return new BulkIngestResult
            {
                ProcessingDuration = stopwatch.Elapsed,
                BatchErrors = [ex.Message]
            };
        }
    }

    /// <summary>
    /// Reads records from CSV file
    /// </summary>
    private IEnumerable<BulkIngestRecord> ReadCsvRecords(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Bulk ingestion file not found: {filePath}");
        }

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        });

        csv.Context.RegisterClassMap<BulkIngestRecordMap>();
        
        foreach (var record in csv.GetRecords<BulkIngestRecord>())
        {
            yield return record;
        }
    }

    /// <summary>
    /// Processes records in optimized batches
    /// </summary>
    private async Task<BulkIngestResult> ProcessRecordsInBatches(
        IEnumerable<BulkIngestRecord> records,
        BulkIngestCommand request,
        CancellationToken cancellationToken)
    {
        var totalProcessed = 0L;
        var totalInserted = 0L;
        var totalUpdated = 0L;
        var totalFailed = 0L;
        var failedRecords = new ConcurrentBag<FailedRecordInfo>();
        var batchErrors = new ConcurrentBag<string>();

        var batches = records
            .Select((record, index) => new { Record = record, Index = index })
            .GroupBy(x => x.Index / request.BatchSize)
            .Select(g => g.Select(x => x.Record).ToList());

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = request.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(batches, parallelOptions, async (batch, ct) =>
        {
            try
            {
                var batchResult = await ProcessBatch(batch, request.SkipPhoneticEncoding, ct);
                
                Interlocked.Add(ref totalProcessed, batchResult.Processed);
                Interlocked.Add(ref totalInserted, batchResult.Inserted);
                Interlocked.Add(ref totalUpdated, batchResult.Updated);
                Interlocked.Add(ref totalFailed, batchResult.Failed);

                foreach (var error in batchResult.FailedRecords)
                {
                    failedRecords.Add(error);
                }

                if (batchResult.Processed % 10000 == 0) // Log progress every 10k records
                {
                    _logger.LogInformation("Processed {Processed} records so far...", totalProcessed);
                }
            }
            catch (Exception ex)
            {
                var error = $"Batch processing failed: {ex.Message}";
                batchErrors.Add(error);
                _logger.LogError(ex, "Failed to process batch");

                if (!request.ContinueOnError)
                {
                    throw;
                }
            }
        });

        return new BulkIngestResult
        {
            TotalRecordsProcessed = totalProcessed,
            RecordsInserted = totalInserted,
            RecordsUpdated = totalUpdated,
            RecordsFailed = totalFailed,
            BatchErrors = batchErrors.ToList(),
            SampleFailedRecords = failedRecords.Take(100).ToList() // Limit to first 100 for performance
        };
    }

    /// <summary>
    /// Processes a single batch of records
    /// </summary>
    private async Task<BatchProcessingResult> ProcessBatch(
        IList<BulkIngestRecord> batchRecords,
        bool skipPhoneticEncoding,
        CancellationToken cancellationToken)
    {
        var persons = new List<Person>();
        var failedRecords = new List<FailedRecordInfo>();
        var processed = 0;

        foreach (var record in batchRecords)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(record.ExternalId) || string.IsNullOrWhiteSpace(record.FullName))
                {
                    failedRecords.Add(new FailedRecordInfo
                    {
                        RecordId = record.ExternalId ?? "UNKNOWN",
                        ErrorMessage = "Missing required fields (ExternalId or FullName)"
                    });
                    continue;
                }

                var externalId = ExternalId.Create(record.ExternalId);
                
                // Generate phonetic codes if not skipped
                PhoneticCode? primaryDM = null;
                PhoneticCode? alternateDM = null;
                List<PhoneticCode> beiderMorseCodes = [];

                if (!skipPhoneticEncoding)
                {
                    var normalizedName = NormalizedName.Create(record.FullName);
                    var phoneticResult = await _phoneticService.EncodeAsync(normalizedName);
                    primaryDM = phoneticResult.PrimaryDoubleMetaphone;
                    alternateDM = phoneticResult.AlternateDoubleMetaphone;
                    beiderMorseCodes = phoneticResult.BeiderMorseCodes.ToList();
                }

                var person = Person.Create(
                    externalId,
                    record.FullName,
                    record.County,
                    record.CountyId,
                    record.CountyName,
                    record.Flag,
                    primaryDM,
                    alternateDM,
                    beiderMorseCodes);

                persons.Add(person);
                processed++;
            }
            catch (Exception ex)
            {
                failedRecords.Add(new FailedRecordInfo
                {
                    RecordId = record.ExternalId ?? "UNKNOWN",
                    ErrorMessage = ex.Message,
                    RawData = $"{record.ExternalId}|{record.FullName}|{record.County}"
                });
            }
        }

        // Bulk insert/update using repository
        var result = await _personRepository.BulkUpsertAsync(persons, cancellationToken);

        return new BatchProcessingResult
        {
            Processed = processed,
            Inserted = result.Inserted,
            Updated = result.Updated,
            Failed = failedRecords.Count,
            FailedRecords = failedRecords
        };
    }

    /// <summary>
    /// Result of processing a single batch
    /// </summary>
    private sealed class BatchProcessingResult
    {
        public int Processed { get; init; }
        public int Inserted { get; init; }
        public int Updated { get; init; }
        public int Failed { get; init; }
        public IList<FailedRecordInfo> FailedRecords { get; init; } = [];
    }
}

/// <summary>
/// CSV mapping configuration for BulkIngestRecord
/// </summary>
public sealed class BulkIngestRecordMap : ClassMap<BulkIngestRecord>
{
    /// <summary>
    /// Initializes a new instance of the BulkIngestRecordMap class
    /// </summary>
    public BulkIngestRecordMap()
    {
        Map(m => m.ExternalId).Name("Id", "ExternalId", "external_id");
        Map(m => m.FullName).Name("FullName", "Name", "full_name");
        Map(m => m.County).Name("County", "county");
        Map(m => m.CountyId).Name("CountyId", "CountyID", "county_id");
        Map(m => m.CountyName).Name("CountyName", "county_name");
        Map(m => m.Flag).Name("Flag", "Type", "RecordType").Convert(args =>
        {
            var value = args.Row.GetField("Flag")?.ToUpperInvariant();
            return value switch
            {
                "I" or "INDIVIDUAL" => RecordTypeFlag.Individual,
                "B" or "BUSINESS" => RecordTypeFlag.Business,
                _ => RecordTypeFlag.Unknown
            };
        });
        Map(m => m.SourceSystem).Name("SourceSystem", "Source").Optional();
    }
}
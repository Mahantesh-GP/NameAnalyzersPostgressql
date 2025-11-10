using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Services.LLM;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Implementation of batch enrichment service with file upload support
    /// </summary>
    public class BatchEnrichmentService : IBatchEnrichmentService, IDisposable
    {
        private readonly ILogger<BatchEnrichmentService> _logger;
        private readonly ILLMNameProcessingService _llmService;
        private readonly IPersonNameRepository _personNameRepository;
        private readonly INameAliasRepository _nameAliasRepository;
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, BatchJobContext> _runningJobs;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens;

        public BatchEnrichmentService(
            ILogger<BatchEnrichmentService> logger,
            ILLMNameProcessingService llmService,
            IPersonNameRepository personNameRepository,
            INameAliasRepository nameAliasRepository,
            IMemoryCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _personNameRepository = personNameRepository ?? throw new ArgumentNullException(nameof(personNameRepository));
            _nameAliasRepository = nameAliasRepository ?? throw new ArgumentNullException(nameof(nameAliasRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            
            _runningJobs = new ConcurrentDictionary<string, BatchJobContext>();
            _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        }

        /// <inheritdoc />
        public async Task<BatchEnrichmentResult> ProcessCsvFileAsync(BatchEnrichmentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var jobContext = CreateJobContext(request);
            var linkedToken = CreateLinkedCancellationToken(request.JobId, cancellationToken);

            try
            {
                _logger.LogInformation("Starting CSV batch enrichment job {JobId} for file '{FileName}' ({FileSize} bytes)",
                    request.JobId, request.FileName, request.FileSizeBytes);

                // Parse CSV file
                ReportProgress(jobContext, BatchProcessingPhase.ParseFile, 0, 0, 0, 0, "Parsing CSV file...");
                
                var parseStopwatch = Stopwatch.StartNew();
                var rawNames = await ParseCsvFileAsync(request.FileContent, request.Options.CsvOptions, linkedToken);
                parseStopwatch.Stop();

                _logger.LogInformation("Parsed {Count} names from CSV file in {ElapsedMs}ms", rawNames.Count, parseStopwatch.ElapsedMilliseconds);

                // Process names
                var result = await ProcessNamesAsync(jobContext, rawNames, parseStopwatch.Elapsed, linkedToken);

                _logger.LogInformation("Completed CSV batch enrichment job {JobId}: {Successful}/{Total} successful",
                    request.JobId, result.Statistics.SuccessfulItems, result.Statistics.TotalItems);

                return result;
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                _logger.LogWarning("CSV batch enrichment job {JobId} was cancelled", request.JobId);
                ReportProgress(jobContext, BatchProcessingPhase.Cancelled, 0, 0, 0, 0, "Job was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV batch enrichment job {JobId} failed", request.JobId);
                ReportProgress(jobContext, BatchProcessingPhase.Failed, 0, 0, 0, 0, $"Job failed: {ex.Message}");
                
                return new BatchEnrichmentResult
                {
                    JobId = request.JobId,
                    Success = false,
                    Statistics = CreateEmptyStatistics(),
                    SuccessfulResults = Array.Empty<EnrichedNameResult>(),
                    FailedResults = Array.Empty<FailedNameEnrichment>(),
                    SkippedResults = Array.Empty<SkippedNameResult>(),
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                CleanupJob(request.JobId);
            }
        }

        /// <inheritdoc />
        public async Task<BatchEnrichmentResult> ProcessJsonFileAsync(BatchEnrichmentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var jobContext = CreateJobContext(request);
            var linkedToken = CreateLinkedCancellationToken(request.JobId, cancellationToken);

            try
            {
                _logger.LogInformation("Starting JSON batch enrichment job {JobId} for file '{FileName}' ({FileSize} bytes)",
                    request.JobId, request.FileName, request.FileSizeBytes);

                // Parse JSON file
                ReportProgress(jobContext, BatchProcessingPhase.ParseFile, 0, 0, 0, 0, "Parsing JSON file...");
                
                var parseStopwatch = Stopwatch.StartNew();
                var rawNames = await ParseJsonFileAsync(request.FileContent, request.Options.JsonOptions, linkedToken);
                parseStopwatch.Stop();

                _logger.LogInformation("Parsed {Count} names from JSON file in {ElapsedMs}ms", rawNames.Count, parseStopwatch.ElapsedMilliseconds);

                // Process names
                var result = await ProcessNamesAsync(jobContext, rawNames, parseStopwatch.Elapsed, linkedToken);

                _logger.LogInformation("Completed JSON batch enrichment job {JobId}: {Successful}/{Total} successful",
                    request.JobId, result.Statistics.SuccessfulItems, result.Statistics.TotalItems);

                return result;
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                _logger.LogWarning("JSON batch enrichment job {JobId} was cancelled", request.JobId);
                ReportProgress(jobContext, BatchProcessingPhase.Cancelled, 0, 0, 0, 0, "Job was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON batch enrichment job {JobId} failed", request.JobId);
                ReportProgress(jobContext, BatchProcessingPhase.Failed, 0, 0, 0, 0, $"Job failed: {ex.Message}");
                
                return new BatchEnrichmentResult
                {
                    JobId = request.JobId,
                    Success = false,
                    Statistics = CreateEmptyStatistics(),
                    SuccessfulResults = Array.Empty<EnrichedNameResult>(),
                    FailedResults = Array.Empty<FailedNameEnrichment>(),
                    SkippedResults = Array.Empty<SkippedNameResult>(),
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                CleanupJob(request.JobId);
            }
        }

        /// <inheritdoc />
        public Task<BatchEnrichmentStatus?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return Task.FromResult<BatchEnrichmentStatus?>(null);

            // Try to get from running jobs first
            if (_runningJobs.TryGetValue(jobId, out var jobContext))
            {
                var status = new BatchEnrichmentStatus
                {
                    JobId = jobId,
                    Phase = jobContext.CurrentProgress?.Phase ?? BatchProcessingPhase.Initializing,
                    StartedAt = jobContext.StartedAt,
                    CompletedAt = null,
                    CurrentProgress = jobContext.CurrentProgress,
                    Result = null
                };
                return Task.FromResult<BatchEnrichmentStatus?>(status);
            }

            // Try to get from cache (completed jobs)
            if (_cache.TryGetValue($"batch-job-status:{jobId}", out BatchEnrichmentStatus? cachedStatus))
            {
                return Task.FromResult(cachedStatus);
            }

            return Task.FromResult<BatchEnrichmentStatus?>(null);
        }

        /// <inheritdoc />
        public Task<bool> CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return Task.FromResult(false);

            if (_cancellationTokens.TryGetValue(jobId, out var cts))
            {
                try
                {
                    cts.Cancel();
                    _logger.LogInformation("Cancelled batch enrichment job {JobId}", jobId);
                    return Task.FromResult(true);
                }
                catch (ObjectDisposedException)
                {
                    // Job already completed
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<FileFormatInfo>> GetSupportedFormatsAsync()
        {
            var formats = new List<FileFormatInfo>
            {
                new FileFormatInfo
                {
                    Name = "CSV",
                    Extensions = new[] { ".csv", ".txt" },
                    ContentTypes = new[] { "text/csv", "text/plain", "application/csv" },
                    MaxSizeBytes = 100 * 1024 * 1024, // 100 MB
                    Description = "Comma-separated values with configurable delimiter",
                    SampleFormat = "FirstName,LastName,Culture\nJohn,Smith,English\nMaria,Garcia,Spanish"
                },
                new FileFormatInfo
                {
                    Name = "JSON",
                    Extensions = new[] { ".json" },
                    ContentTypes = new[] { "application/json", "text/json" },
                    MaxSizeBytes = 100 * 1024 * 1024, // 100 MB
                    Description = "JSON array of name objects",
                    SampleFormat = "[{\"firstName\":\"John\",\"lastName\":\"Smith\",\"culture\":\"English\"}]"
                }
            };

            return Task.FromResult<IReadOnlyList<FileFormatInfo>>(formats);
        }

        #region File Parsing Methods

        /// <summary>
        /// Parses a CSV file into raw name data
        /// </summary>
        private Task<List<RawNameData>> ParseCsvFileAsync(byte[] fileContent, CsvProcessingOptions options, CancellationToken cancellationToken)
        {
            var encoding = Encoding.GetEncoding(options.Encoding);
            var content = encoding.GetString(fileContent);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                throw new InvalidOperationException("CSV file is empty");

            var names = new List<RawNameData>();
            var headers = new Dictionary<string, int>();
            var startRow = 0;

            // Process headers if present
            if (options.HasHeader && lines.Length > 0)
            {
                var headerLine = lines[0];
                var headerColumns = ParseCsvLine(headerLine, options.Delimiter);
                
                for (int i = 0; i < headerColumns.Length; i++)
                {
                    headers[headerColumns[i].Trim()] = i;
                }
                startRow = 1;
            }

            // Process data rows
            for (int rowIndex = startRow; rowIndex < lines.Length; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var line = lines[rowIndex];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var columns = ParseCsvLine(line, options.Delimiter);
                    
                    var firstName = GetCsvValue(columns, options.FirstNameColumn, headers);
                    if (string.IsNullOrWhiteSpace(firstName))
                        continue; // Skip rows without first name

                    var lastName = GetCsvValue(columns, options.LastNameColumn, headers);
                    var culturalHint = GetCsvValue(columns, options.CulturalHintColumn, headers);

                    var nameData = new RawNameData
                    {
                        FirstName = firstName.Trim(),
                        LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
                        CulturalHint = string.IsNullOrWhiteSpace(culturalHint) ? null : culturalHint.Trim(),
                        RowNumber = rowIndex + 1
                    };

                    names.Add(nameData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse CSV row {RowNumber}: {Line}", rowIndex + 1, lines[rowIndex]);
                    // Continue with other rows
                }
            }

            return Task.FromResult(names);
        }

        /// <summary>
        /// Parses a JSON file into raw name data
        /// </summary>
        private Task<List<RawNameData>> ParseJsonFileAsync(byte[] fileContent, JsonProcessingOptions options, CancellationToken cancellationToken)
        {
            var content = Encoding.UTF8.GetString(fileContent);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = !options.CaseSensitive,
                AllowTrailingCommas = true
            };

            try
            {
                var jsonDocument = JsonDocument.Parse(content, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                var names = new List<RawNameData>();
                JsonElement arrayElement;

                // Navigate to the array using the array path
                if (options.ArrayPath == "$")
                {
                    arrayElement = jsonDocument.RootElement;
                }
                else
                {
                    // Simple path navigation (could be enhanced with JSONPath library)
                    arrayElement = NavigateJsonPath(jsonDocument.RootElement, options.ArrayPath);
                }

                if (arrayElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"JSON path '{options.ArrayPath}' does not point to an array");
                }

                var rowNumber = 1;
                foreach (var item in arrayElement.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var firstName = GetJsonStringValue(item, options.FirstNameProperty);
                        if (string.IsNullOrWhiteSpace(firstName))
                        {
                            rowNumber++;
                            continue; // Skip items without first name
                        }

                        var lastName = GetJsonStringValue(item, options.LastNameProperty);
                        var culturalHint = GetJsonStringValue(item, options.CulturalHintProperty);

                        var nameData = new RawNameData
                        {
                            FirstName = firstName.Trim(),
                            LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
                            CulturalHint = string.IsNullOrWhiteSpace(culturalHint) ? null : culturalHint.Trim(),
                            RowNumber = rowNumber
                        };

                        names.Add(nameData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JSON item at index {Index}", rowNumber - 1);
                        // Continue with other items
                    }

                    rowNumber++;
                }

                return Task.FromResult(names);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
            }
        }

        #endregion

        #region Name Processing Methods

        /// <summary>
        /// Processes raw name data and enriches with LLM analysis
        /// </summary>
        private async Task<BatchEnrichmentResult> ProcessNamesAsync(
            BatchJobContext jobContext, 
            List<RawNameData> rawNames, 
            TimeSpan fileParsingTime,
            CancellationToken cancellationToken)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var llmStopwatch = new Stopwatch();
            var dbStopwatch = new Stopwatch();

            var successfulResults = new List<EnrichedNameResult>();
            var failedResults = new List<FailedNameEnrichment>();
            var skippedResults = new List<SkippedNameResult>();

            var processedCount = 0;
            var totalTokens = 0;
            var totalCost = 0m;

            ReportProgress(jobContext, BatchProcessingPhase.ValidatingData, rawNames.Count, 0, 0, 0, "Validating name data...");

            // Filter and validate names
            var validNames = rawNames.Where(n => !string.IsNullOrWhiteSpace(n.FirstName)).ToList();
            var invalidCount = rawNames.Count - validNames.Count;

            if (invalidCount > 0)
            {
                _logger.LogWarning("Filtered out {InvalidCount} invalid name entries", invalidCount);
            }

            ReportProgress(jobContext, BatchProcessingPhase.ProcessingNames, validNames.Count, 0, 0, 0, "Starting name processing...");

            // Process names with controlled concurrency
            var semaphore = new SemaphoreSlim(jobContext.Request.Options.MaxConcurrency, jobContext.Request.Options.MaxConcurrency);
            var tasks = validNames.Select(async nameData =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var currentCount = Interlocked.Increment(ref processedCount);
                    ReportProgress(jobContext, BatchProcessingPhase.ProcessingNames, validNames.Count, currentCount, 
                        failedResults.Count, skippedResults.Count, $"Processing: {nameData.FirstName}");

                    // Check if name already exists (if skip option is enabled)
                    if (jobContext.Request.Options.SkipExistingNames)
                    {
                        dbStopwatch.Start();
                        var canonicalNameToCheck = !string.IsNullOrWhiteSpace(nameData.LastName) ? 
                            $"{nameData.FirstName} {nameData.LastName}" : nameData.FirstName;
                        var existing = await _personNameRepository.GetByCanonicalNameAsync(canonicalNameToCheck, cancellationToken);
                        dbStopwatch.Stop();

                        if (existing != null)
                        {
                            lock (skippedResults)
                            {
                                skippedResults.Add(new SkippedNameResult
                                {
                                    OriginalName = nameData.FirstName,
                                    Surname = nameData.LastName,
                                    RowNumber = nameData.RowNumber,
                                    SkipReason = "Name already exists in database",
                                    ExistingPersonNameId = existing.Id
                                });
                            }
                            return;
                        }
                    }

                    // Process with LLM
                    llmStopwatch.Start();
                    var itemStopwatch = Stopwatch.StartNew();
                    
                    var analysisRequest = new ComprehensiveNameAnalysisRequest
                    {
                        Name = nameData.FirstName,
                        Surname = nameData.LastName,
                        CulturalHints = nameData.CulturalHint != null ? new[] { nameData.CulturalHint } : Array.Empty<string>(),
                        Options = jobContext.Request.Options.AnalysisOptions,
                        Metadata = new NameAnalysisRequestMetadata
                        {
                            RequestId = $"{jobContext.Request.JobId}-{nameData.RowNumber}",
                            UserId = jobContext.Request.UserId,
                            AdditionalContext = new Dictionary<string, object>
                            {
                                { "BatchJobId", jobContext.Request.JobId },
                                { "RowNumber", nameData.RowNumber },
                                { "FileName", jobContext.Request.FileName }
                            }
                        }
                    };

                    var analysisResult = await _llmService.AnalyzeNameAsync(analysisRequest, cancellationToken);
                    itemStopwatch.Stop();
                    llmStopwatch.Stop();

                    // Save to database
                    dbStopwatch.Start();
                    var personName = await SavePersonNameAsync(nameData, analysisResult, cancellationToken);
                    dbStopwatch.Stop();

                    // Track statistics
                    if (analysisResult.Metadata.TotalTokensUsed.HasValue)
                        Interlocked.Add(ref totalTokens, analysisResult.Metadata.TotalTokensUsed.Value);
                    
                    if (analysisResult.Metadata.EstimatedCost.HasValue)
                        totalCost += analysisResult.Metadata.EstimatedCost.Value;

                    // Create success result
                    var confidenceScores = CreateConfidenceScoreSummary(analysisResult.CombinedAliases, analysisResult.Summary);
                    
                    lock (successfulResults)
                    {
                        successfulResults.Add(new EnrichedNameResult
                        {
                            OriginalName = nameData.FirstName,
                            Surname = nameData.LastName,
                            CulturalHint = nameData.CulturalHint,
                            PersonNameId = personName.Id,
                            AliasCount = analysisResult.CombinedAliases.Count,
                            ProcessingTime = itemStopwatch.Elapsed,
                            ConfidenceScores = confidenceScores
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process name '{Name}' from row {RowNumber}", nameData.FirstName, nameData.RowNumber);
                    
                    if (!jobContext.Request.Options.ContinueOnError)
                        throw;

                    lock (failedResults)
                    {
                        failedResults.Add(new FailedNameEnrichment
                        {
                            OriginalName = nameData.FirstName,
                            Surname = nameData.LastName,
                            RowNumber = nameData.RowNumber,
                            ErrorMessage = ex.Message,
                            ExceptionDetails = ex.ToString(),
                            FailedAt = DateTime.UtcNow
                        });
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            totalStopwatch.Stop();

            ReportProgress(jobContext, BatchProcessingPhase.SavingResults, validNames.Count, processedCount, 
                failedResults.Count, skippedResults.Count, "Finalizing results...");

            // Create output file if requested
            EnrichmentOutputFile? outputFile = null;
            if (jobContext.Request.Options.AnalysisOptions.MaxAliases > 0) // Simple condition to determine if output is wanted
            {
                outputFile = await CreateOutputFileAsync(successfulResults, jobContext.Request.FileName, cancellationToken);
            }

            var avgProcessingTime = successfulResults.Any() ? 
                TimeSpan.FromMilliseconds(successfulResults.Average(r => r.ProcessingTime.TotalMilliseconds)) : 
                TimeSpan.Zero;

            var result = new BatchEnrichmentResult
            {
                JobId = jobContext.Request.JobId,
                Success = failedResults.Count == 0 || jobContext.Request.Options.ContinueOnError,
                Statistics = new BatchEnrichmentStatistics
                {
                    TotalProcessingTime = totalStopwatch.Elapsed,
                    FileParsingTime = fileParsingTime,
                    LLMProcessingTime = llmStopwatch.Elapsed,
                    DatabaseTime = dbStopwatch.Elapsed,
                    TotalItems = rawNames.Count,
                    SuccessfulItems = successfulResults.Count,
                    FailedItems = failedResults.Count,
                    SkippedItems = skippedResults.Count,
                    AverageProcessingTime = avgProcessingTime,
                    TotalTokensUsed = totalTokens > 0 ? totalTokens : null,
                    TotalCost = totalCost > 0 ? totalCost : null
                },
                SuccessfulResults = successfulResults,
                FailedResults = failedResults,
                SkippedResults = skippedResults,
                OutputFile = outputFile
            };

            ReportProgress(jobContext, BatchProcessingPhase.Completed, validNames.Count, processedCount, 
                failedResults.Count, skippedResults.Count, "Job completed successfully");

            // Cache the result
            var completedStatus = new BatchEnrichmentStatus
            {
                JobId = jobContext.Request.JobId,
                Phase = BatchProcessingPhase.Completed,
                StartedAt = jobContext.StartedAt,
                CompletedAt = DateTime.UtcNow,
                CurrentProgress = null,
                Result = result
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), // Keep completed job info for 24 hours
                Priority = CacheItemPriority.Normal
            };
            _cache.Set($"batch-job-status:{jobContext.Request.JobId}", completedStatus, cacheOptions);

            return result;
        }

        /// <summary>
        /// Saves a PersonName entity and its aliases to the database
        /// </summary>
        private async Task<PersonName> SavePersonNameAsync(RawNameData nameData, ComprehensiveNameAnalysisResult analysisResult, CancellationToken cancellationToken)
        {
            // Create PersonName entity using the factory method
            var canonicalName = !string.IsNullOrWhiteSpace(nameData.LastName) ? 
                $"{nameData.FirstName} {nameData.LastName}" : nameData.FirstName;

            // Create default locale and script hints
            var defaultLocale = Domain.ValueObjects.Locale.Create(analysisResult.Summary.PrimaryCulture ?? "en");
            var defaultScript = Domain.ValueObjects.Script.Create("Latin");

            var personName = PersonName.Create(
                canonicalName,
                defaultLocale,
                defaultScript);

            await _personNameRepository.AddAsync(personName, cancellationToken);

            // Create NameAlias entities for each LLM-generated alias
            foreach (var llmAlias in analysisResult.CombinedAliases)
            {
                var aliasLocale = !string.IsNullOrWhiteSpace(llmAlias.CulturalContext) ? 
                    Domain.ValueObjects.Locale.Create(llmAlias.CulturalContext) : defaultLocale;

                // Map LLM alias type to domain enum
                var aliasType = MapLLMAliasTypeToDomain(llmAlias.AliasType);
                var aliasSource = Domain.Enums.AliasSource.Llm;

                var nameAlias = NameAlias.Create(
                    personName.Id,
                    llmAlias.Alias,
                    aliasType,
                    aliasLocale,
                    defaultScript,
                    aliasSource,
                    (decimal)llmAlias.Confidence);

                await _nameAliasRepository.AddAsync(nameAlias, cancellationToken);
            }

            // Mark the PersonName as enriched
            personName.MarkAsEnriched();
            await _personNameRepository.UpdateAsync(personName, cancellationToken);

            return personName;
        }

        /// <summary>
        /// Maps LLM alias type string to domain enum
        /// </summary>
        private Domain.Enums.AliasType MapLLMAliasTypeToDomain(string llmAliasType)
        {
            return llmAliasType?.ToLowerInvariant() switch
            {
                "nickname" => Domain.Enums.AliasType.Nickname,
                "formal" => Domain.Enums.AliasType.LlmGenerated,
                "diminutive" => Domain.Enums.AliasType.Diminutive,
                "transliteration" => Domain.Enums.AliasType.Transliteration,
                "cultural_variant" => Domain.Enums.AliasType.Cultural,
                "phonetic" => Domain.Enums.AliasType.LlmGenerated,
                _ => Domain.Enums.AliasType.Spelling
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a new job context
        /// </summary>
        private BatchJobContext CreateJobContext(BatchEnrichmentRequest request)
        {
            var context = new BatchJobContext
            {
                Request = request,
                StartedAt = DateTime.UtcNow,
                CurrentProgress = null
            };

            _runningJobs[request.JobId] = context;
            return context;
        }

        /// <summary>
        /// Creates a linked cancellation token for the job
        /// </summary>
        private CancellationToken CreateLinkedCancellationToken(string jobId, CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationTokens[jobId] = cts;
            return cts.Token;
        }

        /// <summary>
        /// Reports progress for a job
        /// </summary>
        private void ReportProgress(BatchJobContext context, BatchProcessingPhase phase, int total, int processed, int failed, int skipped, string? message)
        {
            var elapsed = DateTime.UtcNow - context.StartedAt;
            var rate = processed > 0 ? processed / elapsed.TotalSeconds : 0.0;
            var remaining = processed > 0 && rate > 0 ? TimeSpan.FromSeconds((total - processed) / rate) : (TimeSpan?)null;

            var progress = new BatchEnrichmentProgress
            {
                JobId = context.Request.JobId,
                Phase = phase,
                TotalItems = total,
                ProcessedItems = processed,
                FailedItems = failed,
                SkippedItems = skipped,
                CurrentItem = null,
                EstimatedTimeRemaining = remaining,
                ProcessingRate = rate,
                StatusMessage = message
            };

            context.CurrentProgress = progress;
            context.Request.ProgressCallback?.Report(progress);
        }

        /// <summary>
        /// Cleans up job resources
        /// </summary>
        private void CleanupJob(string jobId)
        {
            _runningJobs.TryRemove(jobId, out _);
            
            if (_cancellationTokens.TryRemove(jobId, out var cts))
            {
                cts.Dispose();
            }
        }

        /// <summary>
        /// Creates empty statistics for failed jobs
        /// </summary>
        private BatchEnrichmentStatistics CreateEmptyStatistics()
        {
            return new BatchEnrichmentStatistics
            {
                TotalProcessingTime = TimeSpan.Zero,
                FileParsingTime = TimeSpan.Zero,
                LLMProcessingTime = TimeSpan.Zero,
                DatabaseTime = TimeSpan.Zero,
                TotalItems = 0,
                SuccessfulItems = 0,
                FailedItems = 0,
                SkippedItems = 0,
                AverageProcessingTime = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Creates confidence score summary
        /// </summary>
        private ConfidenceScoreSummary CreateConfidenceScoreSummary(IReadOnlyList<LLMNameAlias> aliases, AnalysisSummary summary)
        {
            if (!aliases.Any())
            {
                return new ConfidenceScoreSummary
                {
                    AverageConfidence = 0.0,
                    HighestConfidence = 0.0,
                    HighConfidenceCount = 0,
                    CulturalConfidence = summary.CulturalConfidence
                };
            }

            return new ConfidenceScoreSummary
            {
                AverageConfidence = aliases.Average(a => a.Confidence),
                HighestConfidence = aliases.Max(a => a.Confidence),
                HighConfidenceCount = aliases.Count(a => a.Confidence >= 0.8),
                CulturalConfidence = summary.CulturalConfidence
            };
        }

        /// <summary>
        /// Creates an output file with enriched results
        /// </summary>
        private Task<EnrichmentOutputFile> CreateOutputFileAsync(List<EnrichedNameResult> results, string originalFileName, CancellationToken cancellationToken)
        {
            var outputData = results.Select(r => new
            {
                OriginalName = r.OriginalName,
                Surname = r.Surname,
                CulturalHint = r.CulturalHint,
                PersonNameId = r.PersonNameId,
                AliasCount = r.AliasCount,
                ProcessingTimeMs = r.ProcessingTime.TotalMilliseconds,
                AverageConfidence = r.ConfidenceScores.AverageConfidence,
                HighestConfidence = r.ConfidenceScores.HighestConfidence,
                HighConfidenceCount = r.ConfidenceScores.HighConfidenceCount,
                CulturalConfidence = r.ConfidenceScores.CulturalConfidence
            });

            var json = JsonSerializer.Serialize(outputData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            var content = Encoding.UTF8.GetBytes(json);
            var outputFileName = Path.ChangeExtension(originalFileName, ".enriched.json");

            return Task.FromResult(new EnrichmentOutputFile
            {
                FileName = outputFileName,
                ContentType = "application/json",
                Content = content
            });
        }

        #region CSV Parsing Helpers

        /// <summary>
        /// Parses a CSV line respecting quoted values
        /// </summary>
        private string[] ParseCsvLine(string line, char delimiter)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        /// <summary>
        /// Gets a value from CSV columns by name or index
        /// </summary>
        private string? GetCsvValue(string[] columns, string? columnNameOrIndex, Dictionary<string, int> headers)
        {
            if (string.IsNullOrWhiteSpace(columnNameOrIndex))
                return null;

            // Try as column name first
            if (headers.TryGetValue(columnNameOrIndex, out var columnIndex))
            {
                return columnIndex < columns.Length ? columns[columnIndex] : null;
            }

            // Try as numeric index
            if (int.TryParse(columnNameOrIndex, out var index) && index >= 0 && index < columns.Length)
            {
                return columns[index];
            }

            return null;
        }

        #endregion

        #region JSON Parsing Helpers

        /// <summary>
        /// Navigates a simple JSON path
        /// </summary>
        private JsonElement NavigateJsonPath(JsonElement element, string path)
        {
            if (path == "$")
                return element;

            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = element;

            foreach (var part in parts)
            {
                if (part == "$")
                    continue;

                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    throw new InvalidOperationException($"JSON path '{path}' not found");
                }
            }

            return current;
        }

        /// <summary>
        /// Gets a string value from a JSON element by property name
        /// </summary>
        private string? GetJsonStringValue(JsonElement element, string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return null;

            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
            }

            return null;
        }

        #endregion

        #endregion

        public void Dispose()
        {
            foreach (var cts in _cancellationTokens.Values)
            {
                cts.Dispose();
            }
            _cancellationTokens.Clear();
            _runningJobs.Clear();
        }

        /// <summary>
        /// Internal job context for tracking running jobs
        /// </summary>
        private class BatchJobContext
        {
            public required BatchEnrichmentRequest Request { get; init; }
            public required DateTime StartedAt { get; init; }
            public BatchEnrichmentProgress? CurrentProgress { get; set; }
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;
using System.Data;
using System.Text;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL implementation of the person repository
/// </summary>
public sealed class PersonRepository : IPersonRepository
{
    private readonly PhoneticAnalyzersDbContext _context;
    private readonly ILogger<PersonRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the PersonRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public PersonRepository(PhoneticAnalyzersDbContext context, ILogger<PersonRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Log connection details when repository is created
        LogConnectionDetails();
    }

    /// <summary>
    /// Logs database connection details for debugging
    /// </summary>
    private void LogConnectionDetails()
    {
        try
        {
            if (!_context.Database.IsRelational())
            {
                _logger.LogDebug("PersonRepository initialized with non-relational provider (e.g., InMemory) - skipping connection details.");
                return;
            }

            var connectionString = _context.Database.GetConnectionString();
            var maskedConnectionString = MaskConnectionStringPassword(connectionString ?? "");
            _logger.LogInformation("PersonRepository initialized with connection: {ConnectionString}", maskedConnectionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve connection string details");
        }
    }

    /// <summary>
    /// Masks the password in a connection string for secure logging
    /// </summary>
    /// <param name="connectionString">The original connection string</param>
    /// <returns>Connection string with password masked</returns>
    private static string MaskConnectionStringPassword(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Replace password value with asterisks
        var patterns = new[]
        {
            @"Password\s*=\s*[^;]+",
            @"Pwd\s*=\s*[^;]+",
            @"password\s*=\s*[^;]+"
        };

        var result = connectionString;
        foreach (var pattern in patterns)
        {
            result = System.Text.RegularExpressions.Regex.Replace(
                result, 
                pattern, 
                match => 
                {
                    var keyPart = match.Value.Split('=')[0];
                    return $"{keyPart}=***MASKED***";
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Person> AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        if (person == null)
            throw new ArgumentNullException(nameof(person));

        _logger.LogDebug("Adding person with external ID '{ExternalId}'", person.ExternalId.Value);

        try
        {
            // Log database connection attempt
            if (_context.Database.IsRelational())
            {
                _logger.LogDebug("Attempting database operation with connection: {ConnectionString}",
                    MaskConnectionStringPassword(_context.Database.GetConnectionString() ?? ""));
            }
            
            _logger.LogInformation("Person ID before Add: {PersonId}", person.Id);
            _context.Persons.Add(person);
            _logger.LogInformation("Entity state after Add: {State}", _context.Entry(person).State);
            
            _logger.LogInformation("Calling SaveChangesAsync to persist person {PersonId} with ExternalId '{ExternalId}'", 
                person.Id, person.ExternalId.Value);
            
            var changeCount = await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("SaveChangesAsync completed. Changes saved: {ChangeCount}, Person ID after save: {PersonId}", 
                changeCount, person.Id);
        }
        catch (Exception ex)
        {
            if (_context.Database.IsRelational())
            {
                _logger.LogError(ex, "Database operation failed. Connection: {ConnectionString}",
                    MaskConnectionStringPassword(_context.Database.GetConnectionString() ?? ""));
            }
            else
            {
                _logger.LogError(ex, "Database operation failed using non-relational provider.");
            }
            throw;
        }

        _logger.LogInformation("Successfully added person with ID {PersonId}", person.Id);
        return person;
    }

    /// <inheritdoc/>
    public async Task<Person> UpdateAsync(Person person, CancellationToken cancellationToken = default)
    {
        if (person == null)
            throw new ArgumentNullException(nameof(person));

        _logger.LogDebug("Updating person with ID {PersonId}", person.Id);

        _context.Persons.Update(person);
        
        _logger.LogInformation("Calling SaveChangesAsync to update person {PersonId}", person.Id);
        var changeCount = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SaveChangesAsync completed. Changes saved: {ChangeCount}", changeCount);

        _logger.LogInformation("Successfully updated person with ID {PersonId}", person.Id);
        return person;
    }

    /// <inheritdoc/>
    public async Task<Person?> GetByExternalIdAsync(ExternalId externalId, CancellationToken cancellationToken = default)
    {
        if (externalId == null)
            throw new ArgumentNullException(nameof(externalId));

        _logger.LogDebug("Getting person by external ID '{ExternalId}'", externalId.Value);

        var person = await _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);

        _logger.LogDebug("Person with external ID '{ExternalId}' {Found}", 
            externalId.Value, 
            person != null ? "found" : "not found");

        return person;
    }

    /// <inheritdoc/>
    public async Task<Person?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be greater than zero", nameof(id));

        _logger.LogDebug("Getting person by ID {PersonId}", id);

        var person = await _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        _logger.LogDebug("Person with ID {PersonId} {Found}", 
            id, 
            person != null ? "found" : "not found");

        return person;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PhoneticSearchResult>> SearchByPhoneticAsync(
        PhoneticSearchCriteria searchCriteria, 
        CancellationToken cancellationToken = default)
    {
        if (searchCriteria == null)
            throw new ArgumentNullException(nameof(searchCriteria));

        _logger.LogDebug("Starting phonetic search for '{QueryName}'", searchCriteria.QueryName.Value);

        var results = new List<PhoneticSearchResult>();

        // 1. Exact matches first
        await AddExactMatches(results, searchCriteria, cancellationToken);

        // 1b. Whole-token contains matches (e.g., query "JOHN" matches "JOHN MICHAEL SMITH")
        await AddTokenContainsMatches(results, searchCriteria, cancellationToken);

        // 2. Double Metaphone matches
        if (searchCriteria.PrimaryDoubleMetaphone != null)
        {
            await AddDoubleMetaphoneMatches(results, searchCriteria, cancellationToken);
        }

        // 3. Beider-Morse matches
        if (searchCriteria.BeiderMorseCodes.Any())
        {
            await AddBeiderMorseMatches(results, searchCriteria, cancellationToken);
        }

        // 4. Trigram similarity matches (if enabled and we need more results)
        if (searchCriteria.IncludeTrigramSimilarity && results.Count < searchCriteria.MaxResults)
        {
            await AddTrigramSimilarityMatches(results, searchCriteria, cancellationToken);
        }

        // 5. Nickname expansion matches (whole-token) if we still have room
        if (searchCriteria.NicknameVariants.Count > 0 && results.Count < searchCriteria.MaxResults)
        {
            await AddNicknameMatches(results, searchCriteria, cancellationToken);
        }

        // Remove duplicates and sort by similarity score
        var uniqueResults = results
            .GroupBy(r => r.Person.Id)
            .Select(g => g.OrderByDescending(r => r.SimilarityScore).First())
            .OrderByDescending(r => r.SimilarityScore)
            .ThenBy(r => r.Person.FullName)
            .Take(searchCriteria.MaxResults)
            .ToList();

        _logger.LogInformation(
            "Phonetic search for '{QueryName}' returned {ResultCount} results", 
            searchCriteria.QueryName.Value, 
            uniqueResults.Count);

        return uniqueResults;
    }

    /// <summary>
    /// Adds whole-token contains matches (query must match an entire word in the name)
    /// </summary>
    private async Task AddTokenContainsMatches(
        List<PhoneticSearchResult> results,
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var token = searchCriteria.QueryName.Value.Trim();
        if (token.Length < 2)
        {
            return;
        }

        // Build ILIKE patterns to enforce token boundaries using spaces.
        // We assume names are normalized to upper case with single spaces between tokens.
        var atStart = token + " %";        // e.g., "JOHN %" => JOHN <space> ...
        var inMiddle = "% " + token + " %"; // e.g., "% JOHN %"
        var atEnd = "% " + token;        // e.g., "% JOHN"

        var query = _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .Where(p =>
                EF.Functions.ILike(p.NormalizedName, atStart) ||
                EF.Functions.ILike(p.NormalizedName, inMiddle) ||
                EF.Functions.ILike(p.NormalizedName, atEnd));

        if (searchCriteria.CountyId.HasValue)
        {
            query = query.Where(p => p.CountyId == searchCriteria.CountyId.Value);
        }
        if (searchCriteria.RecordTypeFilter.HasValue)
        {
            query = query.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);
        }

        var tokenMatches = await query
            .Take(searchCriteria.MaxResults)
            .ToListAsync(cancellationToken);

        foreach (var match in tokenMatches)
        {
            // Give token contains matches a high score but below exact
            results.Add(new PhoneticSearchResult(
                match,
                0.95,
                PhoneticMatchType.TokenContains,
                $"Whole-word contains: '{token}'"));
        }

        _logger.LogDebug("Found {Count} token-contains matches for token '{Token}'", tokenMatches.Count, token);
    }

    /// <summary>
    /// Adds nickname variant whole-token matches
    /// </summary>
    private async Task AddNicknameMatches(
        List<PhoneticSearchResult> results,
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        if (searchCriteria.NicknameVariants.Count == 0)
            return;

        var tokens = searchCriteria.NicknameVariants
            .Select(v => v.Trim().ToUpperInvariant())
            .Where(v => v.Length >= 2)
            .Distinct()
            .ToList();

        if (tokens.Count == 0) return;

        var totalAdded = 0;
        foreach (var token in tokens)
        {
            var atStart = token + " %";
            var inMiddle = "% " + token + " %";
            var atEnd = "% " + token;

            var q = _context.Persons
                .Include(p => p.BeiderMorseVariants)
                .Where(p => EF.Functions.ILike(p.NormalizedName, atStart)
                         || EF.Functions.ILike(p.NormalizedName, inMiddle)
                         || EF.Functions.ILike(p.NormalizedName, atEnd));

            if (searchCriteria.CountyId.HasValue)
                q = q.Where(p => p.CountyId == searchCriteria.CountyId.Value);
            if (searchCriteria.RecordTypeFilter.HasValue)
                q = q.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);

            var matches = await q.Take(searchCriteria.MaxResults).ToListAsync(cancellationToken);
            foreach (var match in matches)
            {
                results.Add(new PhoneticSearchResult(
                    match,
                    0.93,
                    PhoneticMatchType.NicknameExpansion,
                    $"Nickname token match: '{token}'"));
                totalAdded++;
            }
        }

        _logger.LogDebug("Found {Count} nickname expansion matches across {TokenCount} tokens", totalAdded, tokens.Count);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Person>> UpsertBatchAsync(
        IEnumerable<Person> persons, 
        CancellationToken cancellationToken = default)
    {
        if (persons == null)
            throw new ArgumentNullException(nameof(persons));

        var personList = persons.ToList();
        _logger.LogDebug("Starting batch upsert for {Count} persons", personList.Count);

        var results = new List<Person>();

        foreach (var person in personList)
        {
            try
            {
                var existing = await GetByExternalIdAsync(person.ExternalId, cancellationToken);
                
                if (existing != null)
                {
                    // Update existing person
                    existing.Update(
                        person.FullName,
                        person.County,
                        person.CountyId,
                        person.CountyName,
                        person.Flag,
                        person.PrimaryDoubleMetaphone,
                        person.AlternateDoubleMetaphone,
                        person.BeiderMorseVariants.Select(bm => bm.BeiderMorseCode));

                    results.Add(existing);
                }
                else
                {
                    // Add new person
                    _context.Persons.Add(person);
                    results.Add(person);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during upsert for person with external ID '{ExternalId}'", 
                    person.ExternalId.Value);
                throw;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully completed batch upsert for {Count} persons", results.Count);
        return results;
    }

    /// <inheritdoc/>
    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Persons.LongCountAsync(cancellationToken);
        _logger.LogDebug("Total person count: {Count}", count);
        return count;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(ExternalId externalId, CancellationToken cancellationToken = default)
    {
        if (externalId == null)
            throw new ArgumentNullException(nameof(externalId));

        var exists = await _context.Persons
            .AnyAsync(p => p.ExternalId == externalId, cancellationToken);

        _logger.LogDebug("Person with external ID '{ExternalId}' exists: {Exists}", 
            externalId.Value, exists);

        return exists;
    }

    /// <summary>
    /// Adds exact name matches to the results
    /// </summary>
    private async Task AddExactMatches(
        List<PhoneticSearchResult> results,
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var exactQuery = _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .Where(p => p.NormalizedName == searchCriteria.QueryName);

        if (searchCriteria.CountyId.HasValue)
        {
            exactQuery = exactQuery.Where(p => p.CountyId == searchCriteria.CountyId.Value);
        }
        if (searchCriteria.RecordTypeFilter.HasValue)
        {
            exactQuery = exactQuery.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);
        }

        var exactMatches = await exactQuery
            .Take(searchCriteria.MaxResults)
            .ToListAsync(cancellationToken);

        foreach (var match in exactMatches)
        {
            results.Add(new PhoneticSearchResult(match, 1.0, PhoneticMatchType.Exact, "Exact name match"));
        }

        _logger.LogDebug("Found {Count} exact matches", exactMatches.Count);
    }

    /// <summary>
    /// Adds Double Metaphone matches to the results
    /// </summary>
    private async Task AddDoubleMetaphoneMatches(
        List<PhoneticSearchResult> results,
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        // Primary Double Metaphone matches
        if (searchCriteria.PrimaryDoubleMetaphone != null)
        {
            var primaryQuery = _context.Persons
                .Include(p => p.BeiderMorseVariants)
                .Where(p => p.PrimaryDoubleMetaphone == searchCriteria.PrimaryDoubleMetaphone);
            if (searchCriteria.CountyId.HasValue)
                primaryQuery = primaryQuery.Where(p => p.CountyId == searchCriteria.CountyId.Value);
            if (searchCriteria.RecordTypeFilter.HasValue)
                primaryQuery = primaryQuery.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);
            var primaryMatches = await primaryQuery
                .Take(searchCriteria.MaxResults)
                .ToListAsync(cancellationToken);

            foreach (var match in primaryMatches)
            {
                results.Add(new PhoneticSearchResult(match, 0.9, PhoneticMatchType.PrimaryDoubleMetaphone, 
                    $"Primary DM: {searchCriteria.PrimaryDoubleMetaphone.Value}"));
            }

            _logger.LogDebug("Found {Count} primary Double Metaphone matches", primaryMatches.Count);
        }

        // Alternate Double Metaphone matches
        if (searchCriteria.AlternateDoubleMetaphone != null)
        {
            var alternateQuery = _context.Persons
                .Include(p => p.BeiderMorseVariants)
                .Where(p => p.AlternateDoubleMetaphone == searchCriteria.AlternateDoubleMetaphone);
            if (searchCriteria.CountyId.HasValue)
                alternateQuery = alternateQuery.Where(p => p.CountyId == searchCriteria.CountyId.Value);
            if (searchCriteria.RecordTypeFilter.HasValue)
                alternateQuery = alternateQuery.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);
            var alternateMatches = await alternateQuery
                .Take(searchCriteria.MaxResults)
                .ToListAsync(cancellationToken);

            foreach (var match in alternateMatches)
            {
                results.Add(new PhoneticSearchResult(match, 0.85, PhoneticMatchType.AlternateDoubleMetaphone,
                    $"Alternate DM: {searchCriteria.AlternateDoubleMetaphone.Value}"));
            }

            _logger.LogDebug("Found {Count} alternate Double Metaphone matches", alternateMatches.Count);
        }
    }

    /// <summary>
    /// Adds Beider-Morse matches to the results
    /// </summary>
    private async Task AddBeiderMorseMatches(
        List<PhoneticSearchResult> results,
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var bmCodes = searchCriteria.BeiderMorseCodes.Select(c => c.Value).ToList();
        if (bmCodes.Count == 0)
        {
            return;
        }

        // Query variant table first to avoid complex navigation translation issues
        var matchingPersonIdsQuery = _context.BeiderMorseVariants
            .Where(bm => bmCodes.Contains(bm.BeiderMorseCode))
            .Select(bm => bm.PersonId)
            .Distinct();

        var bmQuery = _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .Where(p => matchingPersonIdsQuery.Contains(p.Id));
        if (searchCriteria.CountyId.HasValue)
            bmQuery = bmQuery.Where(p => p.CountyId == searchCriteria.CountyId.Value);
        if (searchCriteria.RecordTypeFilter.HasValue)
            bmQuery = bmQuery.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);
        var bmMatches = await bmQuery
            .Take(searchCriteria.MaxResults)
            .ToListAsync(cancellationToken);

        foreach (var match in bmMatches)
        {
            var matchingCode = match.BeiderMorseVariants
                .Select(v => v.BeiderMorseCode.Value)
                .FirstOrDefault(code => bmCodes.Contains(code));

            results.Add(new PhoneticSearchResult(match, 0.8, PhoneticMatchType.BeiderMorse,
                $"Beider-Morse: {matchingCode}"));
        }

        _logger.LogDebug("Found {Count} Beider-Morse matches", bmMatches.Count);
    }

    /// <summary>
    /// Adds trigram similarity matches to the results
    /// </summary>
    private async Task AddTrigramSimilarityMatches(
        List<PhoneticSearchResult> results,
        PhoneticSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        // Use PostgreSQL's trigram similarity
        // Note: In a real implementation, you would use FromSqlRaw with parameters
        // For now, using LIKE similarity as a placeholder
        // This is simplified for demonstration
        var similarityThreshold = searchCriteria.MinSimilarityThreshold;

        var trigramQuery = _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .Where(p => EF.Functions.TrigramsWordSimilarity(p.NormalizedName, searchCriteria.QueryName.Value) >= similarityThreshold);
        if (searchCriteria.CountyId.HasValue)
            trigramQuery = trigramQuery.Where(p => p.CountyId == searchCriteria.CountyId.Value);
        if (searchCriteria.RecordTypeFilter.HasValue)
            trigramQuery = trigramQuery.Where(p => p.Flag == searchCriteria.RecordTypeFilter.Value);
        var similarMatches = await trigramQuery
            .OrderByDescending(p => EF.Functions.TrigramsWordSimilarity(p.NormalizedName, searchCriteria.QueryName.Value))
            .Take(searchCriteria.MaxResults)
            .ToListAsync(cancellationToken);

        foreach (var match in similarMatches)
        {
            // Calculate similarity score (kept as backup; server-side filter already enforces threshold)
            var similarity = CalculateSimpleSimilarity(match.NormalizedName.Value, searchCriteria.QueryName.Value);
            
            if (similarity >= searchCriteria.MinSimilarityThreshold)
            {
                results.Add(new PhoneticSearchResult(match, similarity, PhoneticMatchType.TrigramSimilarity,
                    $"Trigram similarity: {similarity:F2}"));
            }
        }

        _logger.LogDebug("Found {Count} trigram similarity matches", similarMatches.Count);
    }

    /// <summary>
    /// Calculates a simple similarity score between two strings
    /// </summary>
    private static double CalculateSimpleSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0.0;

        if (a == b)
            return 1.0;

        var longer = a.Length > b.Length ? a : b;
        var shorter = a.Length > b.Length ? b : a;

        if (longer.Length == 0)
            return 1.0;

        return (longer.Length - ComputeLevenshteinDistance(longer, shorter)) / (double)longer.Length;
    }

    /// <summary>
    /// Computes the Levenshtein distance between two strings
    /// </summary>
    private static int ComputeLevenshteinDistance(string a, string b)
    {
        var distance = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; distance[i, 0] = i++) { }
        for (var j = 0; j <= b.Length; distance[0, j] = j++) { }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(Math.Min(
                    distance[i - 1, j] + 1,
                    distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[a.Length, b.Length];
    }

    /// <summary>
    /// Performs a high-performance bulk upsert operation optimized for millions of records
    /// </summary>
    public async Task<BulkUpsertResult> BulkUpsertAsync(IEnumerable<Person> persons, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bulk upsert operation");

        var personsList = persons.ToList();
        if (personsList.Count == 0)
        {
            return new BulkUpsertResult { Inserted = 0, Updated = 0 };
        }

        var processed = 0;

        try
        {
            // Get the connection string directly from the context to bypass EF Core retry strategy
            var connectionString = _context.Database.GetConnectionString();
            
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Use ADO.NET directly to avoid EF Core retry strategy conflicts
            foreach (var person in personsList)
            {
                // Insert/update person record (without BeiderMorseVariants - separate table)
                var personSql = """
                    INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, 
                                      dm_primary, dm_alternate, created_utc)
                    VALUES (@externalId, @fullName, @normalizedName, @county, @countyId, @countyName, @flag, 
                           @primaryDoubleMetaphone, @alternateDoubleMetaphone, @createdAt)
                    ON CONFLICT (external_id) 
                    DO UPDATE SET 
                        full_name = @fullName,
                        normalized_name = @normalizedName,
                        county = @county,
                        county_id = @countyId,
                        county_name = @countyName,
                        flag = @flag,
                        dm_primary = @primaryDoubleMetaphone,
                        dm_alternate = @alternateDoubleMetaphone,
                        updated_utc = CURRENT_TIMESTAMP
                    RETURNING id;
                """;

                using var personCommand = new NpgsqlCommand(personSql, connection);
                
                personCommand.Parameters.Add(new NpgsqlParameter("@externalId", person.ExternalId.Value));
                personCommand.Parameters.Add(new NpgsqlParameter("@fullName", person.FullName));
                personCommand.Parameters.Add(new NpgsqlParameter("@normalizedName", person.NormalizedName.Value));
                personCommand.Parameters.Add(new NpgsqlParameter("@county", person.County));
                personCommand.Parameters.Add(new NpgsqlParameter("@countyId", person.CountyId));
                personCommand.Parameters.Add(new NpgsqlParameter("@countyName", person.CountyName));
                personCommand.Parameters.Add(new NpgsqlParameter("@flag", (char)person.Flag));
                personCommand.Parameters.Add(new NpgsqlParameter("@primaryDoubleMetaphone", 
                    (object?)person.PrimaryDoubleMetaphone?.Value ?? DBNull.Value));
                personCommand.Parameters.Add(new NpgsqlParameter("@alternateDoubleMetaphone", 
                    (object?)person.AlternateDoubleMetaphone?.Value ?? DBNull.Value));
                personCommand.Parameters.Add(new NpgsqlParameter("@createdAt", DateTime.UtcNow));

                await personCommand.ExecuteNonQueryAsync(cancellationToken);
                
                // Skip BeiderMorseVariants for now - would need separate handling
                // TODO: Handle BeiderMorseVariants separately if needed
                
                processed++;

                // Log progress for larger batches
                if (processed % 100 == 0)
                {
                    _logger.LogDebug("Processed {Count} records so far", processed);
                }
            }

            _logger.LogInformation("Bulk upsert completed successfully. Processed: {Total}", processed);

            return new BulkUpsertResult { Inserted = processed, Updated = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk upsert failed");
            throw;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;
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
        var exactMatches = await _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .Where(p => p.NormalizedName == searchCriteria.QueryName)
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
            var primaryMatches = await _context.Persons
                .Include(p => p.BeiderMorseVariants)
                .Where(p => p.PrimaryDoubleMetaphone == searchCriteria.PrimaryDoubleMetaphone)
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
            var alternateMatches = await _context.Persons
                .Include(p => p.BeiderMorseVariants)
                .Where(p => p.AlternateDoubleMetaphone == searchCriteria.AlternateDoubleMetaphone)
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

        var bmMatches = await _context.Persons
            .Include(p => p.BeiderMorseVariants)
            .Where(p => matchingPersonIdsQuery.Contains(p.Id))
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
        var similarMatches = await _context.Persons
            .Include(p => p.BeiderMorseVariants)
            // Use the mapped property directly so EF can apply the value converter
            .Where(p => EF.Functions.Like(p.NormalizedName, $"%{searchCriteria.QueryName.Value}%"))
            .OrderByDescending(p => EF.Functions.TrigramsWordSimilarity(p.NormalizedName, searchCriteria.QueryName.Value))
            .Take(searchCriteria.MaxResults)
            .ToListAsync(cancellationToken);

        foreach (var match in similarMatches)
        {
            // Calculate similarity score (in real implementation, this would come from the SQL query)
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
}
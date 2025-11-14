using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Domain.Services;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure.Persistence;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for advanced person search using PostgreSQL pg_trgm and phonetic matching.
/// </summary>
public class PersonSearchRepository : IPersonSearchRepository
{
    private readonly PhoneticDbContext _context;
    private readonly IPhoneticEncodingService _phoneticService;

    public PersonSearchRepository(PhoneticDbContext context, IPhoneticEncodingService phoneticService)
    {
        _context = context;
        _phoneticService = phoneticService;
    }

    public async Task<IEnumerable<PersonSearchMatch>> SearchByNameAsync(
        string searchName,
        double minSimilarity = 0.3,
        bool expandNicknames = false,
        CancellationToken cancellationToken = default)
    {
        var normalized = _phoneticService.NormalizeName(searchName);

        // Use raw SQL with pg_trgm similarity function to return similarity scores
        var sql = @"
            SELECT p.person_id, p.external_id, p.full_name, p.normalized_name, 
                   p.primary_metaphone, p.alternate_metaphone, p.county, 
                   p.flag, p.created_utc, p.updated_utc,
                   CASE 
                       WHEN p.normalized_name = $1 THEN 1.0
                       ELSE similarity(p.normalized_name, $1)
                   END as similarity_score
            FROM person p
            WHERE 
                p.normalized_name = $1
                OR p.normalized_name ILIKE $2
                OR similarity(p.normalized_name, $1) >= $3
            ORDER BY similarity_score DESC
            LIMIT 100";

        var likePattern = $"%{normalized}%";
        var results = new List<PersonSearchMatch>();

        // Use ADO.NET with separate connection
        var connectionString = _context.Database.GetConnectionString();
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var command = new Npgsql.NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(normalized);
        command.Parameters.AddWithValue(likePattern);
        command.Parameters.AddWithValue(minSimilarity);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var person = new Person
            {
                PersonId = reader.GetInt64(0),
                ExternalId = reader.GetString(1),
                FullName = reader.GetString(2),
                NormalizedName = reader.GetString(3),
                PrimaryMetaphone = reader.IsDBNull(4) ? null : reader.GetString(4),
                AlternateMetaphone = reader.IsDBNull(5) ? null : reader.GetString(5),
                County = reader.IsDBNull(6) ? null : reader.GetString(6),
                Flag = reader.GetChar(7),
                CreatedUtc = reader.GetDateTime(8),
                UpdatedUtc = reader.GetDateTime(9)
            };

            var similarityScore = reader.GetDouble(10);

            results.Add(new PersonSearchMatch
            {
                Person = person,
                SimilarityScore = similarityScore
            });
        }

        // Load navigation properties for each person
        var personIds = results.Select(r => r.Person.PersonId).ToList();
        var personsWithNav = await _context.Persons
            .Where(p => personIds.Contains(p.PersonId))
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .ToListAsync(cancellationToken);

        // Update persons with navigation properties
        foreach (var result in results)
        {
            var personWithNav = personsWithNav.FirstOrDefault(p => p.PersonId == result.Person.PersonId);
            if (personWithNav != null)
            {
                result.Person = personWithNav;
            }
        }

        return results;
    }

    public async Task<IEnumerable<Person>> SearchByMetaphoneAsync(
        string searchName,
        CancellationToken cancellationToken = default)
    {
        var normalized = _phoneticService.NormalizeName(searchName);
        var (primaryMetaphone, alternateMetaphone) = _phoneticService.GetDoubleMetaphone(normalized);

        if (string.IsNullOrEmpty(primaryMetaphone))
            return Enumerable.Empty<Person>();

        var query = _context.Persons
            .Where(p => p.PrimaryMetaphone == primaryMetaphone 
                     || p.AlternateMetaphone == primaryMetaphone);

        if (!string.IsNullOrEmpty(alternateMetaphone))
        {
            query = query.Union(_context.Persons
                .Where(p => p.PrimaryMetaphone == alternateMetaphone 
                         || p.AlternateMetaphone == alternateMetaphone));
        }

        return await query
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Person>> SearchByBeiderMorseAsync(
        string searchName,
        CancellationToken cancellationToken = default)
    {
        var normalized = _phoneticService.NormalizeName(searchName);
        var bmCodes = _phoneticService.GetBeiderMorseCodes(normalized).ToList();

        if (!bmCodes.Any())
            return Enumerable.Empty<Person>();

        // Search in person_bm table only (beider_morse column doesn't exist in person table)
        var personsFromBmTable = await _context.Persons
            .Where(p => p.PersonBms.Any(bm => bmCodes.Contains(bm.BmCode)))
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .ToListAsync(cancellationToken);

        return personsFromBmTable;
    }

    public async Task<IEnumerable<Person>> SearchByTrigramAsync(
        string searchName,
        double minSimilarity = 0.3,
        CancellationToken cancellationToken = default)
    {
        var normalized = _phoneticService.NormalizeName(searchName);

        // Use raw SQL with pg_trgm similarity function
        var sql = @"
            SELECT p.*
            FROM person p
            WHERE similarity(p.normalized_name, {0}) >= {1}
            ORDER BY similarity(p.normalized_name, {0}) DESC
            LIMIT 100";

        return await _context.Persons
            .FromSqlRaw(sql, normalized, minSimilarity)
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .ToListAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure.Persistence;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Person entity using EF Core and scaffolded models.
/// </summary>
public class PersonRepository : IPersonRepository
{
    private readonly PhoneticDbContext _context;

    public PersonRepository(PhoneticDbContext context)
    {
        _context = context;
    }

    public async Task<Person?> GetByIdAsync(long personId, CancellationToken cancellationToken = default)
    {
        return await _context.Persons
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);
    }

    public async Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Persons
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<IEnumerable<Person>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Persons
            .Include(p => p.PersonNames)
            .Include(p => p.PersonBms)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        _context.Persons.Add(person);
        await _context.SaveChangesAsync(cancellationToken);
        return person.PersonId;
    }

    public async Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
    {
        _context.Persons.Update(person);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long personId, CancellationToken cancellationToken = default)
    {
        var person = await _context.Persons.FindAsync(new object[] { personId }, cancellationToken);
        if (person != null)
        {
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Persons
            .AnyAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Persons.CountAsync(cancellationToken);
    }

    public async Task<List<string>> GetNameSuggestionsAsync(string prefix, int maxSuggestions = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
        {
            return new List<string>();
        }

        var normalizedPrefix = prefix.Trim().ToLowerInvariant();

        // Use raw SQL to query the normalized_name column directly
        // ILIKE is case-insensitive in PostgreSQL
        // Using a subquery with LIMIT before DISTINCT for better performance
        var sql = @"
            SELECT full_name 
            FROM (
                SELECT DISTINCT ON (full_name) full_name
                FROM person 
                WHERE normalized_name ILIKE {0} || '%'
                ORDER BY full_name
                LIMIT {1}
            ) subq";

        var suggestions = await _context.Database
            .SqlQueryRaw<string>(sql, normalizedPrefix, maxSuggestions)
            .ToListAsync(cancellationToken);

        return suggestions;
    }
}

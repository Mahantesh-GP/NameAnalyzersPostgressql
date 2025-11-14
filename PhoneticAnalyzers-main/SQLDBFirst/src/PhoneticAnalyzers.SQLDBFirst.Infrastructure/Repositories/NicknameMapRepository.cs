using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure.Persistence;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for NicknameMap operations.
/// Handles bidirectional nickname lookups from database.
/// </summary>
public class NicknameMapRepository : INicknameMapRepository
{
    private readonly PhoneticDbContext _context;

    public NicknameMapRepository(PhoneticDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<string>> GetNicknamesAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.ToUpperInvariant().Trim();

        // Get direct mappings (canonical -> nickname)
        var directNicknames = await _context.NicknameMaps
            .Where(nm => nm.CanonicalName == normalized)
            .Select(nm => nm.Nickname)
            .ToListAsync(cancellationToken);

        // Get reverse mappings if bidirectional (nickname -> canonical)
        var reverseMappings = await _context.NicknameMaps
            .Where(nm => nm.Nickname == normalized && nm.IsBidirectional)
            .Select(nm => nm.CanonicalName)
            .ToListAsync(cancellationToken);

        // For each reverse canonical name, get all its nicknames
        var additionalNicknames = new List<string>();
        foreach (var canonical in reverseMappings)
        {
            var nicknames = await _context.NicknameMaps
                .Where(nm => nm.CanonicalName == canonical)
                .Select(nm => nm.Nickname)
                .ToListAsync(cancellationToken);
            additionalNicknames.AddRange(nicknames);
        }

        // Combine and deduplicate
        return directNicknames
            .Concat(reverseMappings)
            .Concat(additionalNicknames)
            .Distinct()
            .Where(n => !n.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IEnumerable<string>> GetCanonicalNamesAsync(string nickname, CancellationToken cancellationToken = default)
    {
        var normalized = nickname.ToUpperInvariant().Trim();

        return await _context.NicknameMaps
            .Where(nm => nm.Nickname == normalized)
            .Select(nm => nm.CanonicalName)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NicknameMap>> GetAllMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NicknameMaps.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NicknameMap nicknameMap, CancellationToken cancellationToken = default)
    {
        _context.NicknameMaps.Add(nicknameMap);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string canonicalName, string nickname, CancellationToken cancellationToken = default)
    {
        var normalizedCanonical = canonicalName.ToUpperInvariant().Trim();
        var normalizedNickname = nickname.ToUpperInvariant().Trim();

        return await _context.NicknameMaps
            .AnyAsync(nm => nm.CanonicalName == normalizedCanonical && nm.Nickname == normalizedNickname, 
                cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NicknameMaps.CountAsync(cancellationToken);
    }
}

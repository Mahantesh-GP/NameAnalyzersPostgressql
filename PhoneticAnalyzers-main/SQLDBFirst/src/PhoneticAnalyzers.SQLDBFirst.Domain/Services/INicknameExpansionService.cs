namespace PhoneticAnalyzers.SQLDBFirst.Domain.Services;

/// <summary>
/// Service interface for nickname expansion operations.
/// Expands search names to include nickname variants (e.g., William → Bill, Will, Billy).
/// </summary>
public interface INicknameExpansionService
{
    /// <summary>
    /// Get all nickname variants for a given name.
    /// </summary>
    Task<IEnumerable<string>> GetNicknameVariantsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a name has any nicknames in the database.
    /// </summary>
    Task<bool> HasNicknamesAsync(string name, CancellationToken cancellationToken = default);
}

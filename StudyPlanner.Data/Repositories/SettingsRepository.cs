using Microsoft.EntityFrameworkCore;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Core.Entities;

namespace StudyPlanner.Data.Repositories;

/// <summary>
/// Data-layer implementation of ISettingsRepository.
/// Manages persistence of application-wide settings using Entity Framework Core and SQLite.
/// Ensures a singleton-like behavior by always returning the single existing record,
/// or creating one with default values if none exists.
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly StudyPlannerDbContext _context;

    /// <summary>
    /// Constructor receives DbContext via Dependency Injection.
    /// </summary>
    public SettingsRepository(StudyPlannerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves the current application settings.
    /// If no settings exist in the database (first run), creates a new record with default values.
    /// </summary>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>The AppSettings entity.</returns>
    public async Task<AppSettings> GetOrCreateAsync(CancellationToken ct = default)
    {
        // Attempt to fetch the existing settings record.
        // AsNoTracking() improves performance for read-only scenarios.
        var settings = await _context.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        // If no record exists, create one with defaults.
        if (settings == null)
        {
            settings = new AppSettings(); // Defaults are set in the entity constructor

            // Add to context and save immediately to ensure ID is generated and persisted.
            _context.AppSettings.Add(settings);
            await _context.SaveChangesAsync(ct);
        }

        return settings;
    }

    /// <summary>
    /// Updates the application settings in the database.
    /// </summary>
    /// <param name="settings">The updated AppSettings entity.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    public async Task UpdateAsync(AppSettings settings, CancellationToken ct = default)
    {
        // Attach the entity to the context and mark it as modified.
        // Since we likely fetched this without tracking (or from a different scope),
        // Update() ensures EF knows to generate an UPDATE statement for all properties.
        _context.AppSettings.Update(settings);

        // Persist changes to SQLite.
        await _context.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.ValueObjects;

namespace StudyPlanner.Data.Services
{
    /// <summary>
    /// Data-layer implementation of IStudySessionService.
    /// Responsible for persisting and retrieving study session records from the database.
    /// Located in the Data layer because it directly depends on Entity Framework Core's DbContext.
    /// This separation follows Clean Architecture principles: Application defines contracts, Data implements them.
    /// </summary>
    public class StudySessionService : IStudySessionService
    {
        private readonly StudyPlannerDbContext _context;

        /// <summary>
        /// Constructor receives DbContext via Dependency Injection.
        /// The DI container provides the configured database context at runtime.
        /// </summary>
        public StudySessionService(StudyPlannerDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Records a new study session in the database.
        /// Typically called automatically when a Pomodoro work phase completes.
        /// </summary>
        /// <param name="subjectId">The ID of the subject being studied.</param>
        /// <param name="sessionDate">The date the session occurred.</param>
        /// <param name="duration">The duration of the study session.</param>
        /// <param name="ct">Cancellation token for async operations.</param>
        public async Task RecordSessionAsync(Guid subjectId, DateTime sessionDate, TimeSpan duration, CancellationToken ct = default)
        {
            // Create a new StudySession entity using the domain factory method.
            // This ensures validation rules (e.g., non-empty SubjectId) are enforced.
            var session = StudySession.Create(subjectId, sessionDate, Duration.Create(duration));

            // Add the new entity to the DbContext's change tracker.
            // EF Core marks this entity as "Added" and prepares an INSERT statement.
            await _context.StudySessions.AddAsync(session, ct);

            // Persist changes to the SQLite database.
            // This executes the SQL INSERT command within a transaction.
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Retrieves all study sessions within a specified date range.
        /// Used primarily for generating weekly statistics and charts.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <param name="ct">Cancellation token for async operations.</param>
        /// <returns>A read-only list of study sessions.</returns>
        public async Task<IReadOnlyList<StudySession>> GetWeeklySessionsAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            // Query the database for sessions within the given date range.
            // AsNoTracking(): Improves performance by disabling change tracking for read-only queries.
            // Where(): Filters records to only include those within the specified dates.
            // ToListAsync(): Executes the query asynchronously and materializes results into a list.
            return await _context.StudySessions
                .AsNoTracking()
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .ToListAsync(ct);
        }
    }
}

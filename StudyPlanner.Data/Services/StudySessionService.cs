using Microsoft.EntityFrameworkCore;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.ValueObjects;

namespace StudyPlanner.Data.Services
{

    /// <summary>
    /// Concrete implementation of IStudySessionService.
    /// Located in Data layer because it directly accesses DbContext.
    /// This follows Clean Architecture: Application defines interface, Data implements it.
    /// </summary>
    public class StudySessionService : IStudySessionService
    {
        private readonly StudyPlannerDbContext _context;

        /// <summary>
        /// Constructor receives DbContext via Dependency Injection.
        /// EF Core will provide the configured database context.
        /// </summary>
        public StudySessionService(StudyPlannerDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Records a new study session in the database.
        /// This is called automatically when the Pomodoro timer completes a work phase.
        /// </summary>
        public async Task RecordSessionAsync(Guid subjectId, DateTime sessionDate, TimeSpan duration, CancellationToken ct = default)
        {
            // Create a new StudySession entity using the factory method
            // The factory validates the data and creates a properly initialized entity
            var session = StudySession.Create(subjectId, sessionDate, Duration.Create(duration));

            // Add the session to the DbContext's Change Tracker
            // This marks it as "new" and ready to be inserted
            await _context.StudySessions.AddAsync(session, ct);

            // Save changes to the database
            // This executes an INSERT SQL command
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Retrieves all study sessions within a date range.
        /// Used to calculate weekly statistics for the chart.
        /// </summary>
        public async Task<IReadOnlyList<StudySession>> GetWeeklySessionsAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            // Query the database for sessions within the specified date range
            // AsNoTracking() improves performance since we're only reading (not updating)
            // Where() filters by date range
            // ToListAsync() executes the query and returns results as a list
            return await _context.StudySessions
                .AsNoTracking()
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .ToListAsync(ct);
        }
    }
}

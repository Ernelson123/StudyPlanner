using StudyPlanner.Core.Entities;

namespace StudyPlanner.Application.Services;

public interface IStudySessionService
{
    Task RecordSessionAsync(Guid subjectId, DateTime sessionDate, TimeSpan duration, CancellationToken ct = default);
    Task<IReadOnlyList<StudySession>> GetWeeklySessionsAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

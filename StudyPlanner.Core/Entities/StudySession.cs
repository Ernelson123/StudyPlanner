namespace StudyPlanner.Core.Entities;

using StudyPlanner.Core.Exceptions;
using StudyPlanner.Core.ValueObjects;

public class StudySession
{
    public Guid Id { get; private set; }
    public Guid SubjectId { get; private set; }
    public DateTime Date { get; private set; }
    public Duration Duration { get; private set; }

    private StudySession(Guid id, Guid subjectId, DateTime date, Duration duration)
    {
        Id = id;
        SubjectId = subjectId;
        Date = date;
        Duration = duration;
    }

    public static StudySession Create(Guid subjectId, DateTime date, Duration duration)
    {
        if (subjectId == Guid.Empty)
            throw new DomainException("Subject ID cannot be empty.");

        return new StudySession(Guid.NewGuid(), subjectId, date.Date, duration);
    }
}

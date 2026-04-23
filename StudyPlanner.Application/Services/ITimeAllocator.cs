using StudyPlanner.Core.Entities;

namespace StudyPlanner.Application.Services;

public interface ITimeAllocator
{
    Dictionary<Guid, TimeSpan> AllocateTime(IEnumerable<Subject> subjects, TimeSpan totalTime);
}

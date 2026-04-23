using StudyPlanner.Core.Entities;

namespace StudyPlanner.Application.Services;

public class TimeAllocator : ITimeAllocator
{
    public Dictionary<Guid, TimeSpan> AllocateTime(IEnumerable<Subject> subjects, TimeSpan totalTime)
    {
        var subjectsList = subjects.ToList();
        if (subjectsList.Count == 0) return new Dictionary<Guid, TimeSpan>();

        double totalWeight = subjectsList.Sum(s => s.PriorityWeight);
        if (totalWeight == 0) throw new ArgumentException("Total weight cannot be zero.");

        double secondsPerWeight = totalTime.TotalSeconds / totalWeight;
        var allocation = new Dictionary<Guid, TimeSpan>();

        foreach (var subject in subjectsList)
        {
            var seconds = subject.PriorityWeight * secondsPerWeight;
            allocation[subject.Id] = TimeSpan.FromSeconds(Math.Floor(seconds));
        }

        double allocatedSeconds = allocation.Values.Sum(t => t.TotalSeconds);
        double remainingSeconds = totalTime.TotalSeconds - allocatedSeconds;

        if (remainingSeconds > 0 && allocation.Count > 0)
        {
            var firstKey = allocation.Keys.First();
            allocation[firstKey] = allocation[firstKey].Add(TimeSpan.FromSeconds(remainingSeconds));
        }

        return allocation;
    }
}

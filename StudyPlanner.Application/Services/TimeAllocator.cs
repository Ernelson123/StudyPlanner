using StudyPlanner.Core.Entities;

namespace StudyPlanner.Application.Services
{
    /// <summary>
    /// Service responsible for calculating study time allocation based on subject priority weights.
    /// Implements a weighted proportional distribution algorithm.
    /// </summary>
    public class TimeAllocator : ITimeAllocator
    {
        /// <summary>
        /// Allocates a total available time span among a list of subjects based on their priority weights.
        /// Higher priority subjects receive more time.
        /// </summary>
        /// <param name="subjects">Collection of subjects with priority weights.</param>
        /// <param name="totalTime">The total time available to be distributed.</param>
        /// <returns>A dictionary mapping Subject IDs to their allocated TimeSpan.</returns>
        /// <exception cref="ArgumentException">Thrown if subjects are empty or total weight is zero.</exception>
        public Dictionary<Guid, TimeSpan> AllocateTime(IEnumerable<Subject> subjects, TimeSpan totalTime)
        {
            // Convert to list to allow multiple iterations without re-enumerating the source
            var subjectsList = subjects.ToList();

            // Validate input: No subjects means no allocation needed
            if (subjectsList.Count == 0)
                return new Dictionary<Guid, TimeSpan>();

            // Calculate the sum of all priority weights
            double totalWeight = subjectsList.Sum(s => s.PriorityWeight);

            // Validate: Cannot divide by zero weight
            if (totalWeight <= 0)
                throw new ArgumentException("Total priority weight must be greater than zero.", nameof(subjects));

            // Calculate how many seconds each "point" of priority is worth
            double secondsPerPriorityPoint = totalTime.TotalSeconds / totalWeight;

            var allocation = new Dictionary<Guid, TimeSpan>();

            // Initial allocation: Assign time based on weight, rounding down to avoid over-allocation
            foreach (var subject in subjectsList)
            {
                var seconds = subject.PriorityWeight * secondsPerPriorityPoint;
                // Math.Floor ensures we don't exceed total time due to floating-point precision issues
                allocation[subject.Id] = TimeSpan.FromSeconds(Math.Floor(seconds));
            }

            // Calculate remaining seconds due to rounding down
            double allocatedSeconds = allocation.Values.Sum(t => t.TotalSeconds);
            double remainingSeconds = totalTime.TotalSeconds - allocatedSeconds;

            // Distribute any remaining seconds to the first subject to ensure exact total match
            if (remainingSeconds > 0 && allocation.Count > 0)
            {
                var firstKey = allocation.Keys.First();
                allocation[firstKey] = allocation[firstKey].Add(TimeSpan.FromSeconds(remainingSeconds));
            }

            return allocation;
        }
    }
}

using StudyPlanner.Core.Exceptions;

namespace StudyPlanner.Core.Entities
{
    /// <summary>
    /// Represents a study subject (e.g., "Mathematics", "History").
    /// This entity uses encapsulation to ensure data integrity and validity.
    /// Instances are created via the static Create factory method to enforce domain rules.
    /// </summary>
    public class Subject
    {
        /// <summary>
        /// Unique identifier for the subject.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The name of the subject. Cannot be null or whitespace.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The priority weight used for time allocation algorithms.
        /// Higher values indicate higher priority. Must be positive.
        /// </summary>
        public int PriorityWeight { get; private set; }

        // ========================================================================
        // Domain Logic: Private Constructor & Factory Method
        // ========================================================================

        /// <summary>
        /// Private constructor to enforce creation via the static Factory Method.
        /// Ensures all required fields are provided and validated.
        /// </summary>
        private Subject(Guid id, string name, int priorityWeight)
        {
            Id = id;
            Name = name;
            PriorityWeight = priorityWeight;
        }

        /// <summary>
        /// Factory method to create a new Subject instance.
        /// Performs domain validation before instantiation.
        /// </summary>
        /// <param name="name">The name of the subject.</param>
        /// <param name="priorityWeight">The priority weight (default is 1).</param>
        /// <returns>A new Subject instance.</returns>
        /// <exception cref="DomainException">Thrown if name is empty or priority is invalid.</exception>
        public static Subject Create(string name, int priorityWeight = 1)
        {
            // Domain Validation: Ensure name is valid
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Subject name cannot be empty.");

            // Domain Validation: Ensure priority is positive
            if (priorityWeight <= 0)
                throw new DomainException("Priority weight must be positive.");

            // Trim whitespace from name for consistency
            var trimmedName = name.Trim();

            return new Subject(
                id: Guid.NewGuid(),
                name: trimmedName,
                priorityWeight: priorityWeight
            );
        }
    }
}

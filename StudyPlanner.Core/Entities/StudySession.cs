using StudyPlanner.Core.Exceptions;
using StudyPlanner.Core.ValueObjects;

namespace StudyPlanner.Core.Entities
{
    /// <summary>
    /// Represents a recorded study session.
    /// This entity tracks when a subject was studied and for how long.
    /// It uses encapsulation to ensure data integrity and immutability after creation.
    /// </summary>
    public class StudySession
    {
        /// <summary>
        /// Unique identifier for the study session.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Foreign key linking this session to a specific Subject.
        /// </summary>
        public Guid SubjectId { get; private set; }

        /// <summary>
        /// The date when the study session occurred.
        /// Stored as Date only (time component is normalized to midnight).
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// The duration of the study session, represented by the Duration Value Object.
        /// </summary>
        public Duration Duration { get; private set; }

        // ========================================================================
        // EF Core Support: Private Parameterless Constructor
        // ========================================================================
        /// <summary>
        /// Private parameterless constructor required by Entity Framework Core 
        /// for materializing entities from the database.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        private StudySession() { }
#pragma warning restore CS8618

        // ========================================================================
        // Domain Logic: Private Constructor & Factory Method
        // ========================================================================
        /// <summary>
        /// Private constructor to enforce creation via the static Factory Method.
        /// Ensures all required fields are provided and validated.
        /// </summary>
        private StudySession(Guid id, Guid subjectId, DateTime date, Duration duration)
        {
            Id = id;
            SubjectId = subjectId;
            Date = date; // Assumes date is already normalized to .Date by caller or DB
            Duration = duration;
        }

        /// <summary>
        /// Factory method to create a new StudySession instance.
        /// Performs domain validation before instantiation.
        /// </summary>
        /// <param name="subjectId">The ID of the subject being studied.</param>
        /// <param name="date">The date of the study session.</param>
        /// <param name="duration">The duration of the study session.</param>
        /// <returns>A new StudySession instance.</returns>
        /// <exception cref="DomainException">Thrown if SubjectId is empty.</exception>
        public static StudySession Create(Guid subjectId, DateTime date, Duration duration)
        {
            // Domain Validation: Ensure the subject exists
            if (subjectId == Guid.Empty)
                throw new DomainException("Subject ID cannot be empty.");

            // Normalize date to remove time component for consistent grouping/reporting
            var normalizedDate = date.Date;

            return new StudySession(
                id: Guid.NewGuid(),
                subjectId: subjectId,
                date: normalizedDate,
                duration: duration
            );
        }
    }
}

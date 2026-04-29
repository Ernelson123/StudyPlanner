using System;

namespace StudyPlanner.Core.Events
{

    /// <summary>
    /// Event arguments for when a Pomodoro work phase is completed.
    /// This follows the .NET event pattern (inherits from EventArgs).
    /// </summary>
    public class WorkPhaseCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the duration of the completed work phase.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance with the specified duration.
        /// </summary>
        public WorkPhaseCompletedEventArgs(TimeSpan duration)
        {
            Duration = duration;
        }
    }
}

namespace StudyPlanner.Core.Entities
{
    /// <summary>
    /// Represents the global configuration settings for the application.
    /// This entity is designed to have a single record in the database (Id = 1),
    /// acting as a singleton configuration store.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Primary Key to ensure only one configuration record exists in the database.
        /// </summary>
        public int Id { get; set; } = 1;

        /// <summary>
        /// Duration of the Work session in minutes.
        /// Default value: 25 minutes.
        /// </summary>
        public int WorkDurationMinutes { get; set; } = 25;

        /// <summary>
        /// Duration of the Short Break session in minutes.
        /// Default value: 5 minutes.
        /// </summary>
        public int ShortBreakDurationMinutes { get; set; } = 5;

        /// <summary>
        /// Duration of the Long Break session in minutes.
        /// Default value: 15 minutes.
        /// </summary>
        public int LongBreakDurationMinutes { get; set; } = 15;
    }
}

using Microsoft.EntityFrameworkCore;
using StudyPlanner.Core.Entities;

namespace StudyPlanner.Data
{
    /// <summary>
    /// The main Entity Framework Core DbContext for the Study Planner application.
    /// Manages database connections, change tracking, and schema configuration for SQLite.
    /// </summary>
    public class StudyPlannerDbContext : DbContext
    {
        /// <summary>
        /// Constructor receives DbContextOptions via Dependency Injection.
        /// These options are configured in App.xaml.cs (e.g., UseSqlite).
        /// </summary>
        public StudyPlannerDbContext(DbContextOptions<StudyPlannerDbContext> options)
            : base(options) { }

        // ========================================================================
        // DbSets: Represent tables in the database
        // ========================================================================

        /// <summary>
        /// Represents the 'Subjects' table.
        /// Stores all study subjects/topics.
        /// </summary>
        public DbSet<Subject> Subjects => Set<Subject>();

        /// <summary>
        /// Represents the 'StudySessions' table.
        /// Stores individual recorded study sessions linked to subjects.
        /// </summary>
        public DbSet<StudySession> StudySessions => Set<StudySession>();

        /// <summary>
        /// Represents the 'AppSettings' table.
        /// Stores global application configuration (singleton record).
        /// </summary>
        public DbSet<AppSettings> AppSettings => Set<AppSettings>();

        // ========================================================================
        // Model Configuration: Defines schema, keys, and relationships
        // ========================================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configure Subject Entity ---
#pragma warning disable CA1062 // Validate arguments of public methods
            modelBuilder.Entity<Subject>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // Property Configurations
                entity.Property(e => e.Name)
                    .IsRequired() // Name cannot be null
                    .HasMaxLength(100); // Limit length for performance

                entity.Property(e => e.PriorityWeight)
                    .IsRequired(); // Priority must have a value
            });
#pragma warning restore CA1062 // Validate arguments of public methods

            // --- Configure StudySession Entity ---
            modelBuilder.Entity<StudySession>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // Foreign Key Relationship to Subject
                entity.HasOne<Subject>() // Assuming no navigation property back to Subject in Session for simplicity
                      .WithMany()
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade); // If subject is deleted, delete its sessions

                // Date Configuration
                entity.Property(e => e.Date)
                    .IsRequired();

                // Value Object Mapping: Duration
                // We map the 'Duration' Value Object to a single column 'DurationTicks' (long)
                // This avoids creating a separate table for Duration and keeps queries simple.
                entity.OwnsOne(e => e.Duration, duration =>
                {
                    duration.Property(d => d.Value)
                            .HasColumnName("DurationTicks") // Map to specific column name
                            .HasConversion<long>();         // Convert TimeSpan.Ticks to long
                });
            });

            // --- Configure AppSettings Entity ---
            modelBuilder.Entity<AppSettings>(entity =>
            {
                // Primary Key
                entity.HasKey(s => s.Id);

                // Ensure only one record exists by seeding or logic (handled in Repository)
                // No additional constraints needed here as Id is fixed to 1.
            });
        }
    }
}

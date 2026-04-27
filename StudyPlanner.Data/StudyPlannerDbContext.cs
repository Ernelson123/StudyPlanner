using Microsoft.EntityFrameworkCore;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.ValueObjects;

namespace StudyPlanner.Data;

public class StudyPlannerDbContext : DbContext
{
    public StudyPlannerDbContext(DbContextOptions<StudyPlannerDbContext> options) : base(options) { }

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();

#pragma warning disable CA1062 // modelBuilder is guaranteed non-null by EF Core
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Subject
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            // EF Core ignores read-only properties like Id by default if they are auto-init,
            // but we need to ensure the private setter is respected if we use one.
            // Since we use private set, EF handles it well via reflection.
        });

        // Configure StudySession
        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();

            // Store Duration as Ticks (long) in the database to avoid complex type issues
            entity.OwnsOne(e => e.Duration, duration =>
            {
                duration.Property(d => d.Value)
                        .HasColumnName("DurationTicks")
                        .HasConversion<long>();
            });
        });
    }
}

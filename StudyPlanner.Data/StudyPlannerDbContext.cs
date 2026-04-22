using Microsoft.EntityFrameworkCore;

namespace StudyPlanner.Data;

public class StudyPlannerDbContext(DbContextOptions<StudyPlannerDbContext> options) : DbContext(options)
{
    // DbSet<T> will be added in Phase 4
}

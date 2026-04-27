using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudyPlanner.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StudyPlannerDbContext>
{
    public StudyPlannerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StudyPlannerDbContext>();
        optionsBuilder.UseSqlite("Data Source=studyplanner.db");

        return new StudyPlannerDbContext(optionsBuilder.Options);
    }
}

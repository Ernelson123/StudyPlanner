using Microsoft.EntityFrameworkCore;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Core.Entities;

namespace StudyPlanner.Data.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly StudyPlannerDbContext _context;

    public SubjectRepository(StudyPlannerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Subject subject, CancellationToken ct = default)
    {
        await _context.Subjects.AddAsync(subject, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Subjects.AsNoTracking().ToListAsync(ct);
    }

    public async Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Subjects.AsNoTracking()
                                     .FirstOrDefaultAsync(s => s.Id == id, ct);
    }
}

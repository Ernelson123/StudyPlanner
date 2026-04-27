using StudyPlanner.Core.Entities;

namespace StudyPlanner.Application.Repositories;

public interface ISubjectRepository
{
    Task AddAsync(Subject subject, CancellationToken ct = default);
    Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct = default);
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

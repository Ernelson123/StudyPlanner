using Microsoft.EntityFrameworkCore;
using StudyPlanner.Core.Entities;
using StudyPlanner.Data;
using StudyPlanner.Data.Repositories;

namespace StudyPlanner.Tests.Data;

/// <summary>
/// Unit tests for SubjectRepository using EF Core In-Memory database.
/// </summary>
public sealed class SubjectRepositoryTests : IDisposable
{
    private readonly StudyPlannerDbContext _context;
    private readonly SubjectRepository _repository;

    public SubjectRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<StudyPlannerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StudyPlannerDbContext(options);
        _repository = new SubjectRepository(_context);
    }

    /// <summary>
    /// Dispose method following the proper Dispose pattern.
    /// Cleans up DbContext after each test to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        // Clean up managed resources (DbContext)
        _context?.Dispose();

        // Suppress finalization - tells GC we already cleaned up
        // This prevents derived types from needing to re-implement IDisposable
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddAsync_AddsSubjectToDatabase()
    {
        var subject = Subject.Create("Physics", 2);
        await _repository.AddAsync(subject);
        var allSubjects = await _repository.GetAllAsync();

        Assert.Single(allSubjects);
        Assert.Equal("Physics", allSubjects[0].Name);
        Assert.Equal(2, allSubjects[0].PriorityWeight);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoSubjects()
    {
        var subjects = await _repository.GetAllAsync();
        Assert.Empty(subjects);
    }
}

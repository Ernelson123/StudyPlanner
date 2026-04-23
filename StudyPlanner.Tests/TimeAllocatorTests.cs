using FluentAssertions;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;
using Xunit;

namespace StudyPlanner.Tests;

public class TimeAllocatorTests
{
    [Fact]
    public void AllocateTime_WithEqualWeights_ShouldSplitEqually()
    {
        // Arrange
        var allocator = new TimeAllocator();
        var subjectA = Subject.Create("Math", 1);
        var subjectB = Subject.Create("Physics", 1);
        var totalTime = TimeSpan.FromHours(2); // 7200 seconds

        // Act
        var result = allocator.AllocateTime(new[] { subjectA, subjectB }, totalTime);

        // Assert
        result.Should().ContainKey(subjectA.Id);
        result.Should().ContainKey(subjectB.Id);
        result[subjectA.Id].Should().Be(TimeSpan.FromHours(1));
        result[subjectB.Id].Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void AllocateTime_WithDifferentWeights_ShouldBeProportional()
    {
        // Arrange
        var allocator = new TimeAllocator();
        var subjectA = Subject.Create("Math", 3);
        var subjectB = Subject.Create("Physics", 1);
        var totalTime = TimeSpan.FromHours(4);

        // Act
        var result = allocator.AllocateTime(new[] { subjectA, subjectB }, totalTime);

        // Assert
        // Math (Weight 3) should get 3h, Physics (Weight 1) should get 1h
        result[subjectA.Id].Should().Be(TimeSpan.FromHours(3));
        result[subjectB.Id].Should().Be(TimeSpan.FromHours(1));
    }
}

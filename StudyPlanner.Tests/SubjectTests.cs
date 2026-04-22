using FluentAssertions;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.Exceptions;

namespace StudyPlanner.Tests;

public class SubjectTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSubject()
    {
        // Act
        var subject = Subject.Create("C# Advanced", 5);

        // Assert
        subject.Should().NotBeNull();
        subject.Name.Should().Be("C# Advanced");
        subject.PriorityWeight.Should().Be(5);
        subject.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        // Act
        var act = () => Subject.Create("");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Subject name cannot be empty.");
    }

    [Fact]
    public void Create_WithZeroPriority_ShouldThrowDomainException()
    {
        // Act
        var act = () => Subject.Create("Math", 0);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Priority weight must be positive.");
    }
}

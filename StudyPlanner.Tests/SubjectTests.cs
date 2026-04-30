using StudyPlanner.Core.Entities;
using StudyPlanner.Core.Exceptions;
using Xunit;

namespace StudyPlanner.Tests.Domain;

/// <summary>
/// Unit tests for the Subject entity.
/// These tests verify that the domain logic works correctly:
/// - Valid data creates subjects successfully
/// - Invalid data throws appropriate exceptions
/// This ensures our business rules are enforced.
/// </summary>
public class SubjectTests
{
    /// <summary>
    /// Test: Creating a subject with valid name and priority should succeed.
    /// This is the "happy path" - when everything goes as expected.
    /// </summary>
    [Fact] // Fact attribute marks this as a test method
    public void Create_ValidNameAndPriority_ReturnsSubject()
    {
        // ========== ARRANGE ==========
        // Setup test data - the inputs we'll use
        var name = "Mathematics";
        var priority = 3;

        // ========== ACT ==========
        // Execute the code we're testing
        // Subject.Create() is the factory method that validates and creates subjects
        var subject = Subject.Create(name, priority);

        // ========== ASSERT ==========
        // Verify the results match our expectations

        // Subject should not be null (creation succeeded)
        Assert.NotNull(subject);

        // Name should match what we passed in
        Assert.Equal(name, subject.Name);

        // Priority should match what we passed in
        Assert.Equal(priority, subject.PriorityWeight);

        // ID should be auto-generated (not empty Guid)
        Assert.NotEqual(Guid.Empty, subject.Id);
    }

    /// <summary>
    /// Test: Creating a subject with empty/null name should fail.
    /// This tests our validation logic - we don't want subjects without names.
    /// </summary>
    /// <param name="name">The invalid name to test (empty, whitespace, or null)</param>
    [Theory] // Theory allows parameterized tests (multiple test cases)
    [InlineData("")]      // Empty string
    [InlineData("   ")]   // Whitespace only
    [InlineData(null)]    // Null value
    public void Create_EmptyName_ThrowsDomainException(string? name)
    {
        // ========== ARRANGE, ACT & ASSERT ==========
        // We combine all three because we're testing that an exception is thrown

        // Assert.Throws verifies that the code throws the expected exception
        // If no exception is thrown, the test fails
        // If wrong exception type is thrown, the test fails
        Assert.Throws<DomainException>(() => Subject.Create(name!, 1));

        // The "!" after name tells compiler "I know this might be null, that's intentional"
    }

    /// <summary>
    /// Test: Creating a subject with invalid priority (0 or negative) should fail.
    /// Priority must be positive (1-10) to make sense in our domain.
    /// </summary>
    /// <param name="priority">The invalid priority to test</param>
    [Theory]
    [InlineData(0)]   // Zero is not valid
    [InlineData(-1)]  // Negative is not valid
    public void Create_InvalidPriority_ThrowsDomainException(int priority)
    {
        // ========== ARRANGE, ACT & ASSERT ==========
        // Verify that invalid priority throws DomainException
        Assert.Throws<DomainException>(() => Subject.Create("Math", priority));

        // We use "Math" as name because it's valid - we're only testing priority validation
    }
}

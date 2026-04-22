using StudyPlanner.Core.Exceptions;

namespace StudyPlanner.Core.Entities;

public class Subject
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int PriorityWeight { get; private set; }

    // Private constructor forces usage of Create method
    private Subject(Guid id, string name, int priorityWeight)
    {
        Id = id;
        Name = name;
        PriorityWeight = priorityWeight;
    }

    public static Subject Create(string name, int priorityWeight = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Subject name cannot be empty.");

        if (priorityWeight <= 0)
            throw new DomainException("Priority weight must be positive.");

        return new Subject(Guid.NewGuid(), name.Trim(), priorityWeight);
    }
}

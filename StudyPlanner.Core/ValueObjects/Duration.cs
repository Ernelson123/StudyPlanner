namespace StudyPlanner.Core.ValueObjects;

public record Duration(TimeSpan Value)
{
    public static Duration Zero => new(TimeSpan.Zero);

    public static Duration Create(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            throw new ArgumentException("Duration cannot be negative.", nameof(value));

        return new Duration(value);
    }

    public Duration Add(Duration other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Create(this.Value + other.Value);
    }
}

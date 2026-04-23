namespace StudyPlanner.Core.Events;

public class TimerTickEventArgs(TimeSpan remaining) : EventArgs
{
    public TimeSpan RemainingTime { get; } = remaining;
}

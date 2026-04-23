using StudyPlanner.Core.Enums;

namespace StudyPlanner.Core.Events;

public class StateChangedEventArgs(PomodoroState state) : EventArgs
{
    public PomodoroState NewState { get; } = state;
}

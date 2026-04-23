using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;

namespace StudyPlanner.Application.Services;

public interface ITimerService
{
    event EventHandler<TimerTickEventArgs>? Tick;
    event EventHandler<StateChangedEventArgs>? StateChanged;

    PomodoroState CurrentState { get; }
    TimeSpan RemainingTime { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}

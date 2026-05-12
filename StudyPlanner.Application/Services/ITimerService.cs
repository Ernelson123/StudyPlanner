using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;

namespace StudyPlanner.Application.Services
{

    public interface ITimerService
    {
        event EventHandler<TimerTickEventArgs>? Tick;
        event EventHandler<StateChangedEventArgs>? StateChanged;
        event EventHandler<WorkPhaseCompletedEventArgs>? WorkPhaseCompleted;

        PomodoroState CurrentState { get; }
        TimeSpan RemainingTime { get; }

        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync();

        /// <summary>
        /// Atualiza as durações do timer em runtime.
        /// Usado quando o usuário altera as configurações.
        /// </summary>
        void UpdateDurations(TimeSpan work, TimeSpan shortBreak, TimeSpan longBreak);
    }
}

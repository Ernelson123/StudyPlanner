using Microsoft.Extensions.Logging;
using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;

namespace StudyPlanner.Application.Services;

public sealed class PomodoroTimerService : ITimerService, IDisposable
{
    private readonly ILogger<PomodoroTimerService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    private readonly TimeSpan _workDuration = TimeSpan.FromMinutes(25);
    private readonly TimeSpan _shortBreak = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _longBreak = TimeSpan.FromMinutes(15);

    private int _completedPomodoros;
    private int _secondsElapsed;
    private CancellationTokenSource? _cts;
    private TimeSpan _currentTargetDuration;

    public event EventHandler<TimerTickEventArgs>? Tick;
    public event EventHandler<StateChangedEventArgs>? StateChanged;
    public event EventHandler<WorkPhaseCompletedEventArgs>? WorkPhaseCompleted;

    public PomodoroState CurrentState { get; private set; } = PomodoroState.Idle;

    public TimeSpan RemainingTime => _currentTargetDuration - TimeSpan.FromSeconds(_secondsElapsed);

    public PomodoroTimerService(ILogger<PomodoroTimerService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState != PomodoroState.Idle)
            throw new InvalidOperationException("Timer is already running.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("Starting Pomodoro cycle");
        await SetStateAsync(PomodoroState.Work);

        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                _secondsElapsed++;
                Tick?.Invoke(this, new TimerTickEventArgs(RemainingTime));

                if (_secondsElapsed >= _currentTargetDuration.TotalSeconds)
                {
                    await HandlePhaseCompletion();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Stop
        }
        finally
        {
            CurrentState = PomodoroState.Idle;
            StateChanged?.Invoke(this, new StateChangedEventArgs(PomodoroState.Idle));
        }
    }

    public async Task StopAsync()
    {
        if (_cts != null) await _cts.CancelAsync();
    }

    private async Task HandlePhaseCompletion()
    {
        _logger.LogInformation("Phase completed: {State}", CurrentState);

        if (CurrentState == PomodoroState.Work)
        {
            // RAISE EVENT: Notify subscribers that a work phase just finished
            // This triggers automatic session recording in the ViewModel
            // The event passes the work duration (25 minutes) so we know how long was studied
            WorkPhaseCompleted?.Invoke(this, new WorkPhaseCompletedEventArgs(_workDuration));

            _completedPomodoros++;
            // After 4 pomodoros, take a long break (15 min)
            if (_completedPomodoros % 4 == 0)
                await SetStateAsync(PomodoroState.LongBreak);
            else
                await SetStateAsync(PomodoroState.ShortBreak);
        }
        else
        {
            // Break is over, start a new work session
            await SetStateAsync(PomodoroState.Work);
        }

        _secondsElapsed = 0;
    }

    private async Task SetStateAsync(PomodoroState newState)
    {
        CurrentState = newState;
        _currentTargetDuration = newState switch
        {
            PomodoroState.Work => _workDuration,
            PomodoroState.ShortBreak => _shortBreak,
            PomodoroState.LongBreak => _longBreak,
            _ => TimeSpan.Zero
        };
        StateChanged?.Invoke(this, new StateChangedEventArgs(newState));
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _timer.Dispose();
    }
}

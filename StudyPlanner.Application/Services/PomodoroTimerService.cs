using Microsoft.Extensions.Logging;
using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;

namespace StudyPlanner.Application.Services
{
    /// <summary>
    /// Core service managing the Pomodoro timer lifecycle.
    /// Handles state transitions (Work -> Break -> Work), timing ticks, and phase completion events.
    /// Runs on a background thread using PeriodicTimer for accurate 1-second intervals.
    /// </summary>
    public sealed class PomodoroTimerService : ITimerService, IDisposable
    {
        private readonly ILogger<PomodoroTimerService> _logger;

        // Timer for 1-second ticks. Using PeriodicTimer for high-precision background timing.
        private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

        // Mutable durations to support dynamic settings (v1.2)
        private TimeSpan _workDuration = TimeSpan.FromMinutes(25);
        private TimeSpan _shortBreakDuration = TimeSpan.FromMinutes(5);
        private TimeSpan _longBreakDuration = TimeSpan.FromMinutes(15);

        private int _completedPomodoros;
        private int _secondsElapsed;
        private CancellationTokenSource? _cts;
        private TimeSpan _currentTargetDuration;

        // Events for UI updates and session recording
        public event EventHandler<TimerTickEventArgs>? Tick;
        public event EventHandler<StateChangedEventArgs>? StateChanged;
        public event EventHandler<WorkPhaseCompletedEventArgs>? WorkPhaseCompleted;

        public PomodoroState CurrentState { get; private set; } = PomodoroState.Idle;

        /// <summary>
        /// Calculates remaining time based on current target duration and elapsed seconds.
        /// </summary>
        public TimeSpan RemainingTime => _currentTargetDuration - TimeSpan.FromSeconds(_secondsElapsed);

        public PomodoroTimerService(ILogger<PomodoroTimerService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Starts the Pomodoro cycle from the Idle state.
        /// Throws InvalidOperationException if already running.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentState != PomodoroState.Idle)
                throw new InvalidOperationException("Timer is already running.");

            // Link external cancellation token with internal CTS for cooperative cancellation
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _logger.LogInformation("Starting Pomodoro cycle");

            // Transition to Work state immediately
            SetState(PomodoroState.Work);

            try
            {
                // Main timer loop: waits for 1-second ticks
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    _secondsElapsed++;

                    // Notify UI of progress
                    Tick?.Invoke(this, new TimerTickEventArgs(RemainingTime));

                    // Check if current phase is complete
                    if (_secondsElapsed >= _currentTargetDuration.TotalSeconds)
                    {
                        await HandlePhaseCompletion();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected behavior when StopAsync is called or token is cancelled
                _logger.LogInformation("Timer stopped or cancelled.");
            }
            finally
            {
                // Ensure we return to Idle state even if an error occurs
                CurrentState = PomodoroState.Idle;
                StateChanged?.Invoke(this, new StateChangedEventArgs(PomodoroState.Idle));
            }
        }

        /// <summary>
        /// Stops the timer gracefully by cancelling the ongoing task.
        /// </summary>
        public async Task StopAsync()
        {
            if (_cts != null)
            {
                await _cts.CancelAsync();
            }
        }

        /// <summary>
        /// Updates the durations for Work, Short Break, and Long Break.
        /// Called when user saves new settings.
        /// If the timer is running, it will continue with the old duration until the next phase.
        /// </summary>
        public void UpdateDurations(TimeSpan work, TimeSpan shortBreak, TimeSpan longBreak)
        {
            _workDuration = work;
            _shortBreakDuration = shortBreak;
            _longBreakDuration = longBreak;

            // If currently idle, update the target duration immediately so UI reflects changes
            if (CurrentState == PomodoroState.Idle)
            {
                _currentTargetDuration = _workDuration;
                Tick?.Invoke(this, new TimerTickEventArgs(RemainingTime));
            }
        }

        /// <summary>
        /// Handles logic when a phase (Work or Break) completes.
        /// Transitions to the next appropriate state.
        /// </summary>
        private async Task HandlePhaseCompletion()
        {
            _logger.LogInformation("Phase completed: {State}", CurrentState);

            if (CurrentState == PomodoroState.Work)
            {
                // Raise event to record study session
                WorkPhaseCompleted?.Invoke(this, new WorkPhaseCompletedEventArgs(_workDuration));

                _completedPomodoros++;

                // Determine next break type: Long break after every 4 pomodoros
                if (_completedPomodoros % 4 == 0)
                    SetState(PomodoroState.LongBreak);
                else
                    SetState(PomodoroState.ShortBreak);
            }
            else
            {
                // Break is over, start a new work session
                SetState(PomodoroState.Work);
            }

            _secondsElapsed = 0;
        }

        /// <summary>
        /// Synchronously updates the current state and target duration.
        /// Raises StateChanged event for UI updates.
        /// </summary>
        private void SetState(PomodoroState newState)
        {
            CurrentState = newState;

            // Set target duration based on new state
            _currentTargetDuration = newState switch
            {
                PomodoroState.Work => _workDuration,
                PomodoroState.ShortBreak => _shortBreakDuration,
                PomodoroState.LongBreak => _longBreakDuration,
                _ => TimeSpan.Zero
            };

            StateChanged?.Invoke(this, new StateChangedEventArgs(newState));
        }

        /// <summary>
        /// Disposes resources to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _timer.Dispose();
        }
    }
}

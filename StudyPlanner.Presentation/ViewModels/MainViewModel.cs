using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;

namespace StudyPlanner.Presentation.ViewModels
{

    /// <summary>
    /// Main ViewModel for the application window.
    /// Follows MVVM pattern: bridges UI (View) with business logic (Model/Service).
    /// Implements IDisposable to clean up timer event subscriptions.
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        // ========== DEPENDENCIES (Injected via Constructor) ==========
        private readonly ISubjectRepository _subjectRepository;  // Database access for subjects
        private readonly ITimerService _timerService;            // Pomodoro timer logic
        private readonly ILogger<MainViewModel> _logger;         // Logging for debugging/errors
        private readonly Dispatcher _dispatcher;                 // Marshals UI updates from background threads
        private bool _disposed;                                  // Prevents double-disposal of resources

        /// <summary>
        /// Constructor - Called by Dependency Injection container when window opens.
        /// All dependencies are automatically injected from App.xaml.cs ConfigureServices().
        /// </summary>
        public MainViewModel(
            ISubjectRepository subjectRepository,
            ITimerService timerService,
            ILogger<MainViewModel> logger)
        {
            // Store dependencies for later use
            _subjectRepository = subjectRepository;
            _timerService = timerService;
            _logger = logger;

            // Get UI thread dispatcher for thread-safe updates
            // Fallback if Application.Current is null (e.g., designer mode)
            _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.FromThread(System.Threading.Thread.CurrentThread);

            // ========== INITIALIZE COMMANDS ==========
            // Commands are like "methods" that buttons can bind to in XAML
            // AsyncRelayCommand handles async/await and CanExecute automatically
            LoadSubjectsCommand = new AsyncRelayCommand(LoadSubjectsAsync);
            AddSubjectCommand = new AsyncRelayCommand(AddSubjectAsync, CanAddSubject);
            StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, () => !IsTimerRunning);
            StopTimerCommand = new AsyncRelayCommand(StopTimerAsync, () => IsTimerRunning);

            // ========== SUBSCRIBE TO TIMER EVENTS ==========
            // Timer runs on background thread, raises events when it ticks or changes state
            // We handle these events to update UI properties (which trigger data binding)
            _timerService.Tick += OnTimerTick;
            _timerService.StateChanged += OnStateChanged;

            // ========== INITIALIZE UI STATE ==========
            // Set initial timer display (e.g., "25:00")
            UpdateTimerDisplay(_timerService.RemainingTime);
            // Set initial state text (e.g., "Idle", "Work", "ShortBreak")
            CurrentState = _timerService.CurrentState.ToString();

            // ========== LOAD DATA ON STARTUP ==========
            // Fire-and-forget: Load subjects from database when ViewModel is created
            // The underscore (_) prefix means "we're intentionally ignoring the Task result"
            _ = SafeLoadSubjectsAsync();
        }

        // ========== SUBJECTS SECTION (Data Management) ==========
        #region Subjects

        // ObservableCollection automatically notifies UI when items are added/removed
        // This makes the ListBox update in real-time without manual refresh
        [ObservableProperty] private ObservableCollection<Subject> _subjects = new();

        // Bound to TextBox in XAML - user types subject name here
        [ObservableProperty] private string _newSubjectName = string.Empty;

        // Bound to TextBox in XAML - user enters priority (1-10)
        [ObservableProperty] private int _newPriority = 1;

        // Public commands that XAML buttons bind to
        public IAsyncRelayCommand LoadSubjectsCommand { get; }
        public IAsyncRelayCommand AddSubjectCommand { get; }

        /// <summary>
        /// Loads all subjects from database and populates the ObservableCollection.
        /// Called on startup and after adding a new subject.
        /// </summary>
        private async Task LoadSubjectsAsync()
        {
            // Fetch all subjects from SQLite database (async = non-blocking)
            var list = await _subjectRepository.GetAllAsync();

            // Clear current list and add fresh data
            // ObservableCollection notifies UI automatically when we modify it
            Subjects.Clear();
            foreach (var s in list) Subjects.Add(s);
        }

        /// <summary>
        /// Wrapper for LoadSubjectsAsync that catches errors.
        /// Used for fire-and-forget startup loading (don't crash app if DB fails).
        /// </summary>
        private async Task SafeLoadSubjectsAsync()
        {
            try { await LoadSubjectsAsync(); }
#pragma warning disable CA1031 // Generic catch acceptable for fire-and-forget startup
            catch (Exception ex) { _logger.LogError(ex, "Failed to load subjects on startup"); }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Creates a new subject, saves to database, then refreshes the list.
        /// Called when user clicks "Add Subject" button.
        /// </summary>
        private async Task AddSubjectAsync()
        {
            try
            {
                // Domain validation: Subject.Create() throws if name is empty or priority <= 0
                var subject = Subject.Create(NewSubjectName, NewPriority);

                // Save to SQLite database via repository pattern
                await _subjectRepository.AddAsync(subject);

                // Clear input fields for next entry
                NewSubjectName = string.Empty;  // This triggers property changed → UI updates
                NewPriority = 1;

                // Reload list to show the new subject
                await LoadSubjectsAsync();
            }
#pragma warning disable CA1031 // Generic catch acceptable in UI command handlers
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // Log error (visible in Output window during debug)
                _logger.LogError(ex, "Failed to add subject");
                // In production, you might show a MessageBox here
            }
        }

        /// <summary>
        /// Determines if "Add Subject" button should be enabled.
        /// Button is enabled only if name is not empty and priority > 0.
        /// </summary>
        private bool CanAddSubject() =>
            !string.IsNullOrWhiteSpace(NewSubjectName) && NewPriority > 0;

        /// <summary>
        /// Auto-generated by [ObservableProperty] attribute.
        /// Called whenever NewSubjectName changes (on every keystroke).
        /// Notifies the AddSubjectCommand to re-evaluate CanAddSubject().
        /// </summary>
        partial void OnNewSubjectNameChanged(string value) =>
            AddSubjectCommand.NotifyCanExecuteChanged();

        /// <summary>
        /// Same as above, but for NewPriority.
        /// </summary>
        partial void OnNewPriorityChanged(int value) =>
            AddSubjectCommand.NotifyCanExecuteChanged();
        #endregion

        // ========== TIMER SECTION (Pomodoro Functionality) ==========
        #region Timer

        // Display text for timer (e.g., "25:00", "04:32")
        [ObservableProperty] private string _timerDisplay = "25:00";

        // Current timer state (e.g., "Idle", "Work", "ShortBreak", "LongBreak")
        [ObservableProperty] private string _currentState = "Idle";

        // Controls button enable/disable state
        [ObservableProperty] private bool _isTimerRunning;

        // Public commands for Start/Stop buttons
        public IAsyncRelayCommand StartTimerCommand { get; }
        public IAsyncRelayCommand StopTimerCommand { get; }

        /// <summary>
        /// Called by ITimerService every second when timer is running.
        /// Updates the countdown display on UI thread.
        /// </summary>
        private void OnTimerTick(object? sender, TimerTickEventArgs e)
        {
            if (_disposed) return;  // Don't update if ViewModel is being destroyed

            // Marshal to UI thread (timer runs on background thread)
            // InvokeAsync schedules the update without blocking the timer
            _dispatcher.InvokeAsync(() => UpdateTimerDisplay(e.RemainingTime));
        }

        /// <summary>
        /// Called by ITimerService when state changes (Work → Break → Work, etc.).
        /// Updates state text and button enable/disable states.
        /// </summary>
        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            if (_disposed) return;

            _dispatcher.InvokeAsync(() =>
            {
                // Update state text (e.g., "Work" → "ShortBreak")
                CurrentState = e.NewState.ToString();

                // Update button states: Start enabled when Idle, Stop enabled when Running
                IsTimerRunning = e.NewState != PomodoroState.Idle;
                StartTimerCommand.NotifyCanExecuteChanged();
                StopTimerCommand.NotifyCanExecuteChanged();
            });
        }

        /// <summary>
        /// Formats TimeSpan as "mm:ss" string for display.
        /// Uses InvariantCulture to ensure consistent formatting across locales.
        /// </summary>
        private void UpdateTimerDisplay(TimeSpan remaining)
        {
            TimerDisplay = remaining.TotalSeconds <= 0
                ? "00:00"  // Timer finished
                : remaining.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Starts the Pomodoro timer via ITimerService.
        /// Catches errors to prevent app crash if timer fails.
        /// </summary>
        private async Task StartTimerAsync()
        {
            try { await _timerService.StartAsync(); }
#pragma warning disable CA1031 // Generic catch acceptable in UI command handlers
            catch (Exception ex) { _logger.LogError(ex, "Timer failed to start"); }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Stops the Pomodoro timer via ITimerService.
        /// </summary>
        private async Task StopTimerAsync()
        {
            try { await _timerService.StopAsync(); }
#pragma warning disable CA1031 // Generic catch acceptable in UI command handlers
            catch (Exception ex) { _logger.LogError(ex, "Timer failed to stop"); }
#pragma warning restore CA1031
        }
        #endregion

        // ========== CLEANUP SECTION (Memory Leak Prevention) ==========
        #region IDisposable

        /// <summary>
        /// Public Dispose method - called when window closes.
        /// Unsubscribes from timer events to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // Tells garbage collector we already cleaned up
        }

        /// <summary>
        /// Protected virtual Dispose pattern (allows inheritance if needed).
        /// disposing=true means called from user code, false means called from finalizer.
        /// </summary>
        public void Dispose(bool disposing)
        {
            if (_disposed) return;  // Prevent double-disposal

            if (disposing)
            {
                // Unsubscribe from events - CRITICAL to prevent memory leaks
                // If we don't do this, the timer keeps references to this ViewModel
                _timerService.Tick -= OnTimerTick;
                _timerService.StateChanged -= OnStateChanged;
            }

            _disposed = true;
        }
        #endregion
    }
}

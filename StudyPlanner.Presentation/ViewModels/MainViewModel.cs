using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;
using StudyPlanner.Presentation.Services;
using StudyPlanner.Presentation.Views;

namespace StudyPlanner.Presentation.ViewModels
{


    /// <summary>
    /// Main ViewModel for the Study Planner application.
    /// Acts as the central bridge between the UI (View) and the business logic (Services/Repositories).
    /// Implements MVVM pattern using CommunityToolkit.Mvvm for boilerplate reduction.
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        // ========== DEPENDENCIES (Injected via Constructor) ==========

        /// <summary>
        /// Repository for CRUD operations on Study Subjects.
        /// </summary>
        private readonly ISubjectRepository _subjectRepository;

        /// <summary>
        /// Core service managing the Pomodoro timer state, ticks, and phase transitions.
        /// </summary>
        private readonly ITimerService _timerService;

        /// <summary>
        /// Service responsible for recording and retrieving study sessions for statistics.
        /// </summary>
        private readonly IStudySessionService _studySessionService;

        /// <summary>
        /// Logger for debugging and error tracking.
        /// </summary>
        private readonly ILogger<MainViewModel> _logger;

        /// <summary>
        /// WPF Dispatcher to marshal background thread updates (from Timer) to the UI Thread.
        /// </summary>
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Service Provider used for resolving Views (Navigation) without tight coupling.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Flag to prevent event handlers from executing after the ViewModel is disposed.
        /// Prevents memory leaks and ObjectDisposedExceptions.
        /// </summary>
        private bool _disposed;

        // ========== CONSTANTS & CONFIGURATION ==========

        /// <summary>
        /// Circumference of the circular timer progress bar (2 * π * radius).
        /// Radius is effectively 90px in XAML. Used for StrokeDashOffset animation.
        /// </summary>
        private const double CircleCircumference = 565.487;

        // ========== OBSERVABLE PROPERTIES (UI State) ==========

        /// <summary>
        /// The subject currently selected by the user for studying.
        /// If null, the system falls back to the first available subject for logging.
        /// </summary>
        [ObservableProperty] private Subject? _selectedSubject;

        /// <summary>
        /// Tracks whether Dark Mode is active. Bound to Theme Toggle Button.
        /// </summary>
        [ObservableProperty] private bool _isDarkTheme;

        /// <summary>
        /// Controls the visual fill of the circular timer.
        /// Higher value = less visible stroke (countdown effect).
        /// </summary>
        [ObservableProperty] private double _timerDashOffset;

        /// <summary>
        /// Text display for the timer (e.g., "25:00").
        /// </summary>
        [ObservableProperty] private string _timerDisplay = "25:00";

        /// <summary>
        /// Current phase of the Pomodoro cycle (Idle, Work, ShortBreak, LongBreak).
        /// </summary>
        [ObservableProperty] private string _currentState = "Idle";

        /// <summary>
        /// Indicates if the timer is currently running. Used to enable/disable Start/Stop buttons.
        /// </summary>
        [ObservableProperty] private bool _isTimerRunning;

        /// <summary>
        /// Collection of all subjects, bound to the ListBox and ComboBox.
        /// ObservableCollection ensures UI updates automatically when items are added/removed.
        /// </summary>
        [ObservableProperty] private ObservableCollection<Subject> _subjects = new();

        /// <summary>
        /// Input field for creating a new subject name.
        /// </summary>
        [ObservableProperty] private string _newSubjectName = string.Empty;

        /// <summary>
        /// Input field for setting priority (1-10) for a new subject.
        /// </summary>
        [ObservableProperty] private int _newPriority = 1;

        /// <summary>
        /// Data series for the Weekly Study Hours Chart (LiveCharts2).
        /// </summary>
        [ObservableProperty] private ISeries[] _weeklyStudySeries = Array.Empty<ISeries>();

        /// <summary>
        /// Total hours studied in the current week, calculated from WeeklyStudySeries.
        /// Bound to the UI for display.
        /// </summary>
        [ObservableProperty] private double _totalWeeklyHours = 0;

        // ========== COMMANDS ==========

        public IAsyncRelayCommand LoadSubjectsCommand { get; }
        public IAsyncRelayCommand AddSubjectCommand { get; }
        public IAsyncRelayCommand StartTimerCommand { get; }
        public IAsyncRelayCommand StopTimerCommand { get; }
        public IAsyncRelayCommand GenerateReportCommand { get; }

        /// <summary>
        /// Toggles between Light and Dark themes dynamically.
        /// </summary>
        public ICommand ToggleThemeCommand { get; }

        /// <summary>
        /// Opens the Settings window modally.
        /// </summary>
        public ICommand OpenSettingsCommand { get; }

        // ========== CONSTRUCTOR ==========

        public MainViewModel(
            ISubjectRepository subjectRepository,
            ITimerService timerService,
            IStudySessionService studySessionService,
            IServiceProvider serviceProvider,
            ILogger<MainViewModel> logger)
        {
            // Assign dependencies
            _subjectRepository = subjectRepository;
            _timerService = timerService;
            _studySessionService = studySessionService;
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Capture the UI Dispatcher for thread-safe updates from background threads
            _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.FromThread(System.Threading.Thread.CurrentThread);

            // Initialize Commands
            LoadSubjectsCommand = new AsyncRelayCommand(LoadSubjectsAsync);
            AddSubjectCommand = new AsyncRelayCommand(AddSubjectAsync, CanAddSubject);
            StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, () => !IsTimerRunning);
            StopTimerCommand = new AsyncRelayCommand(StopTimerAsync, () => IsTimerRunning);
            GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);

            // V1.1: Theme Toggle Command
            ToggleThemeCommand = new RelayCommand(() =>
            {
                IsDarkTheme = !IsDarkTheme;
                ThemeManager.ApplyTheme(IsDarkTheme);
            });

            // V1.2: Settings Navigation Command
            OpenSettingsCommand = new RelayCommand(OpenSettings);

            // Subscribe to Timer Events (Background Thread -> UI Thread marshaling handled inside handlers)
            _timerService.Tick += OnTimerTick;
            _timerService.StateChanged += OnStateChanged;
            _timerService.WorkPhaseCompleted += OnWorkPhaseCompleted;

            // Initialize UI State based on current Timer Service state
            UpdateTimerDisplay(_timerService.RemainingTime);
            CurrentState = _timerService.CurrentState.ToString();

            // Load initial data asynchronously (Fire-and-forget with error handling)
            _ = SafeLoadSubjectsAsync();
            _ = SafeLoadWeeklyChartDataAsync();
        }

        // ========== SUBJECT MANAGEMENT ==========
        #region Subjects

        /// <summary>
        /// Fetches all subjects from the database and populates the ObservableCollection.
        /// </summary>
        private async Task LoadSubjectsAsync()
        {
            var list = await _subjectRepository.GetAllAsync();
            Subjects.Clear();
            foreach (var s in list) Subjects.Add(s);
        }

        /// <summary>
        /// Safe wrapper for LoadSubjectsAsync to prevent crashes during startup if DB is unavailable.
        /// </summary>
        private async Task SafeLoadSubjectsAsync()
        {
            try { await LoadSubjectsAsync(); }
#pragma warning disable CA1031 // Generic catch acceptable for fire-and-forget startup tasks
            catch (Exception ex) { _logger.LogError(ex, "Failed to load subjects on startup"); }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Creates a new subject, saves it to the database, and refreshes the list.
        /// </summary>
        private async Task AddSubjectAsync()
        {
            try
            {
                // Domain validation occurs inside Subject.Create
                var subject = Subject.Create(NewSubjectName, NewPriority);
                await _subjectRepository.AddAsync(subject);

                // Reset input fields
                NewSubjectName = string.Empty;
                NewPriority = 1;

                // Refresh UI
                await LoadSubjectsAsync();
            }
#pragma warning disable CA1031 // Generic catch acceptable for UI command handlers
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Failed to add subject");
            }
        }

        /// <summary>
        /// Determines if the "Add Subject" button should be enabled.
        /// </summary>
        private bool CanAddSubject() =>
            !string.IsNullOrWhiteSpace(NewSubjectName) && NewPriority > 0;

        // Auto-generated partial methods by CommunityToolkit.Mvvm to notify command state changes
        partial void OnNewSubjectNameChanged(string value) =>
            AddSubjectCommand.NotifyCanExecuteChanged();

        partial void OnNewPriorityChanged(int value) =>
            AddSubjectCommand.NotifyCanExecuteChanged();
        #endregion

        // ========== TIMER LOGIC ==========
        #region Timer

        /// <summary>
        /// Handles timer tick events from the background service.
        /// Marshals the update to the UI Thread to update the display and circular progress.
        /// </summary>
        private void OnTimerTick(object? sender, TimerTickEventArgs e)
        {
            if (_disposed) return;
            _dispatcher.InvokeAsync(() => UpdateTimerDisplay(e.RemainingTime));
        }

        /// <summary>
        /// Handles state changes (Work -> Break -> Idle).
        /// Updates UI text and button enable/disable states.
        /// </summary>
        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            if (_disposed) return;

            _dispatcher.InvokeAsync(() =>
            {
                CurrentState = e.NewState.ToString();
                IsTimerRunning = e.NewState != PomodoroState.Idle;

                // Notify buttons to re-evaluate their CanExecute predicates
                StartTimerCommand.NotifyCanExecuteChanged();
                StopTimerCommand.NotifyCanExecuteChanged();
            });
        }

        /// <summary>
        /// Triggered when a Work phase completes.
        /// Automatically records a study session for the selected subject.
        /// </summary>
        private void OnWorkPhaseCompleted(object? sender, WorkPhaseCompletedEventArgs e)
        {
            if (_disposed) return;

            // Fallback to first subject if none is explicitly selected
            var subjectToRecord = SelectedSubject ?? Subjects.FirstOrDefault();

            if (subjectToRecord != null)
            {
                // Record session in background (fire-and-forget)
                _ = _studySessionService.RecordSessionAsync(
                    subjectToRecord.Id,
                    DateTime.UtcNow,
                    e.Duration);

                // Refresh statistics chart
                _ = SafeLoadWeeklyChartDataAsync();
            }
        }

        /// <summary>
        /// Updates the textual timer display and the circular progress bar offset.
        /// </summary>
        private void UpdateTimerDisplay(TimeSpan remaining)
        {
            // Determine total duration for the current phase to calculate percentage.
            // Note: In a fully dynamic settings scenario, you might fetch this from ITimerService 
            // if it exposes CurrentTotalDuration. Here we use defaults or infer from state.
            var totalSeconds = _timerService.CurrentState == PomodoroState.Work
                ? TimeSpan.FromMinutes(25).TotalSeconds
                : TimeSpan.FromMinutes(5).TotalSeconds;

            var elapsed = totalSeconds - remaining.TotalSeconds;
            var percent = Math.Clamp(elapsed / totalSeconds, 0, 1);

            // Update Circular Progress
            TimerDashOffset = CircleCircumference * (1 - percent);

            // Update Text Display
            TimerDisplay = remaining.TotalSeconds <= 0
                ? "00:00"
                : remaining.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }

        private async Task StartTimerAsync()
        {
            try { await _timerService.StartAsync(); }
#pragma warning disable CA1031
            catch (Exception ex) { _logger.LogError(ex, "Timer failed to start"); }
#pragma warning restore CA1031
        }

        private async Task StopTimerAsync()
        {
            try { await _timerService.StopAsync(); }
#pragma warning disable CA1031
            catch (Exception ex) { _logger.LogError(ex, "Timer failed to stop"); }
#pragma warning restore CA1031
        }
        #endregion

        // ========== STATISTICS & CHARTS ==========
        #region Charts

        private async Task SafeLoadWeeklyChartDataAsync()
        {
            try { await LoadWeeklyChartDataAsync(); }
#pragma warning disable CA1031
            catch (Exception ex) { _logger.LogError(ex, "Failed to load weekly chart data"); }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Aggregates study sessions by day of the week for the LiveCharts2 visualization.
        /// </summary>
        private async Task LoadWeeklyChartDataAsync()
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek); // Monday
            var weekEnd = weekStart.AddDays(6); // Sunday

            var sessions = await _studySessionService.GetWeeklySessionsAsync(weekStart, weekEnd);

            var dailyHours = sessions
                .GroupBy(s => s.Date.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Duration.Value.TotalHours)
                );

            var values = new double[7];
            for (int i = 0; i < 7; i++)
            {
                // Map array index (0=Mon) to DayOfWeek enum (1=Mon, ..., 0=Sun)
                var day = (DayOfWeek)(i == 0 ? 1 : i);
                if (dailyHours.TryGetValue(day, out var hours))
                {
                    values[i] = hours;
                }
            }


            var totalHours = values.Sum();
            WeeklyStudySeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Name = "Hours Studied",
                    MiniatureStrokeThickness = 0
                }
            };
            TotalWeeklyHours = totalHours;
        }
        #endregion

        // ========== REPORTING ==========
        #region Reports

        private async Task GenerateReportAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var weekEnd = weekStart.AddDays(6);

                var sessions = await _studySessionService.GetWeeklySessionsAsync(weekStart, weekEnd);
                var pdfPath = $"StudyReport_{weekStart:yyyy-MM-dd}.pdf";

                // Static call since ReportService is static
                ReportService.GenerateWeeklyReport(sessions, pdfPath);

                _logger.LogInformation($"Report generated at {pdfPath}");
            }
#pragma warning disable CA1031
            catch (Exception ex) { _logger.LogError(ex, "Failed to generate report"); }
#pragma warning restore CA1031
        }
        #endregion

        // ========== NAVIGATION (V1.2) ==========
        #region Settings

        /// <summary>
        /// Resolves and opens the Settings Window modally.
        /// Uses IServiceProvider to avoid direct dependency on the View class.
        /// </summary>
        private void OpenSettings()
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();

            // Set owner to center the dialog over the main window
            settingsWindow.Owner = System.Windows.Application.Current.MainWindow;

            // Show as modal dialog (blocks interaction with MainWindow until closed)
            settingsWindow.ShowDialog();
        }
        #endregion

        // ========== CLEANUP ==========
        #region IDisposable

        /// <summary>
        /// Disposes resources and unsubscribes from events to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _timerService.Tick -= OnTimerTick;
            _timerService.StateChanged -= OnStateChanged;
            _timerService.WorkPhaseCompleted -= OnWorkPhaseCompleted;

            _disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;
using StudyPlanner.Core.Enums;
using StudyPlanner.Core.Events;
using StudyPlanner.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;


namespace StudyPlanner.Presentation.ViewModels
{

    /// <summary>
    /// Main ViewModel for the Study Planner application.
    /// Acts as the bridge between the UI (View) and business logic (Services/Repositories).
    /// Follows MVVM pattern with data binding and commands.
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        // ========== DEPENDENCIES (Injected via Constructor) ==========

        /// <summary>
        /// Repository for accessing Subject data from SQLite database.
        /// Provides CRUD operations for study subjects.
        /// </summary>
        private readonly ISubjectRepository _subjectRepository;

        /// <summary>
        /// Service that manages the Pomodoro timer (25 min work, 5 min break).
        /// Runs on a background thread and raises events for UI updates.
        /// </summary>
        private readonly ITimerService _timerService;

        /// <summary>
        /// Service for recording and retrieving study sessions.
        /// Automatically logs sessions when Pomodoro work phases complete.
        /// </summary>
        private readonly IStudySessionService _studySessionService;

        // Durações configuráveis do Pomodoro (em segundos)
        private readonly TimeSpan _workDuration = TimeSpan.FromMinutes(25);
        private readonly TimeSpan _breakDuration = TimeSpan.FromMinutes(5);
        // private readonly TimeSpan _longBreakDuration = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Logger for debugging and error tracking.
        /// Writes to console during development.
        /// </summary>
        private readonly ILogger<MainViewModel> _logger;

        /// <summary>
        /// WPF Dispatcher for marshaling background thread updates to UI thread.
        /// Required because timer events fire on a background thread, but UI can only be updated from UI thread.
        /// </summary>
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Flag to prevent event handlers from running after ViewModel is disposed.
        /// Prevents memory leaks and null reference exceptions.
        /// </summary>
        private bool _disposed;

        // ========== V1.1: TIMER CIRCULAR E TEMA ==========
        /// <summary>
        /// Circunferência do círculo do timer (2 * π * raio).
        /// Usado para animação do StrokeDashOffset no WPF.
        /// Raio efetivo = 90px, então: 2 * Math.PI * 90 ≈ 565.487
        /// </summary>
        private const double CircleCircumference = 565.487;

        /// <summary>
        /// Matéria selecionada pelo usuário para registrar a sessão.
        /// Se null, usa a primeira da lista como fallback.
        /// </summary>
        [ObservableProperty] private Subject? _selectedSubject;

        /// <summary>
        /// Controla se o tema escuro está ativo.
        /// Bindado ao ToggleButton no XAML.
        /// </summary>
        [ObservableProperty] private bool _isDarkTheme;

        /// <summary>
        /// Controla o preenchimento visual do timer circular.
        /// Quanto maior o valor, menor a barra visível (efeito de countdown).
        /// Bindado ao StrokeDashOffset do Ellipse no XAML.
        /// </summary>
        [ObservableProperty] private double _timerDashOffset;

        /// <summary>
        /// Comando para alternar entre tema claro e escuro.
        /// </summary>
        public ICommand ToggleThemeCommand { get; }


        /// <summary>
        /// Constructor - Called by Dependency Injection when MainWindow opens.
        /// All dependencies are automatically injected from App.xaml.cs ConfigureServices().
        /// </summary>
        public MainViewModel(
            ISubjectRepository subjectRepository,
            ITimerService timerService,
            IStudySessionService studySessionService,
            ILogger<MainViewModel> logger)
        {
            // Store dependencies for later use
            _subjectRepository = subjectRepository;
            _timerService = timerService;
            _studySessionService = studySessionService;
            _logger = logger;

            // Get UI thread dispatcher for thread-safe updates
            // Fallback if Application.Current is null (e.g., designer mode)
            _dispatcher = System.Windows.Application.Current?.Dispatcher ??
                          Dispatcher.FromThread(System.Threading.Thread.CurrentThread);

            // ========== INITIALIZE COMMANDS ==========
            // Commands are like "methods" that buttons can bind to in XAML
            // AsyncRelayCommand handles async/await and CanExecute automatically
            LoadSubjectsCommand = new AsyncRelayCommand(LoadSubjectsAsync);
            AddSubjectCommand = new AsyncRelayCommand(AddSubjectAsync, CanAddSubject);
            StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, () => !IsTimerRunning);
            StopTimerCommand = new AsyncRelayCommand(StopTimerAsync, () => IsTimerRunning);
            GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);

            // ========== V1.1: COMANDO DE TEMA ==========
            // RelayCommand da CommunityToolkit para ação síncrona simples
            ToggleThemeCommand = new RelayCommand(() =>
            {
                // Inverte o estado atual
                IsDarkTheme = !IsDarkTheme;

                // Aplica o tema via ThemeManager (atualiza ResourceDictionary em runtime)
                ThemeManager.ApplyTheme(IsDarkTheme);
            });

            // ========== SUBSCRIBE TO TIMER EVENTS ==========
            // Timer runs on background thread, raises events when it ticks or changes state
            // We handle these events to update UI properties (which trigger data binding)
            _timerService.Tick += OnTimerTick;
            _timerService.StateChanged += OnStateChanged;
            _timerService.WorkPhaseCompleted += OnWorkPhaseCompleted;

            // ========== INITIALIZE UI STATE ==========
            // Set initial timer display (e.g., "25:00")
            UpdateTimerDisplay(_timerService.RemainingTime);
            // Set initial state text (e.g., "Idle", "Work", "ShortBreak")
            CurrentState = _timerService.CurrentState.ToString();

            // ========== LOAD DATA ON STARTUP ==========
            // Fire-and-forget: Load subjects and chart data when ViewModel is created
            // The underscore (_) prefix means "we're intentionally ignoring the Task result"
            _ = SafeLoadSubjectsAsync();
            _ = SafeLoadWeeklyChartDataAsync();
        }

        // ========== SUBJECTS SECTION (Data Management) ==========
        #region Subjects

        /// <summary>
        /// ObservableCollection automatically notifies UI when items are added/removed.
        /// This makes the ListBox update in real-time without manual refresh.
        /// Bound to ListBox in XAML via ItemsSource="{Binding Subjects}".
        /// </summary>
        [ObservableProperty] private ObservableCollection<Subject> _subjects = new();

        /// <summary>
        /// Bound to TextBox in XAML - user types subject name here.
        /// UpdateSourceTrigger=PropertyChanged means it updates on every keystroke.
        /// </summary>
        [ObservableProperty] private string _newSubjectName = string.Empty;

        /// <summary>
        /// Bound to TextBox in XAML - user enters priority (1-10).
        /// </summary>
        [ObservableProperty] private int _newPriority = 1;

        /// <summary>
        /// Public commands that XAML buttons bind to.
        /// </summary>
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

        /// <summary>
        /// Display text for timer (e.g., "25:00", "04:32").
        /// Bound to TextBlock in XAML.
        /// </summary>
        [ObservableProperty] private string _timerDisplay = "25:00";

        /// <summary>
        /// Current timer state (e.g., "Idle", "Work", "ShortBreak", "LongBreak").
        /// Bound to TextBlock in XAML.
        /// </summary>
        [ObservableProperty] private string _currentState = "Idle";

        /// <summary>
        /// Controls button enable/disable state.
        /// When true, Start button is disabled and Stop button is enabled.
        /// </summary>
        [ObservableProperty] private bool _isTimerRunning;

        /// <summary>
        /// Public commands for Start/Stop buttons.
        /// </summary>
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
        /// Chamado quando uma sessão de trabalho Pomodoro é concluída.
        /// Registra automaticamente a sessão para a matéria selecionada.
        /// </summary>
        private void OnWorkPhaseCompleted(object? sender, WorkPhaseCompletedEventArgs e)
        {
            if (_disposed) return;

            // ========== SELECIONAR MATÉRIA PARA REGISTRAR ==========

            // Prioriza a matéria selecionada pelo usuário no ComboBox
            // Se nenhuma estiver selecionada, usa a primeira da lista como fallback
            var subjectToRecord = SelectedSubject ?? Subjects.FirstOrDefault();

            // Só registra se houver uma matéria válida
            if (subjectToRecord != null)
            {
                // Registra a sessão no banco de dados (async fire-and-forget)
                _ = _studySessionService.RecordSessionAsync(
                    subjectToRecord.Id,
                    DateTime.UtcNow,
                    e.Duration);

                // Atualiza o gráfico de estatísticas para refletir a nova sessão
                _ = SafeLoadWeeklyChartDataAsync();
            }
        }

        /// <summary>
        /// Atualiza o display do timer E o progresso visual circular.
        /// Calcula o percentual concluído e converte para StrokeDashOffset.
        /// </summary>
        private void UpdateTimerDisplay(TimeSpan remaining)
        {
            // ========== CALCULAR PROGRESSO DO CÍRCULO ==========

            // Determina a duração total baseada no estado atual (Work ou Break)
            var totalSeconds = _timerService.CurrentState == PomodoroState.Work
                ? _workDuration.TotalSeconds
                : _breakDuration.TotalSeconds;

            // Calcula segundos já decorridos
            var elapsed = totalSeconds - remaining.TotalSeconds;

            // Percentual de conclusão (0.0 a 1.0), com clamp para segurança
            var percent = Math.Clamp(elapsed / totalSeconds, 0, 1);

            // StrokeDashOffset: quanto maior, menos círculo visível
            // Ex: 100% concluído → offset = circunferência → círculo some
            TimerDashOffset = CircleCircumference * (1 - percent);

            // ========== ATUALIZAR TEXDO DO TIMER ==========
            TimerDisplay = remaining.TotalSeconds <= 0
                ? "00:00"
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

        // ========== CHART SECTION (LiveCharts2) ==========
        #region Charts

        /// <summary>
        /// LiveCharts2 data series for the weekly study hours chart.
        /// Bound to CartesianChart in XAML.
        /// When this property changes, the chart automatically redraws.
        /// </summary>
        [ObservableProperty] private ISeries[] _weeklyStudySeries = Array.Empty<ISeries>();

        /// <summary>
        /// Wrapper for LoadWeeklyChartDataAsync that catches errors.
        /// Prevents app crash if database query fails.
        /// </summary>
        private async Task SafeLoadWeeklyChartDataAsync()
        {
            try { await LoadWeeklyChartDataAsync(); }
#pragma warning disable CA1031 // Generic catch acceptable for chart loading
            catch (Exception ex) { _logger.LogError(ex, "Failed to load weekly chart data"); }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Loads study sessions from the past week and calculates total hours per day.
        /// Updates the LiveCharts2 chart with the results.
        /// </summary>
        private async Task LoadWeeklyChartDataAsync()
        {
            // Calculate the start and end of the current week (Monday to Sunday)
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);  // Monday
            var weekEnd = weekStart.AddDays(6);  // Sunday

            // Fetch all sessions from this week
            var sessions = await _studySessionService.GetWeeklySessionsAsync(weekStart, weekEnd);

            // Group sessions by day of week and sum the hours studied each day
            var dailyHours = sessions
                .GroupBy(s => s.Date.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Duration.Value.TotalHours)
                );

            // Prepare data for LiveCharts (Monday to Sunday = index 0 to 6)
            var values = new double[7];
            // Labels for X-axis (Mon, Tue, Wed, etc.)
            var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            // Fill in the hours for each day
            for (int i = 0; i < 7; i++)
            {
                // Map array index to DayOfWeek (Monday=1, Sunday=0 in .NET)
                var day = (DayOfWeek)(i == 0 ? 1 : i);

                // If we have data for this day, use it; otherwise default to 0
                if (dailyHours.TryGetValue(day, out var hours))
                {
                    values[i] = hours;
                }
            }

            // Update the chart data
            // This triggers UI update automatically via [ObservableProperty]
            WeeklyStudySeries = new ISeries[]
            {
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Hours Studied",
                MiniatureStrokeThickness = 0  // No border around bars
            }
            };
        }
        #endregion

        // ========== REPORT GENERATION ==========
        #region Reports

        /// <summary>
        /// Command for the "Generate Report" button.
        /// Creates a PDF with weekly study statistics.
        /// </summary>
        public IAsyncRelayCommand GenerateReportCommand { get; }

        /// <summary>
        /// Generates a PDF report of weekly study sessions using QuestPDF.
        /// Saves the PDF to the application's bin folder.
        /// </summary>
        private async Task GenerateReportAsync()
        {
            try
            {
                // Calculate current week range
                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var weekEnd = weekStart.AddDays(6);

                // Fetch all sessions for this week
                var sessions = await _studySessionService.GetWeeklySessionsAsync(weekStart, weekEnd);

                // Create report service and generate PDF
                var pdfPath = $"StudyReport_{weekStart:yyyy-MM-dd}.pdf";
                ReportService.GenerateWeeklyReport(sessions, pdfPath);

                _logger.LogInformation($"Report generated at {pdfPath}");
            }
#pragma warning disable CA1031 // Generic catch acceptable for report generation
            catch (Exception ex) { _logger.LogError(ex, "Failed to generate report"); }
#pragma warning restore CA1031
        }
        #endregion

        // ========== CLEANUP (Memory Leak Prevention) ==========
        #region IDisposable

        /// <summary>
        /// Public Dispose method - called when window closes.
        /// Unsubscribes from timer events to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            // Unsubscribe from ALL timer events
            // If we don't do this, the timer keeps references to this ViewModel
            // and it can't be garbage collected (memory leak)
            _timerService.Tick -= OnTimerTick;
            _timerService.StateChanged -= OnStateChanged;
            _timerService.WorkPhaseCompleted -= OnWorkPhaseCompleted;

            _disposed = true;
            GC.SuppressFinalize(this);  // Tells garbage collector we already cleaned up
        }
        #endregion
    }
}

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

namespace StudyPlanner.Presentation.ViewModels;

/// <summary>
/// Main ViewModel for the Study Planner application.
/// Acts as the central bridge between the UI (View) and the business logic (Services/Repositories).
/// Implements MVVM pattern using CommunityToolkit.Mvvm for boilerplate reduction.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    // ========== DEPENDENCIES ==========

    private readonly ISubjectRepository _subjectRepository;
    private readonly ITimerService _timerService;
    private readonly IStudySessionService _studySessionService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly Dispatcher _dispatcher;
    private readonly IServiceProvider _serviceProvider;

    private bool _disposed;

    // ========== CONSTANTS ==========

    /// <summary>
    /// Circumference of the circular timer progress bar (2 * π * radius).
    /// Radius is effectively 90px in XAML. Used for StrokeDashOffset animation.
    /// </summary>
    private const double CircleCircumference = 565.487;

    // ========== OBSERVABLE PROPERTIES ==========

    [ObservableProperty] private Subject? _selectedSubject;
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private double _timerDashOffset;
    [ObservableProperty] private string _timerDisplay = "25:00";
    [ObservableProperty] private string _currentState = "Idle";
    [ObservableProperty] private bool _isTimerRunning;
    [ObservableProperty] private ObservableCollection<Subject> _subjects = new();
    [ObservableProperty] private string _newSubjectName = string.Empty;
    [ObservableProperty] private int _newPriority = 1;

    // Chart Data
    [ObservableProperty] private ISeries[] _weeklyStudySeries = Array.Empty<ISeries>();
    [ObservableProperty] private double _totalWeeklyHours = 0;

    // UI Helper Properties
    /// <summary>
    /// Returns the current date formatted for the UI header.
    /// Example: "terça-feira, 12 de maio"
    /// </summary>
    public static string CurrentDate => DateTime.Now.ToString("dddd, dd MMMM", CultureInfo.CurrentCulture);

    // ========== COMMANDS ==========

    public IAsyncRelayCommand LoadSubjectsCommand { get; }
    public IAsyncRelayCommand AddSubjectCommand { get; }
    public IAsyncRelayCommand StartTimerCommand { get; }
    public IAsyncRelayCommand StopTimerCommand { get; }
    public IAsyncRelayCommand GenerateReportCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    // ========== CONSTRUCTOR ==========

    public MainViewModel(
        ISubjectRepository subjectRepository,
        ITimerService timerService,
        IStudySessionService studySessionService,
        IServiceProvider serviceProvider,
        ILogger<MainViewModel> logger)
    {
        _subjectRepository = subjectRepository;
        _timerService = timerService;
        _studySessionService = studySessionService;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.FromThread(System.Threading.Thread.CurrentThread);

        // Initialize Commands
        LoadSubjectsCommand = new AsyncRelayCommand(LoadSubjectsAsync);
        AddSubjectCommand = new AsyncRelayCommand(AddSubjectAsync, CanAddSubject);
        StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, () => !IsTimerRunning);
        StopTimerCommand = new AsyncRelayCommand(StopTimerAsync, () => IsTimerRunning);
        GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);

        ToggleThemeCommand = new RelayCommand(() =>
        {
            IsDarkTheme = !IsDarkTheme;
            ThemeManager.ApplyTheme(IsDarkTheme);
        });

        OpenSettingsCommand = new RelayCommand(OpenSettings);

        // Subscribe to Timer Events
        _timerService.Tick += OnTimerTick;
        _timerService.StateChanged += OnStateChanged;
        _timerService.WorkPhaseCompleted += OnWorkPhaseCompleted;

        // Initialize UI State
        UpdateTimerDisplay(_timerService.RemainingTime);
        CurrentState = _timerService.CurrentState.ToString();

        // Load Data
        _ = SafeLoadSubjectsAsync();
        _ = SafeLoadWeeklyChartDataAsync();
    }

    // ========== SUBJECT MANAGEMENT ==========
    #region Subjects

    private async Task LoadSubjectsAsync()
    {
        var list = await _subjectRepository.GetAllAsync();
        Subjects.Clear();
        foreach (var s in list) Subjects.Add(s);
    }

    private async Task SafeLoadSubjectsAsync()
    {
        try { await LoadSubjectsAsync(); }
#pragma warning disable CA1031
        catch (Exception ex) { _logger.LogError(ex, "Failed to load subjects on startup"); }
#pragma warning restore CA1031
    }

    private async Task AddSubjectAsync()
    {
        try
        {
            var subject = Subject.Create(NewSubjectName, NewPriority);
            await _subjectRepository.AddAsync(subject);
            NewSubjectName = string.Empty;
            NewPriority = 1;
            await LoadSubjectsAsync();
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Failed to add subject");
        }
    }

    private bool CanAddSubject() =>
        !string.IsNullOrWhiteSpace(NewSubjectName) && NewPriority > 0;

    partial void OnNewSubjectNameChanged(string value) =>
        AddSubjectCommand.NotifyCanExecuteChanged();

    partial void OnNewPriorityChanged(int value) =>
        AddSubjectCommand.NotifyCanExecuteChanged();
    #endregion

    // ========== TIMER LOGIC ==========
    #region Timer

    private void OnTimerTick(object? sender, TimerTickEventArgs e)
    {
        if (_disposed) return;
        _dispatcher.InvokeAsync(() => UpdateTimerDisplay(e.RemainingTime));
    }

    private void OnStateChanged(object? sender, StateChangedEventArgs e)
    {
        if (_disposed) return;

        _dispatcher.InvokeAsync(() =>
        {
            CurrentState = e.NewState.ToString();
            IsTimerRunning = e.NewState != PomodoroState.Idle;
            StartTimerCommand.NotifyCanExecuteChanged();
            StopTimerCommand.NotifyCanExecuteChanged();
        });
    }

    private void OnWorkPhaseCompleted(object? sender, WorkPhaseCompletedEventArgs e)
    {
        if (_disposed) return;

        var subjectToRecord = SelectedSubject ?? Subjects.FirstOrDefault();

        if (subjectToRecord != null)
        {
            _ = _studySessionService.RecordSessionAsync(
                subjectToRecord.Id,
                DateTime.UtcNow,
                e.Duration);

            _ = SafeLoadWeeklyChartDataAsync();
        }
    }

    private void UpdateTimerDisplay(TimeSpan remaining)
    {
        // Use dynamic durations from settings if available, otherwise defaults
        // Note: For perfect sync with Settings, ITimerService should expose CurrentTotalDuration
        var totalSeconds = _timerService.CurrentState == PomodoroState.Work
            ? TimeSpan.FromMinutes(25).TotalSeconds
            : TimeSpan.FromMinutes(5).TotalSeconds;

        var elapsed = totalSeconds - remaining.TotalSeconds;
        var percent = Math.Clamp(elapsed / totalSeconds, 0, 1);

        TimerDashOffset = CircleCircumference * (1 - percent);

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

            ReportService.GenerateWeeklyReport(sessions, pdfPath);
            _logger.LogInformation($"Report generated at {pdfPath}");
        }
#pragma warning disable CA1031
        catch (Exception ex) { _logger.LogError(ex, "Failed to generate report"); }
#pragma warning restore CA1031
    }
    #endregion

    // ========== NAVIGATION ==========
    #region Settings

    private void OpenSettings()
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
        settingsWindow.ShowDialog();
    }
    #endregion

    // ========== CLEANUP ==========
    #region IDisposable

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

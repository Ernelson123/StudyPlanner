using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Entities;

namespace StudyPlanner.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel responsible for managing the Settings window.
    /// Handles loading current settings from the database, allowing user edits,
    /// and persisting changes back to the database and Timer Service.
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsRepository _settingsRepo;
        private readonly ITimerService _timerService;

        // ========================================================================
        // Observable Properties (Bound to UI Inputs)
        // ========================================================================

        /// <summary>
        /// Duration of the Work session in minutes.
        /// Bound to the "Work" TextBox in the UI.
        /// </summary>
        [ObservableProperty] private int _workMinutes = 25;

        /// <summary>
        /// Duration of the Short Break in minutes.
        /// Bound to the "Short Break" TextBox in the UI.
        /// </summary>
        [ObservableProperty] private int _shortBreakMinutes = 5;

        /// <summary>
        /// Duration of the Long Break in minutes.
        /// Bound to the "Long Break" TextBox in the UI.
        /// </summary>
        [ObservableProperty] private int _longBreakMinutes = 15;

        // ========================================================================
        // Commands
        // ========================================================================

        /// <summary>
        /// Command triggered by the "Save Changes" button.
        /// Validates inputs, saves to database, updates Timer Service, and requests window close.
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            // Basic Validation: Ensure all durations are positive
            if (WorkMinutes <= 0 || ShortBreakMinutes <= 0 || LongBreakMinutes <= 0)
            {
                // In a production app, you might show a validation error message here
                return;
            }

            // Create AppSettings entity with updated values
            var settings = new AppSettings
            {
                Id = 1, // Singleton ID
                WorkDurationMinutes = WorkMinutes,
                ShortBreakDurationMinutes = ShortBreakMinutes,
                LongBreakDurationMinutes = LongBreakMinutes
            };

            // Persist changes to SQLite database
            await _settingsRepo.UpdateAsync(settings);

            // Immediately update the running Timer Service with new durations
            // This ensures that if the timer is paused/idle, it picks up the new times
            _timerService.UpdateDurations(
                TimeSpan.FromMinutes(WorkMinutes),
                TimeSpan.FromMinutes(ShortBreakMinutes),
                TimeSpan.FromMinutes(LongBreakMinutes)
            );

            // Request the View to close itself
            // We use an Action callback instead of a static command for better MVVM decoupling
            RequestClose?.Invoke();
        }

        /// <summary>
        /// Command triggered by the "Cancel" button or Window Close 'X'.
        /// Simply requests the window to close without saving.
        /// </summary>
        [RelayCommand]
        public void CancelAndClose()
        {
            RequestClose?.Invoke();
        }

        // ========================================================================
        // Events for View Interaction
        // ========================================================================

        /// <summary>
        /// Event raised when the ViewModel requests the View to close.
        /// The View (SettingsWindow.xaml.cs) should subscribe to this and call this.Close().
        /// This keeps the ViewModel free of direct UI dependencies.
        /// </summary>
#pragma warning disable CA1003 // Use generic event handler instances
        public event Action? RequestClose;
#pragma warning restore CA1003 // Use generic event handler instances

        // ========================================================================
        // Constructor & Initialization
        // ========================================================================

        public SettingsViewModel(ISettingsRepository settingsRepo, ITimerService timerService)
        {
            _settingsRepo = settingsRepo;
            _timerService = timerService;

            // Load initial settings from database asynchronously
            // Fire-and-forget is acceptable here as we just want to populate fields on startup
            _ = LoadSettingsAsync();
        }

        /// <summary>
        /// Loads existing settings from the database and populates the observable properties.
        /// If no settings exist, creates defaults via the Repository.
        /// </summary>
        private async Task LoadSettingsAsync()
        {
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                var settings = await _settingsRepo.GetOrCreateAsync();

                // Update properties (this triggers UI updates via INotifyPropertyChanged)
                WorkMinutes = settings.WorkDurationMinutes;
                ShortBreakMinutes = settings.ShortBreakDurationMinutes;
                LongBreakMinutes = settings.LongBreakDurationMinutes;
            }
            catch (Exception ex)
            {
                // Log error if needed. For now, fail silently and keep defaults.
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}

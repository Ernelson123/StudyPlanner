using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Services;
using StudyPlanner.Data;
using StudyPlanner.Data.Repositories;
using StudyPlanner.Data.Services;
using StudyPlanner.Presentation.Services;
using StudyPlanner.Presentation.ViewModels;
using StudyPlanner.Presentation.Views;
using System.Globalization;
using System.Windows;

namespace StudyPlanner.Presentation
{
    /// <summary>
    /// Application entry point for the Study Planner WPF application.
    /// Responsible for:
    /// - Initializing the Dependency Injection container (Host).
    /// - Configuring global logging and error handling.
    /// - Managing application lifecycle (Startup/Shutdown).
    /// </summary>
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// Gets the application host (DI container).
        /// Static property allows access from anywhere if needed, though primarily used internally.
        /// </summary>
        public static IHost? AppHost { get; private set; }

        /// <summary>
        /// Called when the application starts.
        /// Initializes services, applies default settings, and launches the main window.
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // ========================================================================
                // STEP 1: CONFIGURE GLOBAL SETTINGS
                // ========================================================================

                // Set application culture to match system settings for consistent date/number formatting
                var culture = CultureInfo.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // Apply default Light Theme immediately to avoid visual flicker on startup
                ThemeManager.ApplyTheme(isDark: false);

                // ========================================================================
                // STEP 2: BUILD DEPENDENCY INJECTION HOST
                // ========================================================================

                AppHost = Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging =>
                    {
                        // Clear default providers to use custom configuration
                        logging.ClearProviders();

                        // Add Console Logger (visible in Visual Studio Output Window during Debug)
                        logging.AddConsole();

                        // Set minimum log level to Information (filters out verbose Debug/Trace logs)
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .ConfigureServices((_, services) =>
                    {
                        // --------------------------------------------------------------------
                        // APPLICATION SERVICES (Business Logic & Core Features)
                        // --------------------------------------------------------------------

                        // Singleton: One instance for the entire app lifetime.
                        // The Timer must be singleton so it continues running regardless of UI state.
                        services.AddSingleton<ITimerService, PomodoroTimerService>();

                        // Singleton: Statelessness allows shared usage across ViewModels.
                        services.AddSingleton<ITimeAllocator, TimeAllocator>();

                        // Singleton: Notification service handles OS-level interactions globally.
                        services.AddSingleton<INotificationService, DesktopNotificationService>();

                        // Transient: New instance each time. Lightweight stateless services.
                        services.AddTransient<IPlanningService, PlanningService>();
                        services.AddTransient<IStatisticsService, StatisticsService>();

                        // --------------------------------------------------------------------
                        // DATA LAYER (Database & Repositories)
                        // --------------------------------------------------------------------

                        // Configure Entity Framework Core with SQLite
                        // "studyplanner.db" will be created in the application's working directory
                        services.AddDbContext<StudyPlannerDbContext>(options =>
                            options.UseSqlite("Data Source=studyplanner.db"));

                        // Repositories are Transient as they are lightweight wrappers around DbContext
                        services.AddTransient<ISubjectRepository, SubjectRepository>();
                        services.AddTransient<ISettingsRepository, SettingsRepository>();

                        // Services that directly access DbContext belong in the Data Layer
                        services.AddTransient<IStudySessionService, StudySessionService>();

                        // --------------------------------------------------------------------
                        // UI LAYER (Views & ViewModels)
                        // --------------------------------------------------------------------

                        // Views are Transient so a new window is created each time it's requested
                        services.AddTransient<MainWindow>();
                        services.AddTransient<SettingsWindow>();

                        // ViewModels are Transient to ensure fresh state for each view instance
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<SettingsViewModel>();
                    })
                    .Build();

                // ========================================================================
                // STEP 3: GLOBAL ERROR HANDLING
                // ========================================================================

                // Handle unhandled exceptions from background threads (e.g., Timer ticks)
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    var ex = args.ExceptionObject as Exception;
                    MessageBox.Show(
                        $"Unhandled Error:\n{ex?.Message}",
                        "Fatal",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                };

                // Handle unhandled exceptions from the UI thread (e.g., Button clicks, Binding errors)
                DispatcherUnhandledException += (sender, args) =>
                {
                    MessageBox.Show(
                        $"UI Error:\n{args.Exception.Message}",
                        "UI Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Mark as handled to prevent the app from crashing
                    args.Handled = true;
                };

                // ========================================================================
                // STEP 4: LAUNCH APPLICATION
                // ========================================================================

                // Start the DI Host (initializes all singletons and background services)
                await AppHost.StartAsync().ConfigureAwait(false);

                // Resolve MainWindow via DI (injects MainViewModel and dependencies automatically)
                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();

                // Display the main window
                mainWindow.Show();
            }
#pragma warning disable CA1031 // Generic catch acceptable at application entry point
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // Critical failure during startup
                MessageBox.Show(
                    $"Critical startup error:\n{ex.Message}",
                    "Fatal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        /// <summary>
        /// Called when the application exits.
        /// Ensures graceful shutdown of services and disposal of resources.
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost is not null)
            {
                // Stop the host gracefully (calls Dispose on all IDisposable singletons)
                await AppHost.StopAsync().ConfigureAwait(false);

                // Release all resources held by the host
                AppHost.Dispose();
            }

            base.OnExit(e);
        }
    }
}
